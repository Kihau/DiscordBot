using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.EventArgs;
using System.Collections.Concurrent;
using Shitcord.Services;
using Shitcord.Database;
using Shitcord.Database.Queries;
using Shitcord.Extensions;

namespace Shitcord.Data;

public enum LoopingMode : int
{
    None = 0,
    Queue = 1,
    Song = 2,
    Shuffle = 3,
}

/// This data is stored inside a hashmap (in AudioService) and mapped to each guild
public class GuildAudioData
{
    // I really don't need to store those two referances but whatever
    // It's not that this bot is going to be in hundreds of thousands of guilds
    private LavalinkNodeConnection Lavalink { get; }
    private DiscordClient Client { get; }
    private DatabaseService DatabaseContext { get; }
    private DiscordGuild Guild { get; }

    private ConcurrentQueue<LavalinkTrack> Queue { get; }
    private LavalinkGuildConnection? Player { get; set; }
    public LavalinkTrack? CurrentTrack { get; private set; }
    public LavalinkTrack? PreviousTrack { get; private set; }
    public DiscordChannel? Channel => this.Player?.Channel;
    public bool SkipEventFire { get; set; } = false;
    public bool SkipEnqueue { get; set; } = false;
    // TODO: Use this in all sorts of stuff
    // public bool InvokedPlayIntro { get; set; } = false;

    private LoopingMode _looping;
    public LoopingMode Looping { 
        get => _looping;
        set {
            _looping = value;                                      
            DatabaseContext.executeUpdate(QueryBuilder
                .New().Update(GuildAudioTable.TABLE_NAME)
                .WhereEquals(GuildAudioTable.GUILD_ID, Guild.Id)
                .Set(GuildAudioTable.LOOPING, (int)value)
                .Build()
            );
        }
    }

    public bool IsPaused { get; private set; }
    public bool IsConnected => this.Player != null;
    public bool IsStopped => this.CurrentTrack == null;

    public int Volume { get; private set; } = 100;
    public AudioFilters Filters { get; set; } = new();

    public bool AutoJoinChannel { get; set; } = false;
    public bool ResumeOnAutoJoin { get; set; } = false;

    private Timer? _leaveTimer;
    public TimeSpan LeaveTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool TimeoutStarted { get; private set; }

    public DiscordMessage? QueueUpdateMessage { get; set; }
    public DiscordChannel? QueueUpdateChannel { get; set; }
    public DiscordMessage? SongUpdateMessage { get; set; }
    public DiscordChannel? SongUpdateChannel { get; set; }

    public Timer MessageUpdaterTimer { get; }

    public bool QueueRequiresUpdate { get; set; } = false;
    public bool SongRequiresUpdate { get; set; } = false;

    // Change it later (maybe not?)
    public int page = 0;


    public GuildAudioData(
        DiscordGuild guild, LavalinkNodeConnection lavalink, DiscordClient client, 
        DatabaseService database
    ) {
        Client = client;
        Guild = guild;
        Lavalink = lavalink;
        DatabaseContext = database;

        // Try to load UpdateMessages here
        InitializeDatabase();

        Queue = new ConcurrentQueue<LavalinkTrack>();
        Filters = new AudioFilters();

        //Task.Run(AutoMessageUpdater);
        MessageUpdaterTimer = new Timer(AutoMessageUpdater, null, 1000, 1000);
    }

    private async void AutoMessageUpdater(object? state_info) {
        if (SongRequiresUpdate) {
            SongRequiresUpdate = false;
            await UpdateSongMessage();
            return;
        }

        if (!QueueRequiresUpdate)
            return;
        
        QueueRequiresUpdate = false;
        await UpdateQueueMessage();
    }

    public async Task CreateConnectionAsync(DiscordChannel vchannel)
    {
        if (Player is {IsConnected: true}) {
            if (vchannel != Player.Channel)
                await DestroyConnectionAsync();
            else return;
        }
        
        // NOTE: Cannot self deafen the bot?
        // var member = await Guild.GetMemberAsync(Client.CurrentUser.Id, true);
        // if (member != null)
        //     await member.SetDeafAsync(true, "Bots have no ears");

        // TODO: FIX: For some reason ConnectAsync can block the bot - check why
        Player = await Lavalink.ConnectAsync(vchannel);
        await Player.SetVolumeAsync(Volume);
        await Player.SetAudiofiltersAsync(Filters);

        Player.PlaybackFinished += PlaybackFinished;
        
        // Intro song is broken
        // var tracks = await Lavalink.Rest.GetTracksAsync(new FileInfo("Resources/join-sound.mp3"));
        // var track = tracks.Tracks.First();
        // if (track != null) {
        //     await PlayIntroAsync(track);
        // }
    }

    private void InitializeDatabase() {
        bool exists_in_table = DatabaseContext.ExistsInTable(
            GuildAudioTable.TABLE_NAME, 
            Condition.New(GuildAudioTable.GUILD_ID).Equals(Guild.Id)
        );

        //  0         1           2           3       4       5       6
        //  GUILD_ID, QU_CHANNEL, SU_CHANNEL, QU_MSG, SU_MSG, VOLUME, LOOPING 
        if (!exists_in_table) {
            DatabaseContext.executeUpdate(QueryBuilder
                .New().Insert()
                .Into(GuildAudioTable.TABLE_NAME)
                .Values(
                    Guild.Id,
                    QueueUpdateChannel?.Id,
                    SongUpdateChannel?.Id,
                    QueueUpdateMessage?.Id,
                    SongUpdateMessage?.Id,
                    Volume,
                    (int)Looping,
                    LeaveTimeout.Ticks,
                    AutoJoinChannel,
                    ResumeOnAutoJoin
                ).Build()
            );

            return;
        }

        var retrieved = DatabaseContext.RetrieveColumns(QueryBuilder
            .New().Retrieve("*")
            .From(GuildAudioTable.TABLE_NAME)
            .WhereEquals(GuildAudioTable.GUILD_ID, Guild.Id.ToString())
            .Build()
        );

        if (retrieved is null)
            throw new UnreachableException();

        var qu_channel_id = retrieved[1][0];
        var qu_message_id = retrieved[3][0];

        if (qu_channel_id != null && qu_message_id != null) {
            try { 
                var channel = Guild.GetChannel((ulong)(long)qu_channel_id);
                QueueUpdateChannel = channel;
                var message = channel.GetMessageAsync((ulong)(long)qu_message_id)
                    .GetAwaiter().GetResult();
                QueueUpdateMessage = message; 
                QueueRequiresUpdate = true;
            } catch { /* Ignored */ }
        } 

        var su_channel_id = retrieved[2][0];
        var su_message_id = retrieved[4][0];

        if (su_channel_id != null && su_message_id != null) {
            try { 
                var channel = Guild.GetChannel((ulong)(long)su_channel_id);
                SongUpdateChannel = channel;
                var message = channel.GetMessageAsync((ulong)(long)su_message_id)
                    .GetAwaiter().GetResult();
                SongUpdateMessage = message; 
                SongRequiresUpdate = true;
            } catch { /* Ignored */ }
        } 

        Volume = (int)(long)(retrieved[5][0] ?? 100);
        Looping = (LoopingMode)(long)(retrieved[6][0] ?? 0);
        LeaveTimeout = TimeSpan.FromTicks((long)(retrieved[6][0] ?? 0));
        AutoJoinChannel = (long)(retrieved[7][0] ?? false) == 1;
        ResumeOnAutoJoin = (long)(retrieved[8][0] ?? false) == 1;
    }

    void DatabaseUpdateQU() 
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New().Update(GuildAudioTable.TABLE_NAME)
            .WhereEquals(GuildAudioTable.GUILD_ID, Guild.Id)
            .Set(GuildAudioTable.QU_CHANNEL, QueueUpdateChannel?.Id)
            .Set(GuildAudioTable.QU_MSG, QueueUpdateMessage?.Id)
            .Build()
        );
    }

    void DatabaseUpdateSU() 
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New().Update(GuildAudioTable.TABLE_NAME)
            .WhereEquals(GuildAudioTable.GUILD_ID, Guild.Id)
            .Set(GuildAudioTable.SU_CHANNEL, SongUpdateChannel?.Id)
            .Set(GuildAudioTable.SU_MSG, SongUpdateMessage?.Id)
            .Build()
        );
    }

    public void DatabaseUpdateLeavetimeout() 
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New().Update(GuildAudioTable.TABLE_NAME)
            .WhereEquals(GuildAudioTable.GUILD_ID, Guild.Id)
            .Set(GuildAudioTable.TIMEOUT, LeaveTimeout.Ticks)
            .Build()
        );
    }

    public void DatabaseUpdateAutoJoin() 
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New().Update(GuildAudioTable.TABLE_NAME)
            .WhereEquals(GuildAudioTable.GUILD_ID, Guild.Id)
            .Set(GuildAudioTable.AUTOJOIN, AutoJoinChannel)
            .Set(GuildAudioTable.AUTORESUME, ResumeOnAutoJoin)
            .Build()
        );
    }

    public async Task SetSongUpdate(DiscordChannel channel)
    {
        this.SongUpdateChannel = channel;

        try
        {
            if (this.SongUpdateMessage != null)
                await this.SongUpdateMessage.DeleteAsync();
        } catch { /* ignored */ }

        var mess = GenerateSongMessage();
        this.SongUpdateMessage = await this.SongUpdateChannel.SendMessageAsync(mess);
        DatabaseUpdateSU();
    }

    public async Task SetQueueUpdate(DiscordChannel channel)
    {
        this.QueueUpdateChannel = channel;

        try
        {
            if (this.QueueUpdateMessage != null)
                await this.QueueUpdateMessage.DeleteAsync();
        } catch { /* ignored */ }

        var mess = GenerateQueueMessage();
        this.QueueUpdateMessage = await this.QueueUpdateChannel.SendMessageAsync(mess);
        DatabaseUpdateQU();
    }

    public async Task DestroyQueueUpdate()
    {
        this.QueueUpdateChannel = null;

        try
        {
            if (this.QueueUpdateMessage != null)
                await this.QueueUpdateMessage.DeleteAsync();
        } catch { /* ignored */ }

        this.QueueUpdateMessage = null;
        DatabaseUpdateQU();
    }

    public async Task DestroySongUpdate()
    {
        this.SongUpdateChannel = null;

        try
        {
            if (this.SongUpdateMessage != null)
                await this.SongUpdateMessage.DeleteAsync();
        } catch { /* ignored */ }

        this.SongUpdateMessage = null;
        DatabaseUpdateSU();
    }

    public DiscordMessageBuilder GenerateQueueMessage()
    {
        var tracks = this.GetNextTracks();

        var embed = new DiscordEmbedBuilder();
        string description = "";

        const int page_size = 20;
        var page_count = tracks.Length / page_size;

        if (this.page < 0)
            this.page = 0;
        else if (this.page > page_count)
            this.page = page_count;

        for (var i = this.page * page_size; 
                i < tracks.Length && i < (this.page + 1) * page_size; i++)
            description += $"{i + 1}. [{tracks[i].Title}]({tracks[i].Uri})\n";

        // if (tracks.Length - this.page * page_size > page_size)
        //     description += $". . . and {tracks.Length - page_size * (this.page + 1)} more";

        if (tracks.Length == 0)
            description = "Queue is empty";

        embed.WithTitle(":question:  |  Queue Info: ")
            .WithDescription(description)
            .WithFooter($"Page {this.page + 1} / {page_count + 1}")
            .WithColor(DiscordColor.Purple);

        var builder = new DiscordMessageBuilder()
        {
            Embed = embed.Build(),
        };

        builder.AddComponents(
            new DiscordButtonComponent(ButtonStyle.Primary, "firstpage_btn", null, false,
                new DiscordComponentEmoji("\u23ea")),
            new DiscordButtonComponent(ButtonStyle.Primary, "prevpage_btn", null, false,
                new DiscordComponentEmoji("\u25c0\ufe0f")),
            new DiscordButtonComponent(ButtonStyle.Primary, "nextpage_btn", null, false,
                new DiscordComponentEmoji("\u25b6\ufe0f")),
            new DiscordButtonComponent(ButtonStyle.Primary, "lastpage_btn", null, false,
                new DiscordComponentEmoji("\u23e9"))
        );
        
        builder.AddComponents(
            new DiscordButtonComponent(ButtonStyle.Secondary, "e1_btn", " ", true),
            new DiscordButtonComponent(ButtonStyle.Success, "shuffle_btn", null, false,
                new DiscordComponentEmoji("\U0001f3b2")),
            new DiscordButtonComponent(ButtonStyle.Danger, "clear_btn", null, false,
                new DiscordComponentEmoji("\U0001f5d1\ufe0f")),
            new DiscordButtonComponent(ButtonStyle.Secondary, "e2_btn", " ", true)
        );

        return builder;
    }

    private Task UpdateQueueMessage()
    {
        if (this.QueueUpdateMessage == null || this.QueueUpdateChannel == null)
            return Task.CompletedTask;

        Task.Run(async () => {
            var message = this.GenerateQueueMessage();
            if (DateTime.Now - this.QueueUpdateMessage.Timestamp < TimeSpan.FromHours(1)) {
                await this.QueueUpdateMessage.ModifyAsync(message);
            } else {
                await this.QueueUpdateMessage.DeleteAsync();
                this.QueueUpdateMessage = await this.QueueUpdateChannel.SendMessageAsync(message);
                DatabaseUpdateQU();
            }
        });

        return Task.CompletedTask;
    }

    public DiscordMessageBuilder GenerateSongMessage()
    {
        TimeSpan length;
        string current_song, state, state_btn;
        string? author;
        if (this.IsStopped) {
            current_song = "Nothing is playing";
            author = "N/A";
            state = "Stopped";
            state_btn = "Play";
            length = TimeSpan.Zero;
        } else {
            author = this.CurrentTrack?.Author;
            state = this.IsPaused ? "Paused" : "Playing";
            state_btn = this.IsPaused ? "Resume" : "Pause";
            current_song = $"[{this.CurrentTrack?.Title}]({this.CurrentTrack?.Uri})";
            length = this.CurrentTrack?.Length ?? TimeSpan.Zero;
        }

        var next_song = !this.Queue.TryPeek(out var next) 
            ? "Queue is empty" : $"[{next.Title}]({next.Uri})";

        var embed = new DiscordEmbedBuilder()
            .WithTitle(":question:  |  Song Info: ")
            .WithDescription(
                $":musical_note: **Now playing:** {current_song}\n" +
                $":play_pause: **Song length:** {length}\n" +
                $":cinema: **Song Author:** {author}\n" +
                $":track_next: **Next Song:** {next_song}\n" +
                $":arrow_right: **Songs in queue:** {Queue.Count}\n" +
                $":information_source: **Song state:** {state}\n" +
                $":arrows_clockwise: **Song looping mode:** {Looping}"
            ).WithColor(DiscordColor.Purple);

        var builder = new DiscordMessageBuilder() {
            Embed = embed.Build(),
        };


        builder.AddComponents(
            new DiscordButtonComponent(ButtonStyle.Primary, "skip_btn", "Skip"),
            new DiscordButtonComponent(ButtonStyle.Primary, "prev_btn", "Prev"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "loop_btn", "Loop"),
            // TODO: MAYBE:
            IsConnected 
                ? new DiscordButtonComponent(ButtonStyle.Success, "state_btn", state_btn) 
                : new DiscordButtonComponent(ButtonStyle.Success, "state_btn", state_btn, true) 
            // new DiscordButtonComponent(ButtonStyle.Success, "state_btn", state_btn, true) 
                
        );

        builder.AddComponents(
            new DiscordButtonComponent(ButtonStyle.Danger, "remove_btn", "Remove"),
            new DiscordButtonComponent(ButtonStyle.Danger, "join_btn", "Join"),
            new DiscordButtonComponent(ButtonStyle.Danger, "stop_btn", "Stop"),
            new DiscordButtonComponent(ButtonStyle.Danger, "leave_btn", "Leave")
        );

        return builder;
    }

    private Task UpdateSongMessage()
    {
        if (this.SongUpdateMessage == null || this.SongUpdateChannel == null)
            return Task.CompletedTask;

        Task.Run(async () => {
            var message = this.GenerateSongMessage();
            if (DateTime.Now - this.SongUpdateMessage.Timestamp < TimeSpan.FromHours(1))
                await this.SongUpdateMessage.ModifyAsync(message);
            else
            {
                await this.SongUpdateMessage.DeleteAsync();
                this.SongUpdateMessage = await this.SongUpdateChannel.SendMessageAsync(message);
                DatabaseUpdateSU();
            }
        });

        return Task.CompletedTask;
    }

    public void StartTimeout()
    {
        _leaveTimer?.Dispose();
        _leaveTimer = new Timer(
            LeaveTimerCallback, null, 
            LeaveTimeout, Timeout.InfiniteTimeSpan
        );

        TimeoutStarted = true;
    }

    public void CancelTimeout()
    {
        this._leaveTimer?.Dispose();
        this.TimeoutStarted = false;

        this.ResumeAsync().GetAwaiter().GetResult();
    }

    private async void LeaveTimerCallback(object? sender)
    {
        if (this.Player is not {IsConnected: true}) return;

        await this.DestroyConnectionAsync();

        SongRequiresUpdate = true;

        if (this._leaveTimer != null) 
            await this._leaveTimer.DisposeAsync();
    }

    public async Task SetVolumeAsync(int volume)
    {
        if (this.Player is not {IsConnected: true})
            return;

        await this.Player.SetVolumeAsync(volume);
        this.Volume = volume;

        DatabaseContext.executeUpdate(QueryBuilder
            .New().Update(GuildAudioTable.TABLE_NAME)
            .WhereEquals(GuildAudioTable.GUILD_ID, Guild.Id)
            .Set(GuildAudioTable.VOLUME, Volume)
            .Build()
        );
    }

    public async Task DestroyConnectionAsync()
    {
        if (this.Player == null)
            return;

        if (this.Player.IsConnected) {
            if (CurrentTrack != null) {
                switch (Looping) {
                    case LoopingMode.None: break;

                    case LoopingMode.Queue: {
                        Enqueue(CurrentTrack);
                    } break;

                    case LoopingMode.Song: { 
                        EnqueueFirst(CurrentTrack);
                    } break;

                    case LoopingMode.Shuffle: { 
                        EnqueueRandom(CurrentTrack);
                    } break;
                }
            }

            await this.Player.DisconnectAsync();
        }

        this.Player = null;
        this.CurrentTrack = null;
    }

    private async Task PlaybackFinished(LavalinkGuildConnection con, TrackFinishEventArgs e)
    {
        if (!SkipEnqueue) {
            switch (Looping) {
                case LoopingMode.None: break;

                case LoopingMode.Queue: {
                    Enqueue(e.Track);
                } break;

                case LoopingMode.Song: { 
                    EnqueueFirst(e.Track);
                } break;

                case LoopingMode.Shuffle: { 
                    EnqueueRandom(e.Track);
                } break;
            }
        } else SkipEnqueue = false;

        if (SkipEventFire) {
            SkipEventFire = false;
            return;
        }

        await PlayerHandlerAsync();
    }

    public int ClearQueue()
    {
        var count = Queue.Count;
        Queue.Clear();
        return count;
    }

    // TODO: Do not start the next song if PlayIntro was invoked?
    private async Task PlayerHandlerAsync()
    {
        PreviousTrack = CurrentTrack;
        CurrentTrack = Dequeue();
        if (CurrentTrack == null || Player == null)
            return;

        await Player.PlayAsync(CurrentTrack);
        await Player.SeekAsync(TimeSpan.Zero);
        IsPaused = false;

        SongRequiresUpdate = true;
        QueueRequiresUpdate = true;
    }

    public void Enqueue(LavalinkTrack track)
        => Queue.Enqueue(track);

    public void EnqueueFirst(LavalinkTrack track)
    {
        var items = Queue.ToArray();
        Queue.Clear();
        Queue.Enqueue(track);

        foreach (var item in items)
            Queue.Enqueue(item);
    }

    public void EnqueueRandom(LavalinkTrack track)
    {
        var items = Queue.ToList();
        this.Queue.Clear();

        var rng = new Random();
        var index = rng.Next(items.Count);
        items.Insert(index, track);

        foreach (var item in items)
            this.Queue.Enqueue(item);
    }

    public void Enqueue(IEnumerable<LavalinkTrack> tracks)
    {
        foreach (var track in tracks)
            this.Queue.Enqueue(track);
    }

    public void EnqueueFirst(IEnumerable<LavalinkTrack> tracks)
    {
        var items = Queue.ToArray();
        this.Queue.Clear();

        foreach (var track in tracks)
            this.Queue.Enqueue(track);

        foreach (var item in items)
            this.Queue.Enqueue(item);
    }

    public void EnqueueRandom(IEnumerable<LavalinkTrack> tracks)
    {
        var items = Queue.ToList();
        this.Queue.Clear();

        // Awful, horrible, disgusting
        // Also, don't care
        var rng = new Random();
        foreach (var track in tracks) {
            var index = rng.Next(items.Count);
            items.Insert(index, track);
        }

        foreach (var item in items)
            this.Queue.Enqueue(item);
    }
    
    public async Task SetAudioFiltersAsync(AudioFilters? filters = null)
    {
        if (filters != null)
            this.Filters = filters;
        
        if (this.Player is not {IsConnected: true})
            return;
        
        await this.Player.SetAudiofiltersAsync(this.Filters);
    }
    
    public LavalinkTrack? Dequeue() =>
        this.Queue.TryDequeue(out var track) ? track : null;

    //public LoopingMode ChangeLoopingState(LoopingMode looping)
    //{
    //    Looping = looping;

    //    DatabaseContext.executeUpdate(QueryBuilder
    //        .New().Update(GuildAudioTable.TABLE_NAME)
    //        .WhereEquals(GuildAudioTable.GUILD_ID, Guild.Id)
    //        .Set(GuildAudioTable.LOOPING, Looping)
    //        .Build()
    //    );

    //    return Looping;
    //}

    public LavalinkTrack? Remove(int index)
        => this.RemoveRange(index, 1).FirstOrDefault();

    public IEnumerable<LavalinkTrack> RemoveRange(int index, int count)
    {
        var removed = new List<LavalinkTrack>();
        var qlist = this.Queue.ToList();

        if (index < 0 || index >= qlist.Count || index + count > qlist.Count)
            return removed;

        removed = qlist.GetRange(index, count);
        qlist.RemoveRange(index, count);

        this.ClearQueue();
        this.Enqueue(qlist);

        return removed;
    }

    public async Task PlayAsync()
    {
        if (this.Player is not {IsConnected: true})
            return;

        if (this.CurrentTrack != null)
            this.SkipEventFire = true;

        await this.PlayerHandlerAsync();
    }

    // public async Task PlayIntroAsync(LavalinkTrack track)
    // {
    //     if (Player is not {IsConnected: true})
    //         return;
    //
    //     SkipEventFire = true;
    //     SkipEnqueue = true;
    //
    //     await Player.PlayAsync(track);
    //     await Player.SeekAsync(TimeSpan.Zero);
    //     await Task.Delay(track.Length);
    // }

    public async Task StopAsync()
    {
        if (this.Player is not {IsConnected: true})
            return;

        this.CurrentTrack = null;
        this.SkipEventFire = true;

        await this.Player.StopAsync();
    }

    public async Task PauseAsync()
    {
        if (this.Player is not {IsConnected: true})
            return;

        await this.Player.PauseAsync();
        this.IsPaused = true;
    }

    public async Task ResumeAsync()
    {
        if (this.Player is not {IsConnected: true})
            return;

        await this.Player.ResumeAsync();
        this.IsPaused = false;
    }

    public void Shuffle()
    {
        var rng = new Random();
        var qlist = this.Queue.ToList().OrderBy(_ => rng.Next());
        this.Queue.Clear();

        this.Enqueue(qlist);
    }

    public void SortByTitle(bool ascending) {
        IOrderedEnumerable<LavalinkTrack> qlist;
        // if (ascending)
        //     qlist = this.Queue.OrderByDescending(x => String.Concat(x.Title.OrderBy(c => c)));
        // else qlist = this.Queue.OrderByDescending(x => String.Concat(x.Title.OrderBy(c => c)));

        if (ascending)
            qlist = this.Queue.ToList().OrderBy(x => x.Title);
        else qlist = this.Queue.ToList().OrderByDescending(x => x.Title);

        this.Queue.Clear();
        this.Enqueue(qlist);
    }

    public void SortByLenght(bool ascending) {
        IOrderedEnumerable<LavalinkTrack> qlist;
        if (ascending)
            qlist = this.Queue.ToList().OrderBy(x => x.Length);
        else qlist = this.Queue.ToList(). OrderByDescending(x => x.Length);

        this.Queue.Clear();
        this.Enqueue(qlist);
    }

    public async Task PreviousAsync() {
        if (Player is not {IsConnected: true})
            return;

        if (PreviousTrack == null)
            return;
        
        if (CurrentTrack != null)
            EnqueueFirst(CurrentTrack);

        EnqueueFirst(PreviousTrack);
        PreviousTrack = null;

        await Player.StopAsync();
    }

    public async Task SkipAsync(int num, bool skip_enqueue = false)
    {
        if (Player is not {IsConnected: true})
            return;

        SkipEnqueue = skip_enqueue;

        var tracks = new List<LavalinkTrack>();
        while (--num > 0) {
            var track = Dequeue();
            
            if (track != null && Looping != LoopingMode.None)
                tracks.Add(track);
        }

        await Player.StopAsync();

        if (skip_enqueue) return;

        switch (Looping) {
            case LoopingMode.None:
            case LoopingMode.Queue: {
                Enqueue(tracks);
            } break;

            case LoopingMode.Song: { 
                EnqueueFirst(tracks);
            } break;

            case LoopingMode.Shuffle: { 
                EnqueueRandom(tracks);
            } break;
        }
    }

    public async Task SeekAsync(TimeSpan timestamp) 
    {
        if (this.Player is not {IsConnected: true})
            return;

        await this.Player.SeekAsync(timestamp);
    }

    public LavalinkTrack[] GetNextTracks()
        => this.Queue.ToArray();

    public TimeSpan? GetTimestamp()
        => this.Player?.CurrentState.PlaybackPosition;
}

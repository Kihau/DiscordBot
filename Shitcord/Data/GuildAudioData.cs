using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.EventArgs;
using System.Collections.Concurrent;
using Shitcord.Services;

namespace Shitcord.Data;

/// This data is stored inside a hashmap (in AudioService) and mapped to each guild
public class GuildAudioData
{
    // I really don't need to store those two referances but whatever
    // It's not that this bot is going to be in hundreds of thousands of guilds
    private LavalinkNodeConnection Lavalink { get; }
    private DatabaseService DatabaseContext { get; }
    private DiscordGuild Guild { get; }

    private ConcurrentQueue<LavalinkTrack> Queue { get; }
    private LavalinkGuildConnection? Player { get; set; }
    public LavalinkTrack? CurrentTrack { get; private set; }
    public DiscordChannel? Channel => this.Player?.Channel;
    public bool SkipEventFire { get; set; } = false;

    // TODO: Looping mode (song, queue, shuffle. none)
    public bool IsLooping { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsConnected => this.Player != null;
    public bool IsStopped => this.CurrentTrack == null;

    public int Volume { get; private set; } = 100;
    public AudioFilters Filters { get; set; } = new();

    // TODO: Autoresume, autojoin guild dependent 
    // TODO: Custom timeout times for those two
    private Timer? _leaveTimer;
    public bool TimeoutStarted { get; private set; }

    // TODO: Update message only when content has changed (ratelimits)
    // TODO: Set custom timeout for updating messages (to not queue unnesessary updates)
    public DiscordMessage? QueueUpdateMessage { get; set; }
    public DiscordChannel? QueueUpdateChannel { get; set; }
    public DiscordMessage? SongUpdateMessage { get; set; }
    public DiscordChannel? SongUpdateChannel { get; set; }

    // Change it later (maybe not?)
    public int page = 0;

    public GuildAudioData(DiscordGuild guild, LavalinkNodeConnection lavalink,
        DiscordClient client, DatabaseService database)
    {
        this.Guild = guild;
        this.Lavalink = lavalink;
        this.DatabaseContext = database;

        // Try to load UpdateMessages here
        LoadFromDatabase();

        this.Queue = new ConcurrentQueue<LavalinkTrack>();
        this.Filters = new AudioFilters();
    }

    ~GuildAudioData() => SaveToDatabase();

    public async Task CreateConnectionAsync(DiscordChannel vchannel)
    {
        if (this.Player is {IsConnected: true})
        {
            if (vchannel != this.Player.Channel)
                await this.DestroyConnectionAsync();
            else return;
        }

        this.Player = await this.Lavalink.ConnectAsync(vchannel);
        await this.Player.SetVolumeAsync(this.Volume);
        await this.Player.SetAudiofiltersAsync(this.Filters);
        this.Player.PlaybackFinished += PlaybackFinished;
    }

    private void LoadFromDatabase() 
    {
        var db = DatabaseContext;
        var qu_channel_id = db.ReadQUChannel(Guild.Id);
        var qu_message_id = db.ReadQUMessage(Guild.Id);

        if (qu_channel_id != null && qu_message_id != null) {
            try { 
                var channel = Guild.GetChannel(qu_channel_id.Value);
                QueueUpdateChannel = channel;
                var message = channel.GetMessageAsync(qu_message_id.Value)
                    .GetAwaiter().GetResult();
                QueueUpdateMessage = message; 
                UpdateQueueMessage();
            } catch { /* Ignored */ }
        } 

        var su_channel_id = db.ReadSUChannel(Guild.Id);
        var su_message_id = db.ReadSUMessage(Guild.Id);

        if (su_channel_id != null && su_message_id != null) {
            try { 
                var channel = Guild.GetChannel(su_channel_id.Value);
                SongUpdateChannel = channel;
                var message = channel.GetMessageAsync(su_message_id.Value)
                    .GetAwaiter().GetResult();
                SongUpdateMessage = message; 
                UpdateSongMessage();
            } catch { /* Ignored */ }
        } 

        IsLooping = db.ReadLooping(Guild.Id) ?? false;
        Volume = db.ReadVolume(Guild.Id) ?? 100;
    }

    private void SaveToDatabase() 
    {
        var db = DatabaseContext;
        if (db.IsGuildInTable(Guild.Id)) {
            // TODO: Updating all values is not good - change it?
            db.UpdateTable(Guild.Id, QueueUpdateChannel?.Id, SongUpdateChannel?.Id, 
                QueueUpdateMessage?.Id, SongUpdateMessage?.Id, Volume, IsLooping);
        } else db.InsertRow(Guild.Id, QueueUpdateChannel?.Id, SongUpdateChannel?.Id, 
            QueueUpdateMessage?.Id, SongUpdateMessage?.Id, Volume, IsLooping);
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
        SaveToDatabase();
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
        SaveToDatabase();
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
        SaveToDatabase();
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
        SaveToDatabase();
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

        for (var i = this.page * page_size; i < tracks.Length && i < (this.page + 1) * page_size; i++)
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

    public Task UpdateQueueMessage()
    {
        if (this.QueueUpdateMessage == null || this.QueueUpdateChannel == null)
            return Task.CompletedTask;

        Task.Run(async () =>
        {
            var message = this.GenerateQueueMessage();
            if (DateTime.Now - this.QueueUpdateMessage.Timestamp < TimeSpan.FromHours(1))
                await this.QueueUpdateMessage.ModifyAsync(message);
            else
            {
                await this.QueueUpdateMessage.DeleteAsync();
                this.QueueUpdateMessage = await this.QueueUpdateChannel.SendMessageAsync(message);
                SaveToDatabase();
            }
        });

        return Task.CompletedTask;
    }

    public DiscordMessageBuilder GenerateSongMessage()
    {
        TimeSpan length;
        string current_song, state, state_btn, author;
        if (this.IsStopped)
        {
            current_song = "Nothing is playing";
            author = "N/A";
            state = "Stopped";
            state_btn = "Play";
            length = TimeSpan.Zero;
        }
        else
        {
            #pragma warning disable CS8602
            author = this.CurrentTrack.Author;
            #pragma warning restore CS8602
            
            state = this.IsPaused ? "Paused" : "Playing";
            state_btn = this.IsPaused ? "Resume" : "Pause";
            current_song = $"[{this.CurrentTrack.Title}]({this.CurrentTrack.Uri})";
            length = this.CurrentTrack.Length;
        }

        var next_song = !this.Queue.TryPeek(out var next) ? "Queue is empty" : $"[{next.Title}]({next.Uri})";

        var embed = new DiscordEmbedBuilder()
            .WithTitle(":question:  |  Song Info: ")
            .WithDescription($":musical_note: **Now playing:** {current_song}\n" +
                             $":play_pause: **Song length:** {length}\n" +
                             $":cinema: **Song Author:** {author}\n" +
                             $":track_next: **Next Song:** {next_song}\n" +
                             $":arrow_right: **Songs in queue:** {this.Queue.Count}\n" +
                             $":information_source: **Song state:** {state}\n" +
                             $":arrows_clockwise: **Song looping:** {this.IsLooping}")
            .WithColor(DiscordColor.Purple);

        var builder = new DiscordMessageBuilder()
        {
            Embed = embed.Build(),
        };

        builder.AddComponents(
            new DiscordButtonComponent(ButtonStyle.Primary, "skip_btn", "Skip"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "loop_btn", "Loop"),
            new DiscordButtonComponent(ButtonStyle.Success, "state_btn", state_btn)
        );

        builder.AddComponents(
            new DiscordButtonComponent(ButtonStyle.Danger, "join_btn", "Join"),
            new DiscordButtonComponent(ButtonStyle.Danger, "stop_btn", "Stop"),
            new DiscordButtonComponent(ButtonStyle.Danger, "leave_btn", "Leave")
        );

        return builder;
    }

    public Task UpdateSongMessage()
    {
        if (this.SongUpdateMessage == null || this.SongUpdateChannel == null)
            return Task.CompletedTask;

        Task.Run(async () =>
        {
            var message = this.GenerateSongMessage();
            if (DateTime.Now - this.SongUpdateMessage.Timestamp < TimeSpan.FromHours(1))
                await this.SongUpdateMessage.ModifyAsync(message);
            else
            {
                await this.SongUpdateMessage.DeleteAsync();
                this.SongUpdateMessage = await this.SongUpdateChannel.SendMessageAsync(message);
                SaveToDatabase();
            }
        });

        return Task.CompletedTask;
    }

    public void StartTimeout()
    {
        this._leaveTimer?.Dispose();
        
        this._leaveTimer = new Timer(
            this.LeaveTimerCallback, null, 
            new TimeSpan(0, 0, 0, 10), Timeout.InfiniteTimeSpan
        );

        this.TimeoutStarted = true;
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

        await this.UpdateSongMessage();
        await this.UpdateQueueMessage();

        if (this._leaveTimer != null) 
            await this._leaveTimer.DisposeAsync();
    }

    public async Task SetVolumeAsync(int volume)
    {
        if (this.Player is not {IsConnected: true})
            return;

        await this.Player.SetVolumeAsync(volume);
        this.Volume = volume;
        SaveToDatabase();
    }

    public async Task DestroyConnectionAsync()
    {
        if (this.Player == null)
            return;

        if (this.Player.IsConnected)
            await this.Player.DisconnectAsync();

        this.Player = null;
        this.CurrentTrack = null;
    }

    private async Task PlaybackFinished(LavalinkGuildConnection con, TrackFinishEventArgs e)
    {
        // var previous = e.Track;
        if (this.IsLooping /*&& !this.IsStopped*/)
            this.Queue.Enqueue(e.Track);

        if (this.SkipEventFire)
        {
            this.SkipEventFire = false;
            return;
        }

        await this.PlayerHandlerAsync();
    }

    public int ClearQueue()
    {
        var count = this.Queue.Count;
        this.Queue.Clear();
        return count;
    }

    private async Task PlayerHandlerAsync()
    {
        this.CurrentTrack = this.Dequeue();
        if (this.CurrentTrack == null || this.Player == null)
            return;

        await this.Player.PlayAsync(this.CurrentTrack);
        await this.Player.SeekAsync(TimeSpan.Zero);
        this.IsPaused = false;

        var _ = Task.Run(async () =>
        {
            await this.UpdateSongMessage();
            await this.UpdateQueueMessage();
        });

    }

    public void Enqueue(LavalinkTrack track)
        => this.Queue.Enqueue(track);

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
    
    public async Task SetAudioFiltersAsync(AudioFilters? filters = null)
    {
        if (filters != null)
            this.Filters = filters;

        // if (this.Filters == null)
        //     return;
        
        if (this.Player is not {IsConnected: true})
            return;
        
        await this.Player.SetAudiofiltersAsync(this.Filters);
    }
    
    public LavalinkTrack? Dequeue() =>
        this.Queue.TryDequeue(out var track) ? track : null;

    public bool ChangeLoopingState()
    {
        this.IsLooping = !this.IsLooping;
        SaveToDatabase();
        return IsLooping;
    }

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

    public async Task SkipAsync(int num)
    {
        if (this.Player is not {IsConnected: true})
            return;

        var tracks = new List<LavalinkTrack>();
        while (--num > 0)
        {
            var track = this.Dequeue();
            
            if (track != null && this.IsLooping)
                tracks.Add(track);
        }
        
        await this.Player.StopAsync();
        this.Enqueue(tracks);
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

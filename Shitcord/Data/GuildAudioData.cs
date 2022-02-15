using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;

namespace Shitcord.Data;

public class GuildAudioData
{
    private LavalinkNodeConnection Lavalink { get; }
    private ConcurrentQueue<LavalinkTrack> Queue { get; }
    public LavalinkTrack? CurrentTrack { get; private set; }
    private DiscordGuild Guild { get; }

    public bool IsLooping { get; private set; }
    public DiscordChannel? Channel => this.Player?.Channel;
    public bool IsConnected => this.Player != null;
    public bool IsPaused { get; private set; }
    public bool IsStopped => this.CurrentTrack == null;
    public int Volume { get; private set; } = 100;

    public bool SkipEventFire { get; set; } = false;

    // Replace this with Filters class
    //public TimeScale Scale { get; set; }
    private LavalinkGuildConnection? Player { get; set; }

    public bool TimeoutStarted { get; private set; }
    private Timer? _timeoutTimer;

    public DiscordChannel? UpdatesChannel { get; set; }
    public DiscordMessage? SongUpdates { get; set; }
    public DiscordMessage? QueueUpdates { get; set; }

    // Change it later (maybe not?)
    public int page = 0;

    public GuildAudioData(DiscordGuild guild, LavalinkNodeConnection lavalink, DiscordClient client)
    {
        this.Guild = guild;
        this.Lavalink = lavalink;
        this.Queue = new ConcurrentQueue<LavalinkTrack>();
    }

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
        this.Player.PlaybackFinished += PlaybackFinished;
    }

    public async Task SetSongUpdate(DiscordChannel channel)
    {
        this.UpdatesChannel = channel;

        if (this.SongUpdates != null)
            await this.SongUpdates.DeleteAsync();

        var mess = GenerateSongMessage();
        this.SongUpdates = await this.UpdatesChannel.SendMessageAsync(mess);
    }

    public async Task SetQueueUpdate(DiscordChannel channel)
    {
        this.UpdatesChannel = channel;

        if (this.QueueUpdates != null)
            await this.QueueUpdates.DeleteAsync();

        var mess = GenerateQueueMessage();
        this.QueueUpdates = await this.UpdatesChannel.SendMessageAsync(mess);
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
        if (this.QueueUpdates == null || this.UpdatesChannel == null)
            return Task.CompletedTask;

        Task.Run(async () =>
        {
            var message = this.GenerateQueueMessage();
            if (DateTime.Now - this.QueueUpdates.Timestamp < TimeSpan.FromHours(1))
                await this.QueueUpdates.ModifyAsync(message);
            else
            {
                await this.QueueUpdates.DeleteAsync();
                this.QueueUpdates = await this.UpdatesChannel.SendMessageAsync(message);
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
            author = this.CurrentTrack.Author;
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
        if (this.SongUpdates == null || this.UpdatesChannel == null)
            return Task.CompletedTask;

        Task.Run(async () =>
        {
            var message = this.GenerateSongMessage();
            if (DateTime.Now - this.SongUpdates.Timestamp < TimeSpan.FromHours(1))
                await this.SongUpdates.ModifyAsync(message);
            else
            {
                await this.SongUpdates.DeleteAsync();
                this.SongUpdates = await this.UpdatesChannel.SendMessageAsync(message);
            }
        });

        return Task.CompletedTask;
    }

    public void StartTimeout()
    {
        this._timeoutTimer?.Dispose();
        this._timeoutTimer = new Timer(
            this.TimerCallback, null, new TimeSpan(0, 0, 10, 0), Timeout.InfiniteTimeSpan);

        this.TimeoutStarted = true;
    }

    private async void TimerCallback(object? sender)
    {
        if (this.Player is not {IsConnected: true}) return;

        await this.StopAsync();

        await this.UpdateSongMessage();
        await this.UpdateQueueMessage();

        if (this._timeoutTimer != null) 
            await this._timeoutTimer.DisposeAsync();
    }

    public void CancelTimeout()
    {
        this._timeoutTimer?.Dispose();
        this.TimeoutStarted = false;
    }

    public async Task SetVolumeAsync(int volume)
    {
        if (this.Player is not {IsConnected: true})
            return;

        await this.Player.SetVolumeAsync(volume);
        this.Volume = volume;
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
        if (this.IsLooping && !this.IsStopped)
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

        await this.UpdateSongMessage();
        await this.UpdateQueueMessage();
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

    public LavalinkTrack? Dequeue() =>
        this.Queue.TryDequeue(out var track) ? track : null;

    public bool ChangeLoopingState()
    {
        this.IsLooping = !this.IsLooping;
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

    // TODO: check if logic is correct
    public async Task SkipAsync(int num)
    {
        if (this.Player is not {IsConnected: true})
            return;

        while (--num > 0)
        {
            var track = this.Dequeue();
            if (this.IsLooping && track != null)
                this.Enqueue(track);
        }
        
        if (!this.IsStopped)
            await this.Player.StopAsync();
        
        // TODO: Add support to skip multiple songs
        //await this.Player.StopAsync();
    }

    public LavalinkTrack[] GetNextTracks()
        => this.Queue.ToArray();

    public TimeSpan? GetTimestamp()
        => this.Player?.CurrentState.PlaybackPosition;
}
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using Microsoft.VisualBasic;
using Shitcord.Data;
using Shitcord.Extensions;
using Shitcord.Services;
using ExtensionMethods = Shitcord.Extensions.ExtensionMethods;

namespace Shitcord.Modules;

// TODO: savequque, loadqueue (number) and listqueues, markovqueue commands

[Description("Audio and music commands")]
public class AudioModule : BaseCommandModule{
    private static HttpClient SharedClient = new();
    private GuildAudioData Data { get; set; }
    private AudioService Audio { get; init; }
    //it's actually pronounced 'yenius'
    private GeniusConfig Genius { get; init; }

#pragma warning disable CS8618
    public AudioModule(AudioService service, DiscordBot bot) {
        Audio = service;
        SharedClient.Timeout = TimeSpan.FromSeconds(5);
        Genius = bot.Config.Genius;
    } 
#pragma warning restore CS8618

    public override async Task BeforeExecutionAsync(CommandContext ctx)
    {
        Data = Audio.GetOrAddData(ctx.Guild);
        await base.BeforeExecutionAsync(ctx);
    }

    public override async Task AfterExecutionAsync(CommandContext ctx)
    {
        Data.SongRequiresUpdate = true;
        Data.QueueRequiresUpdate = true;
        await base.AfterExecutionAsync(ctx);
    }

    [Command("join"), Aliases("j")]
    [Description("Joins the voice channel")]
    public async Task JoinCommand(CommandContext ctx, 
        [Description("Name of the channel to join")] String? channel_name = null
    ) {
        var channel = channel_name == null
            ? ctx.Member?.VoiceState?.Channel
            : ctx.Guild.Channels.First(x =>
                x.Value.Name.Contains(channel_name, StringComparison.OrdinalIgnoreCase) &&
                x.Value.Type == ChannelType.Voice
            ).Value;

        if (channel is null)
            return;

        await this.Data.CreateConnectionAsync(channel);
    }

    [Command("stop")]
    [Description("Stops the currently playing track")]
    public async Task StopCommand(CommandContext ctx)
        => await this.Data.StopAsync();

    [Command("reset")]
    [Description("Stops current track and clears the queue")]
    public async Task ResetCommand(CommandContext ctx)
    {
        this.Data.ClearQueue();
        await this.Data.StopAsync();
    }

    [Command("play"), Aliases("p")]
    [Description("Stops current track, searches for the specified song and plays it")]
    public async Task PlayCommand(CommandContext ctx,
        [RemainingText, Description("Name of the song, song uri or playlist uri")]
        string? message = null
    ) {
        var channel = ctx.Member?.VoiceState?.Channel;

        if (channel is not null)
            await this.Data.CreateConnectionAsync(channel);

        var msgBuilder = new DiscordMessageBuilder();

        if (!String.IsNullOrWhiteSpace(message)) {
            IEnumerable<LavalinkTrack> tracks;
            if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
                tracks = await this.Audio.GetTracksAsync(uri);
            else tracks = await this.Audio.GetTracksAsync(message);

            var lavalinkTracks = tracks.ToArray();
            if (lavalinkTracks.Length == 0)
                throw new CommandException("Could not find requested song");

            if (lavalinkTracks.Length > 1) {
                msgBuilder.AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle(":thumbsup:  |  Enqueued: ")
                    .WithDescription($"Enqueued {lavalinkTracks.Length} songs")
                    .WithColor(DiscordColor.Purple));
            } 

            this.Data.EnqueueFirst(lavalinkTracks);
        }

        await this.Data.PlayAsync();

        if (this.Data.CurrentTrack != null)
        {
            msgBuilder.AddEmbed(new DiscordEmbedBuilder()
                .WithTitle(":musical_note:  |  Now playing: ")
                .WithDescription($"[{this.Data.CurrentTrack.Title}]({this.Data.CurrentTrack.Uri})")
                .WithColor(DiscordColor.Purple));
        }
        else throw new CommandException("Failed to play the song");

        await ctx.Channel.SendMessageAsync(msgBuilder);
    }

    [Command("songupdates"), Aliases("su")]
    [Description("Creates an interactive embed that displays music info")]
    public async Task SetSongUpdatesCommand(CommandContext ctx, 
        [Description("Channel in which the embed should be created")] 
        DiscordChannel? channel = null
    ) {
        await ctx.Message.DeleteAsync();
        await Data.SetSongUpdate(channel ?? ctx.Channel);
    }

    [Command("queueupdates"), Aliases("qu")]
    [Description("Creates an interactive embed that displays song queue")]
    public async Task SetQueueUpdatesCommand(CommandContext ctx,
        [Description("Channel in which the embed should be created")] 
        DiscordChannel? channel = null
    ) {
        await ctx.Message.DeleteAsync();
        await Data.SetQueueUpdate(channel ?? ctx.Channel);
    }

    [Command("destroysongupdates"), Aliases("dsu")]
    [Description("Removes the interactive embed that displays music info")]
    public async Task DestroySongUpdatesCommand(CommandContext ctx) {
        await ctx.Message.DeleteAsync();
        await Data.DestroySongUpdate();
    }

    [Command("destroyqueueupdates"), Aliases("dqu")]
    [Description("Removes the interactive embed that displays song queue")]
    public async Task DestroyQueueUpdatesCommand(CommandContext ctx) {
        await ctx.Message.DeleteAsync();
        await Data.DestroyQueueUpdate();
    }

    [Command("queue"), Aliases("q")]
    [Description("Enqueues specified track or playlist")]
    public async Task QueueCommand(
        CommandContext ctx,
        [RemainingText, Description("Name of the song, song uri or playlist uri")] string message
    ) {
        if (String.IsNullOrWhiteSpace(message))
            throw new CommandException("Name of song cannot be an empty string");

        IEnumerable<LavalinkTrack> tracks;
        if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
            tracks = await this.Audio.GetTracksAsync(uri);
        else tracks = await this.Audio.GetTracksAsync(message);

        var lavalinkTracks = tracks.ToList();

        var embed = new DiscordEmbedBuilder();
        if (lavalinkTracks.Any()) {
            var description = lavalinkTracks.Count == 1
                ? $"[{lavalinkTracks.First().Title}]({lavalinkTracks.First().Uri})"
                : $"Enqueued {lavalinkTracks.Count} songs";

            embed.WithTitle(":thumbsup:  |  Enqueued: ")
                .WithDescription(description)
                .WithColor(DiscordColor.Purple);

            this.Data.Enqueue(lavalinkTracks);
        }
        else throw new CommandException("Failed to enqueue. Zero tracks found");

        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("queuefirst"), Aliases("qf")]
    [Description("Enqueues a song (or playlist) on top of a current queue")]
    public async Task QueueFirstCommand(
        CommandContext ctx,
        [RemainingText, Description("Name of the song, song uri or playlist uri")] string message
    ) {
        IEnumerable<LavalinkTrack> tracks;
        if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
            tracks = await this.Audio.GetTracksAsync(uri);
        else tracks = await this.Audio.GetTracksAsync(message);

        var lavalinkTracks = tracks.ToList();

        var embed = new DiscordEmbedBuilder();
        if (lavalinkTracks.Any()) {
            var description = lavalinkTracks.Count == 1
                ? $"[{lavalinkTracks.First().Title}]({lavalinkTracks.First().Uri})"
                : $"Enqueued {lavalinkTracks.Count} songs";

            embed.WithTitle(":thumbsup:  |  Enqueued first: ")
                .WithDescription(description)
                .WithColor(DiscordColor.Purple);

            this.Data.EnqueueFirst(lavalinkTracks);
        }
        else throw new CommandException("Failed to enqueue");

        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("queuemany"), Aliases("qm")]
    [Description("Enqueues multiple songs")]
    public async Task QueueManyCommand(
        CommandContext ctx, [RemainingText, Description(
            "Multiple song names (ex. >>qm \"Doin Your Mom\" \"Burning memories\")"
        )] params string[] message
    ) {
        var tracks = new List<LavalinkTrack>();
        foreach (var s in message) {
            var foundTracks = (await this.Audio.GetTracksAsync(s)).ToList();
            if (foundTracks.Any())
                tracks.Add(foundTracks.First());
        }

        var embed = new DiscordEmbedBuilder();
        if (tracks.Any()) {
            string description = $"Enqueued {tracks.Count} songs";

            embed.WithTitle(":thumbsup:  |  Enqueued: ")
                .WithDescription(description)
                .WithColor(DiscordColor.Purple);

            this.Data.Enqueue(tracks);
        } else throw new CommandException("Failed to enqueue");

        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("queuefile")]
    [Description("Enqueues all songs from a file")]
    public async Task QueueFileCommand(CommandContext ctx) {
        var attachement = ctx.Message.Attachments.FirstOrDefault();
        if (attachement == null) {
            throw new CommandException("You must attach a file.");
        }
        
        string content = await SharedClient.GetStringAsync(attachement.Url);
        // await ctx.RespondAsync(content);
        var songs = content.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        var tracks = new List<LavalinkTrack>();
        foreach (var s in songs) {
            var foundTracks = (await this.Audio.GetTracksAsync(s)).ToList();
            if (foundTracks.Any())
                tracks.Add(foundTracks.First());
        }

        var embed = new DiscordEmbedBuilder();
        if (tracks.Any()) {
            string description = $"Enqueued {tracks.Count} songs";

            embed.WithTitle(":thumbsup:  |  Enqueued: ")
                .WithDescription(description)
                .WithColor(DiscordColor.Purple);

            Data.Enqueue(tracks);
        } else throw new CommandException("Failed to enqueue");

        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("queuesort"), Aliases("qs")]
    [Description("Sorts the queue")]
    public async Task QueueSortCommand(
        CommandContext ctx, 
        [Description("0 to sort by song title, 1 to sort by song length")] int mode = 0, 
        [Description("true or false, ascending sort by default")] bool ascending = true
    ) {
        switch (mode) {
            case 0: {
                Data.SortByTitle(ascending);
            } break;
            case 1: {
                Data.SortByLenght(ascending);
            } break;
            default: throw new CommandException(
                "Incorrect mode selected. Type >>help mode for more info");
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle(":thumbsup:  |  Queue Sorted");
        embed.WithColor(DiscordColor.Purple);
        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("revertqueue"), Aliases("rq")]
    [Description("Reverts queue to its previous stage")]
    public Task RevertQueueCommand(CommandContext ctx) {
        Data.RevertQueue();
        return Task.CompletedTask;
    }

    [Command("listqueue"), Aliases("lq")]
    [Description("Lists next 10 songs in the queue")]
    public async Task ListQueueCommand(
        CommandContext ctx, 
        [Description("Page number (single page is 10 songs)")] int page = 1
    ) {
        page -= 1;

        var tracks = this.Data.GetNextTracks();

        const int page_size = 10;
        // int page_count = tracks.Length / page_size;
        int page_count = ((int)Math.Ceiling(tracks.Length / (float)page_size)) - 1;
        if (page_count < 0) page_count = 0;

        if (page < 0) page = 0;
        else if (page > page_count)
            page = page_count;

        string description = "";

        for (var i = page * page_size; i < tracks.Length && i < (page + 1) * page_size; i++)
            description += $"{i + 1}. [{tracks[i].Title}]({tracks[i].Uri})\n";

        var embed = new DiscordEmbedBuilder();
        switch (tracks.Length) {
            case > page_size: {
                if (tracks.Length - page * page_size > page_size) {
                    embed.WithFooter(
                        $". . . and {tracks.Length - page_size * (page + 1)} more " + 
                        $"| Page {page + 1} / {page_count + 1}"
                    );
                    // embed.WithFooter($". . . and {tracks.Length - page * page_size} more");
                } else embed.WithFooter($"Page {page + 1} / {page_count + 1}");
                } break;
            case 0: {
                description = "Queue is empty";
            } break;
        }

        embed.WithTitle(":question:  |  Next tracks in the queue: ")
            .WithDescription(description)
            .WithColor(DiscordColor.Purple);

        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("clearqueue"), Aliases("cq")]
    [Description("Clears the song queue")]
    public async Task ClearQueueCommand(CommandContext ctx)
    {
        var embed = new DiscordEmbedBuilder();
        var count = this.Data.ClearQueue();
        embed.WithTitle(":thumbsup:  |  Queue cleared: ");
        embed.WithDescription($"Cleared `{count}` songs");
        embed.WithColor(DiscordColor.Purple);
        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("loop")]
    [Description("Sets the looping mode")]
    public async Task LoopCommand(
        CommandContext ctx, [Description(
            "None (looping disabled), " +
            "Queue (repeat song queue), " +
            "Song (repeat currently playing song), " +
            "Shuffle (suffle song randomly into the queue after it ends)"
        )] string? mode = null
    ) {
        if (mode is null) {
            Data.Looping = Data.Looping switch {
                LoopingMode.None => LoopingMode.Queue,
                _ => LoopingMode.None,
            };
        } else if (Enum.TryParse(typeof(LoopingMode), mode, true, out var looping) && 
                   Enum.IsDefined(typeof(LoopingMode), looping)) {
            Data.Looping = (LoopingMode)looping;
        } else throw new CommandException(
            "Incorrect looping mode, correct modes are:\n" + 
            "`None`, `Queue`, `Song`, `Shuffle`"
        );

        if (Data.Looping is LoopingMode.None)
            await ctx.RespondAsync($"Looping is now disabled (mode: `{Data.Looping}`)");
        else await ctx.RespondAsync($"Looping is now enabled (mode: `{Data.Looping}`)");
    }

    [Command("shuffle")]
    [Description("Shuffles the queue")]
    public async Task ShuffleCommand(CommandContext ctx)
    {
        this.Data.Shuffle();
        await ctx.RespondAsync("Queue shuffled");
    }

    [Command("nightcore")]
    [Description("Switches on/off nightcore")]
    public async Task NightcoreCommand(CommandContext ctx, string state)
    {
        switch (state)
        {
        case "on":
            Data.Filters.Timescale = new TimeScale {
                Speed = 1.0,
                Pitch = 1.05,
                Rate = 1.30
            };
            break;
        case "reset":
        case "off":
            this.Data.Filters.Timescale = null;
            break;
        default:
            throw new CommandException(
                "Incorrect usage, please specify nightcore state (ex. >>nightcore on)"
            );
        }

        await this.Data.SetAudioFiltersAsync();
    }

    [Command("skip"), Aliases("s")]
    [Description("Skips tracks")]
    public async Task SkipCommand(
        CommandContext ctx, [Description("Number of tracks to skip")] int count = 1
    ) => await this.Data.SkipAsync(count);

    [Command("prev"), Aliases("previous")]
    [Description("Plays previous track")]
    public async Task PreviousCommand(CommandContext ctx)
        => await this.Data.PreviousAsync();

    [Command("seek")]
    [Description("Seeks track to specified position")]
    public async Task SeekCommand(
        CommandContext ctx, [Description(
            "Accepted formats: <+/->d:h:m:s, <+/->d.h.m.s, <+/-><number><suffix>\n" + 
            "Example inputs: +0:23, -00:59:59, +30h, -40m, +2s, 9s, +2d (days), -3m 40s, 2h 30s"
        )] [RemainingText] SeekStamp stamp
    ) {
        var track_pos = Data.GetTimestamp();
        if (track_pos is null)
            throw new CommandException("Cannot seek - nothing is playing");

        switch (stamp.sign) {
            case SeekSign.Plus:
                await Data.SeekAsync(track_pos.Value + stamp.seek_time);
                break;
            case SeekSign.Minus:
                await Data.SeekAsync(track_pos.Value - stamp.seek_time);
                break;
            case SeekSign.None:
                await Data.SeekAsync(stamp.seek_time);
                break;
        }
    }

    [Command("remove"), Aliases("r")] [Description(
        "Removes a song from the queue (input nothing to skip and remove currently playing song)")]
    public async Task RemoveCommand(CommandContext ctx,
        [Description("Index of an enqueued song (see >>lq to list songs and their indexes)")]
        int index = 0, [Description("Number of tracks to be removed")] int count = 1) {
        // Skip and remove current song when index is zero
        if (index == 0) {
            await Data.SkipAsync(1, true);
            return;
        }

        switch (count) {
            case 1: {
                var track = this.Data.Remove(--index);

                if (track == null)
                    throw new CommandException("Could not remove the track");

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":thumbsup:  |  Track removed: ")
                    .WithDescription($"[{track.Title}]({track.Uri})\n")
                    .WithColor(DiscordColor.Purple);

                await ctx.Channel.SendMessageAsync(embed.Build());
            } break;
            case <= 0: 
                throw new CommandException("Count must be greater than 0");
            default: {
                var tracks = this.Data.RemoveRange(--index, count).Count();

                if (tracks == 0)
                    throw new CommandException("Could not remove the tracks");

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":thumbsup:  |  Tracks removed: ")
                    .WithDescription($"Removed {tracks} tracks")
                    .WithColor(DiscordColor.Purple);

                await ctx.Channel.SendMessageAsync(embed.Build());
            } break;
        }
    }


    [Command("pause")]
    [Description("Pauses current track")]
    public async Task PauseCommand(CommandContext ctx)
        => await this.Data.PauseAsync();

    [Command("nowplaying"), Aliases("current", "np")]
    [Description("Displays info about current song")]
    public async Task NowPlayingCommand(CommandContext ctx)
    {
        if (this.Data.CurrentTrack != null)
        {
            var timestamp = ExtensionMethods.StripMilliseconds(
                this.Data.GetTimestamp() ?? TimeSpan.Zero
            );

            var embed = new DiscordEmbedBuilder()
                .WithTitle(":musical_note:  |  Now playing: ")
                .WithDescription(
                    $"[{this.Data.CurrentTrack.Title}]({this.Data.CurrentTrack.Uri})\n" +
                    $":play_pause: Current timestamp: {timestamp}\n" +
                    $":play_pause: Song length: {this.Data.CurrentTrack.Length}\n" +
                    $":play_pause: Song Author: {this.Data.CurrentTrack.Author}\n"
                )
                .WithColor(DiscordColor.Purple);
            await ctx.Channel.SendMessageAsync(embed.Build());
        }
        else throw new CommandException("Currently nothing is playing");
    }

    [Command("resume")]
    [Description("Resumes paused song")]
    public async Task ResumeCommand(CommandContext ctx)
        => await Data.ResumeAsync();

    [Command("volume")]
    [Description("Sets volume level of a command")]
    public async Task VolumeCommand(CommandContext ctx, 
        [Description("volume level (greater than 0)")] int level
    ) => await Data.SetVolumeAsync(level);

    [Command("leave")]
    [Description("Leaves the voice channel")]
    public async Task LeaveCommand(CommandContext ctx)
        => await Data.DestroyConnectionAsync();

    [Command("autoleave")]
    [Description("Sets timeout for the auto leave when noone is in a voice channel")]
    public async Task AutoLeaveTimeoutCommand(CommandContext ctx, TimeSpan timeout) 
    {
        Data.LeaveTimeout = timeout;
        Data.DatabaseUpdateLeavetimeout();

        await ctx.RespondAsync($"Leave timeout now set to: {Data.LeaveTimeout}");
    }


    [Command("autojoin")]
    [Description("Sets flag for the auto join when noone when someone enters a voice channel")]
    public async Task AutoJoinCommand(CommandContext ctx, bool? flag = null) 
    {
        if (flag is null)
            Data.AutoJoinChannel = !Data.AutoJoinChannel;
        else Data.AutoJoinChannel = flag.Value;

        Data.DatabaseUpdateAutoJoin();

        await ctx.RespondAsync($"Autojoin is now set to: {Data.AutoJoinChannel}");
    }

    [Command("autoresume")]
    [Description("Sets flag for the auto resume when noone when someone enters a voice channel")]
    public async Task AutoResumeCommand(CommandContext ctx, bool? flag = null)
    {
        if (flag is null)
            Data.ResumeOnAutoJoin = !Data.ResumeOnAutoJoin;
        else Data.ResumeOnAutoJoin = flag.Value;

        Data.DatabaseUpdateAutoJoin();

        await ctx.RespondAsync($"Autoresume is now set to: {Data.ResumeOnAutoJoin}");
    }

    [Group("filters")]
    [Description("Track filter commands")]
    public class FilterModule : BaseCommandModule
    {
        private GuildAudioData Data { get; set; }
        private AudioService Audio { get; init; }

    #pragma warning disable CS8618
        public FilterModule(AudioService service) => this.Audio = service;
    #pragma warning restore CS8618

        public override async Task BeforeExecutionAsync(CommandContext ctx)
        {
            this.Data = this.Audio.GetOrAddData(ctx.Guild);
            await base.BeforeExecutionAsync(ctx);
        }

        public override async Task AfterExecutionAsync(CommandContext ctx)
        {
            await this.Data.SetAudioFiltersAsync();
            await base.AfterExecutionAsync(ctx);
        }

        [Command("reset")]
        public Task ResetFiltersCommand(CommandContext ctx) 
        {
            this.Data.Filters = new AudioFilters();
            return Task.CompletedTask;
        }

        [Command("get")]
        public async Task GetFiltersCommand(CommandContext ctx)
            => await ctx.Channel.SendMessageAsync($"```json\n{this.Data.Filters.GetJson()}\n```");

        [Command("set")]
        public Task SetFiltersCommand(CommandContext ctx, [RemainingText] string json)
        {
            int cs1 = json.IndexOf("```", StringComparison.Ordinal) + 5;
            cs1 = json.IndexOf('\n', cs1) + 1;
            int cs2 = json.LastIndexOf("```", StringComparison.Ordinal);

            if (cs1 is -1 || cs2 is -1) {
                cs1 = 0;
                cs2 = json.Length;
            }

            string filters_string = json.Substring(cs1, cs2 - cs1);

            var output = filters_string.GetAudioFilters();
            if (output != null)
                this.Data.Filters = output;

            return Task.CompletedTask;
        }

        [Command("example")]
        public async Task SetExampleFiltersCommand(CommandContext ctx)
        {
            var example = new AudioFilters
            {
                Karaoke = new Karaoke
                {
                    Level = 1,
                    MonoLevel = 1,
                    FilterBand = 220,
                    FilterWidth = 100
                },

                Timescale = new TimeScale
                {
                    Speed = 1,
                    Pitch = 1,
                    Rate = 1
                },

                Tremolo = new Tremolo
                {
                    Frequency = 2.0,
                    Depth = 0.5
                },

                Vibrato = new Vibrato
                {
                    Frequency = 2.0,
                    Depth = 0.5
                },

                Rotation = new Rotation {RotationFreq = 0},

                Distortion = new Distortion
                {
                    SinOffset = 0,
                    SinScale = 1,
                    CosOffset = 0,
                    CosScale = 1,
                    TanOffset = 0,
                    TanScale = 1,
                    Offset = 0,
                    Scale = 1
                },

                Lowpass = new LowPass{Smoothing = 20.0},

                Channelmix = new ChannelMix
                {
                    LeftToLeft = 1,
                    LeftToRight = 0,
                    RightToLeft = 0,
                    RightToRight = 1
                }
            };

            await ctx.Channel.SendMessageAsync($"```json\n{example.GetJson()}\n```");
        }

        [Command("karaoke"), Priority(0)]
        public Task KaraokeLavaCommand(CommandContext ctx) 
        {
            this.Data.Filters.Karaoke = null;
            return Task.CompletedTask;
        }

        [Command("timescale"), Priority(0)]
        public Task TimescaleLavaCommand(CommandContext ctx) 
        {
            this.Data.Filters.Timescale = null;
            return Task.CompletedTask;
        }

        [Command("tremolo"), Priority(0)]
        public Task TremoloLavaCommand(CommandContext ctx) 
        {
            this.Data.Filters.Tremolo = null;
            return Task.CompletedTask;
        }

        [Command("vibrato"), Priority(0)]
        public Task VibratoLavaCommand(CommandContext ctx) 
        {
            this.Data.Filters.Vibrato = null;
            return Task.CompletedTask;
        }

        [Command("rotation"), Priority(0)]
        public Task RotationLavaCommand(CommandContext ctx) 
        {
            this.Data.Filters.Rotation = null;
            return Task.CompletedTask;
        }

        [Command("lowpass"), Priority(0)]
        public Task LowPassLavaCommand(CommandContext ctx) 
        {
            this.Data.Filters.Lowpass = null;
            return Task.CompletedTask;
        }

        [Command("channelmix"), Priority(0)]
        public Task ChannelMixLavaCommand(CommandContext ctx) 
        {
            this.Data.Filters.Channelmix = null;
            return Task.CompletedTask;
        }

        [Command("karaoke")]
        public Task KaraokeLavaCommand(
            CommandContext ctx, double level = 1, double monoLevel = 1,
            double filterBand = 220, double filterWidth = 100
        ) {
            this.Data.Filters.Karaoke = new Karaoke {
                Level = level,
                MonoLevel = monoLevel,
                FilterBand = filterBand,
                FilterWidth = filterWidth
            };
            return Task.CompletedTask;
        }

        [Command("timescale")]
        [Description("Applies audio effects such as speed, pitch, rate")]
        public Task TimescaleLavaCommand(CommandContext ctx,
          [Description("speed")] double speed = 1.0,
          [Description("pitch")] double pitch = 1.0,
          [Description("rate")]  double rate  = 1.0
        ) {
            this.Data.Filters.Timescale = new TimeScale {
                Pitch = pitch,
                Rate = rate,
                Speed = speed
            };
            return Task.CompletedTask;
        }

        [Command("tremolo")]
        public Task TremoloLavaCommand(
            CommandContext ctx, double frequency = 2.0, double depth = 0.5
        ) {
            this.Data.Filters.Tremolo = new Tremolo {
                Frequency = frequency,
                Depth = depth,
            };
            return Task.CompletedTask;
        }

        [Command("vibrato")]
        public Task VibratoLavaCommand(
            CommandContext ctx, double frequency = 2.0, double depth = 0.5
        ) {
            this.Data.Filters.Vibrato = new Vibrato {
                Frequency = frequency,
                Depth = depth
            };
            return Task.CompletedTask;
        }

        [Command("rotation")]
        public Task RotationLavaCommand(CommandContext ctx, double rotationFreq = 0.0)
        {
            this.Data.Filters.Rotation = new Rotation {RotationFreq = rotationFreq};
            return Task.CompletedTask;
        }

        [Command("distortion")]
        public Task DistortionLavaCommand(
            CommandContext ctx, double sinOffset = 0, double sinScale = 1,
            double cosOffset = 0, double cosScale = 1, double tanOffset = 0,
            double tanScale = 1, double offset = 0, double scale = 1
        ) {
            this.Data.Filters.Distortion = new Distortion {
                SinOffset = sinOffset,
                SinScale = sinScale,
                CosOffset = cosOffset,
                CosScale = cosScale,
                TanOffset = tanOffset,
                TanScale = tanScale,
                Offset = offset,
                Scale = scale,
            };
            return Task.CompletedTask;
        }

        [Command("lowpass")]
        public Task LowPassLavaCommand(CommandContext ctx, double smoothing = 2.0)
        {
            this.Data.Filters.Lowpass = new LowPass {Smoothing = smoothing};
            return Task.CompletedTask;
        }

        [Command("channelmix")]
        public Task ChannelMixLavaCommand(
            CommandContext ctx, double leftToLeft = 1, double leftToRight = 0, 
            double rightToLeft = 0, double rightToRight = 1
        ) {
            this.Data.Filters.Channelmix = new ChannelMix {
                LeftToLeft = leftToLeft,
                LeftToRight = leftToRight,
                RightToLeft = rightToLeft,
                RightToRight = rightToRight
            };
            return Task.CompletedTask;
        }
    }
    
    [Command("lyrics"), Aliases("lyr")]
    [Description("Fetches currently lyrics of the song that's currently being played")]
    public async Task LyricsCommand(CommandContext ctx){
        if (Data.CurrentTrack?.Title == null){
            return;
        }
        string songName = Data.CurrentTrack.Title;
        string author = Data.CurrentTrack.Author;
        bool trackAuthorExists = !string.IsNullOrEmpty(Data.CurrentTrack.Author);
        
        songName = DeGeniusify(songName);
        author = PerformHacksOnAuthor(author);
        var songs = await retrieveSongs(songName);
        
        if (!trackAuthorExists){
            goto defer;
        }
        
        string nameWithAuthor = songName + " " + author;
        var withAuthorSongs = await retrieveSongs(nameWithAuthor);
        songs.AddRange(withAuthorSongs);

        if (!StringHasTwoParts(author)){
            goto defer;
        }
        string firstPart = author[..author.IndexOf(' ')];
        var onePartAuthorSongs = await retrieveSongs(songName + " " + firstPart);
        songs.AddRange(onePartAuthorSongs);

        foreach (var song in songs){
            if (song.full_title == null){
                continue;
            }
            song.full_title = DeGeniusify(song.full_title);
        }
        
        defer:
        SongInfo? mostAccurate = trackAuthorExists ? 
            SelectMostAccurate(songName + " " + author, songs) : 
            SelectMostAccurate(songName, songs);

        if (mostAccurate?.lyrics_url == null) 
            return;
        Console.WriteLine(mostAccurate);
        DiscordEmbed embed = EmbedSong(mostAccurate);
        
        //scrape lyrics request
        if (!mostAccurate.lyrics_url.StartsWith("https://genius.com")){
            return;
        }
        await ctx.Channel.SendMessageAsync(embed);
        
        var webpageRequest = new HttpRequestMessage {
            RequestUri = new Uri(mostAccurate.lyrics_url),
            Method = HttpMethod.Get,
        };
        webpageRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        webpageRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 Gecko/20100101");
        webpageRequest.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.7));
        HttpResponseMessage pageResponse = await SharedClient.SendAsync(webpageRequest);
        if (pageResponse.StatusCode != HttpStatusCode.OK){
            //exception
            return;
        }
        byte[] bytes = await pageResponse.Content.ReadAsByteArrayAsync();
        var pageContent = Encoding.UTF8.GetString(bytes);
        string lyrics = ScrapeLyrics(pageContent);

        //Console.WriteLine(lyrics);
        var interactivity = ctx.Client.GetInteractivity();
        if (ctx.Member is null){
            throw new CommandException("Member null, failed to post message");
        }
        await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, interactivity.GeneratePagesInEmbed(lyrics, SplitType.Line),
            PaginationBehaviour.WrapAround, ButtonPaginationBehavior.DeleteMessage);
    }

    private static DiscordEmbed EmbedSong(SongInfo song){
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
        string? title = song.full_title;
        if (title != null){
            if(title.Length > 256){
                title = title[..256];
            }
            embed.Title = title;
        }

        if (song.thumbnail_url != null){
            embed.WithThumbnail(song.thumbnail_url);
        }

        embed.Description = (song.release?? "Unknown release") +
            (song.lyrics_url == null ? "" : "\n" + song.lyrics_url);
        return embed.Build();
    }

    private static string PerformHacksOnAuthor(string author){
        StringBuilder str = new StringBuilder(author);
        str.Replace("Topic", "");
        str.Replace('-', ' ');
        return str.ToString().Trim();
    }

    private static bool StringHasTwoParts(string song){
        if (song.Length < 3){
            return false;
        }
        int anyWhitespace = song.IndexOf(' ', 1);
        return anyWhitespace != -1;
    }

    private async Task<List<SongInfo>> retrieveSongs(string songName){
        var searchRequest = new HttpRequestMessage {
            RequestUri = new Uri($"https://api.genius.com/search?q={songName}"),
            Method = HttpMethod.Get,
        };
        searchRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Genius.Token);
        HttpResponseMessage response = await SharedClient.SendAsync(searchRequest);
        if (response.StatusCode != HttpStatusCode.OK){
            //exception
            return new List<SongInfo>();
        }
        string content = await response.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromSeconds(2));
        var json = JsonNode.Parse(content);
        var hits = json?["response"]?["hits"]?.AsArray();
        if (hits is null) 
            return new List<SongInfo>();
        
        var songs = new List<SongInfo>();
        foreach (var hit in hits) {
            if (hit is null){
                continue;
            }
            var result = hit["result"];
            var song = result.Deserialize<SongInfo?>();
            if (song != null){
                song.fixEncoding();
                songs.Add(song);
            }
        }

        return songs;
    }

    private static string DeGeniusify(string songName){
        StringBuilder str = new StringBuilder();
        bool inRoundBrackets = false, inSquareBrackets = false;
        int len = songName.Length;
        for (var i = 0; i < len; i++){
            var chr = songName[i];
            switch (chr){
                case '-':
                    if (i == 0 || i == len-1){
                        continue;
                    }

                    if (songName[i-1] != ' '){
                        str.Append(' ');
                    }
                    str.Append('-');
                    if (i+1 < len && songName[i+1] != ' '){
                        str.Append(' ');
                    }

                    break;
                case '[':
                    inSquareBrackets = true;
                    break;
                case '(':
                    inRoundBrackets = true;
                    break;
                case ')':
                    inRoundBrackets = false;
                    break;
                case ']':
                    inSquareBrackets = false;
                    break;
                default:
                    if (!inRoundBrackets && !inSquareBrackets)
                        str.Append(chr);
                    break;
            }
        }

        str.Replace("Official", "");
        str.Replace("Video", "");
        str.Replace("Music", "");
        return str.ToString().Trim();
    }

    private static string ScrapeLyrics(string page){
        if (page == null) 
            return "";
        const string CONTAINER = "Lyrics__Container-sc";
        const string INSTRUMENTAL = "This song is an instrumental";
        int fakeContainer = page.IndexOf(CONTAINER, StringComparison.Ordinal);
        if(fakeContainer == -1){
            return page.Contains(INSTRUMENTAL) ? "This song is an instrumental" : "";
        }
        int lyricContainer = page.IndexOf(CONTAINER, fakeContainer + 1, StringComparison.Ordinal);
        int textStart = page.IndexOf('>', lyricContainer);
        StringBuilder lyrics = new StringBuilder();
        int angleBrackets = 0, divCounter = 1;
        bool spaced = false;
        for (int i = textStart + 1; i < page.Length; i++){
            switch (page[i]){
                case '<':
                    //open + close
                    if (page.Substring(i+1, 2) == "br"){
                        i += 4;
                        if (page[i + 3] == '/'){
                            i++;
                        }
                        if(!spaced)
                            lyrics.Append('\n');
                        spaced = true;
                        continue;
                    }
                    if (page.Substring(i+1, 4) == "/div"){
                        if (divCounter == 0){
                            goto exitLoop;
                        }
                        i += 4;
                        divCounter--;
                    }else if (page.Substring(i + 1, 3) == "div"){
                        i += 3;
                        divCounter++;
                    }
                    angleBrackets++;
                    break;
                case '&':
                    if (page.Substring(i + 1, 4) == "amp;"){
                        lyrics.Append('&');
                        continue;
                    }
                    switch (page.Substring(i + 1, 5)){
                        case "#x27;":
                        case "apos;":
                            i += 5;
                            lyrics.Append('\'');
                            break;
                        case "quot;":
                            i += 5;
                            lyrics.Append('"');
                            break;
                        default:
                            lyrics.Append('&');
                            break;
                    }
                    break;
                case '\\':
                case '\n':
                case ';':
                    break;
                case '>':
                    if (angleBrackets == 0){
                        goto exitLoop;
                    }
                    angleBrackets--;
                    break;
                default:
                    if (angleBrackets == 0){
                        spaced = false;
                        lyrics.Append(page[i]);
                    }
                    break;
            }
        }
        exitLoop:
        lyrics.Replace("You might also like", "");
        stripDigits(lyrics, 3);
        if(endsWith(lyrics, "Embed")){
            lyrics.Length -= 5;
        }
        stripDigits(lyrics, 4);

        return lyrics.ToString();
    }
    
    private static void stripDigits(StringBuilder str, int quantity){
        if(quantity <= 0) 
            return;
        int currLen = str.Length;
        for (int i = currLen-1; i >= currLen - quantity && i > -1; i--){
            if(char.IsDigit(str[i])){
                str.Length = i;
            }
        }
    }

    private static bool endsWith(StringBuilder str, string seq){
        int mainLen = str.Length;
        int start = str.Length - seq.Length;
        if(start < 0){
            return false;
        }
        for (int i = start, j = 0; i < mainLen; i++, j++){
            if(str[i] != seq[j]){
                return false;
            }
        }
        return true;
    }
    
    private static SongInfo? SelectMostAccurate(string name, List<SongInfo> songs){
        int len = songs.Count;
        if(len == 0){
            return null;
        }

        float[] accuracies = new float[len];
        float max = 0;
        int index = -1;
        for (int i = 0; i < len; i++){
            string? fullTitle = songs[i].full_title;
            if (fullTitle == null)
                continue;
            
            accuracies[i] = StringMatching.Accuracy(name, fullTitle);
            if(accuracies[i] > max){
                max = accuracies[i];
                index = i;
            }
        }
        if(index == -1){
            index = 0;
        }

        Console.WriteLine("accc");
        return songs[index];
    }
}
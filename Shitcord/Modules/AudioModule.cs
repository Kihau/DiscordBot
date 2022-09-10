using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using Shitcord.Data;
using Shitcord.Extensions;
using Shitcord.Services;

namespace Shitcord.Modules;

// TODO: savequque, loadqueue (number) and listqueues commands

[Description("Audio and music commands")]
public class AudioModule : BaseCommandModule
{
    private GuildAudioData Data { get; set; }
    private AudioService Audio { get; init; }

#pragma warning disable CS8618
    public AudioModule(AudioService service) => this.Audio = service;
#pragma warning restore CS8618

    public override async Task BeforeExecutionAsync(CommandContext ctx)
    {
        this.Data = this.Audio.GetOrAddData(ctx.Guild);
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

        if (channel == null)
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

        if (channel != null)
            await this.Data.CreateConnectionAsync(channel);

        var msgBuilder = new DiscordMessageBuilder();

        if (!String.IsNullOrWhiteSpace(message))
        {
            IEnumerable<LavalinkTrack> tracks;
            if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
                tracks = await this.Audio.GetTracksAsync(uri);
            else tracks = await this.Audio.GetTracksAsync(message);

            var lavalinkTracks = tracks.ToArray();
            if (lavalinkTracks.Length == 0)
                throw new CommandException("Could not find requested song");

            if (lavalinkTracks.Length > 1)
            {
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
        if (channel is null)
            await ctx.Message.DeleteAsync();

        channel ??= ctx.Channel;
        await this.Data.SetSongUpdate(channel);
    }

    [Command("queueupdates"), Aliases("qu")]
    [Description("Creates an interactive embed that displays song queue")]
    public async Task SetQueueUpdatesCommand(CommandContext ctx,
        [Description("Channel in which the embed should be created")] 
        DiscordChannel? channel = null
    ) {
        if (channel is null)
            await ctx.Message.DeleteAsync();

        channel ??= ctx.Channel;
        await this.Data.SetQueueUpdate(channel);
    }

    [Command("destroysongupdates"), Aliases("dsu")]
    [Description("Removes the interactive embed that displays music info")]
    public async Task DestroySongUpdatesCommand(CommandContext ctx)
        => await this.Data.DestroySongUpdate();

    [Command("createqueueupdates"), Aliases("dqu")]
    [Description("Removes the interactive embed that displays song queue")]
    public async Task DestroyQueueUpdatesCommand(CommandContext ctx)
        => await this.Data.DestroyQueueUpdate();

    [Command("queue"), Aliases("q")]
    [Description("Enqueues specified track or playlist")]
    public async Task QueueCommand(CommandContext ctx,
        [RemainingText, Description("Name of the song, song uri or playlist uri")] string message)
    {
        IEnumerable<LavalinkTrack> tracks;
        if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
            tracks = await this.Audio.GetTracksAsync(uri);
        else tracks = await this.Audio.GetTracksAsync(message);

        var lavalinkTracks = tracks.ToList();

        var embed = new DiscordEmbedBuilder();
        if (lavalinkTracks.Any())
        {
            var description = lavalinkTracks.Count == 1
                ? $"[{lavalinkTracks.First().Title}]({lavalinkTracks.First().Uri})"
                : $"Enqueued {lavalinkTracks.Count} songs";

            embed.WithTitle(":thumbsup:  |  Enqueued: ")
                .WithDescription(description)
                .WithColor(DiscordColor.Purple);

            this.Data.Enqueue(lavalinkTracks);
        }
        else throw new CommandException("Failed to enqueue");

        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("queuefirst"), Aliases("qf")]
    [Description("Enqueues a song (or playlist) on top of a current queue")]
    public async Task QueueFirstCommand(CommandContext ctx,
        [RemainingText, Description("Name of the song, song uri or playlist uri")]
        string message)
    {
        IEnumerable<LavalinkTrack> tracks;
        if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
            tracks = await this.Audio.GetTracksAsync(uri);
        else tracks = await this.Audio.GetTracksAsync(message);

        var lavalinkTracks = tracks.ToList();

        var embed = new DiscordEmbedBuilder();
        if (lavalinkTracks.Any())
        {
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
    public async Task QueueManyCommand(CommandContext ctx,
        [RemainingText, Description(
            "Multiple song names (ex. >>qm \"Doin Your Mom\" \"Burning memories\")")]
        params string[] message)
    {
        var tracks = new List<LavalinkTrack>();
        foreach (var s in message)
        {
            var foundTracks = (await this.Audio.GetTracksAsync(s)).ToList();
            if (foundTracks.Any())
                tracks.Add(foundTracks.First());
        }

        var embed = new DiscordEmbedBuilder();
        if (tracks.Any())
        {
            string description = $"Enqueued {tracks.Count} songs";

            embed.WithTitle(":thumbsup:  |  Enqueued: ")
                .WithDescription(description)
                .WithColor(DiscordColor.Purple);

            this.Data.Enqueue(tracks);
        }
        else throw new CommandException("Failed to enqueue");

        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("listqueue"), Aliases("lq")]
    [Description("Lists next 10 songs in the queue")]
    public async Task QueueCommand(CommandContext ctx)
    {
        var tracks = this.Data.GetNextTracks();

        var embed = new DiscordEmbedBuilder();
        string description = "";
        for (var i = 0; i < tracks.Length && i < 10; i++)
            description += $"{i + 1}. [{tracks[i].Title}]({tracks[i].Uri})\n";

        switch (tracks.Length)
        {
            case > 10:
                embed.WithFooter($". . . and {tracks.Length - 10} more");
                break;
            case 0:
                description = "Queue is empty";
                break;
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
    public async Task LoopCommand(CommandContext ctx, int? mode = null)
    {
        if (mode is null) {
            Data.Looping = Data.Looping switch {
                LoopingMode.None => LoopingMode.Queue,
                _ => LoopingMode.None,
            };
        } else if (Enum.IsDefined(typeof(LoopingMode), mode)) {
            Data.Looping = (LoopingMode)mode;
        } else throw new CommandException(
            "Incorrect looping mode, correct modes are:\n" + 
            "` None - 0 `, ` Queue - 1 `, ` Song - 2 `, ` Shuffle - 3 `"
        );

        await ctx.RespondAsync($"Loopig mode set to: `{Data.Looping}`");
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
                "Incorrect usage, please specify if nightcore state (ex. >>nightcore on)"
            );
        }

        await this.Data.SetAudioFiltersAsync();
    }

    [Command("skip"), Aliases("s")]
    [Description("Skips tracks")]
    public async Task SkipCommand(CommandContext ctx, [Description("Number of tracks to skip")] 
            int count = 1) => await this.Data.SkipAsync(count);


    [Command("seek")]
    [Description("Seeks track to specified position")]
    public async Task SeekCommand(CommandContext ctx, [Description("Song timestap")]
            TimeSpan timestamp) => await this.Data.SeekAsync(timestamp);

    [Command("remove"), Aliases("r")]
    [Description("Removes a song from the queue")]
    public async Task RemoveCommand(CommandContext ctx,
        [Description("Index of an enqueued song (see >>lq to list songs and their indexes)")]
        int index = 1, [Description("Number of tracks to be removed")] int count = 1)
    {
        switch (count)
        {
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
            var embed = new DiscordEmbedBuilder()
                .WithTitle(":musical_note:  |  Now playing: ")
                .WithDescription(
                    $"[{this.Data.CurrentTrack.Title}]({this.Data.CurrentTrack.Uri})\n" +
                    $":play_pause: Current timestamp: {this.Data.GetTimestamp()}\n" +
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
}

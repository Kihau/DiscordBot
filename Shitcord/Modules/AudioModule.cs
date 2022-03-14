using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using Shitcord.Data;
using Shitcord.Services;

namespace Shitcord.Extensions;

//[Group("audio")]
[Description("Audio and music commands")]
public class AudioModule : BaseCommandModule
{
    private GuildAudioData Data { get; set; }
    private AudioService Audio { get; init; }

    public AudioModule(AudioService service) => this.Audio = service;

    public override async Task BeforeExecutionAsync(CommandContext ctx)
    {
        this.Data = this.Audio.GetOrAddData(ctx.Guild);
        await base.BeforeExecutionAsync(ctx);
    }

    public override async Task AfterExecutionAsync(CommandContext ctx)
    {
        await this.Data.UpdateSongMessage();
        await this.Data.UpdateQueueMessage();
        await base.AfterExecutionAsync(ctx);
    }

    [Command("join"), Aliases("j")]
    [Description("Joins the voice channel")]
    public async Task JoinCommand(CommandContext ctx, String? name = null)
    {
        var channel = name == null
            ? ctx.Member.VoiceState?.Channel
            : ctx.Guild.Channels.First(x =>
                x.Value.Name.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                x.Value.Type == ChannelType.Voice).Value;

        if (channel == null)
            return;

        await this.Data.CreateConnectionAsync(channel);
    }

    [Command("stop")]
    [Description("Stops current song")]
    public async Task StopCommand(CommandContext ctx) 
        => await this.Data.StopAsync();

    [Command("reset")]
    [Description("Stops current song and resets the queue")]
    public async Task ResetCommand(CommandContext ctx)
    {
        this.Data.ClearQueue();
        await this.Data.StopAsync();
    }

    [Command("play"), Aliases("p")]
    [Description("Stops current track, searches for a song and plays it.")]
    public async Task PlayCommand(CommandContext ctx,
        [RemainingText, Description("Name of the song, song uri or playlist uri")]
        string message = null) 
    {
        var channel = ctx.Member.VoiceState?.Channel;

        if (channel != null)
            await this.Data.CreateConnectionAsync(channel);

        var msgBuilder = new DiscordMessageBuilder();
        
        if (!String.IsNullOrWhiteSpace(message))
        {
            IEnumerable<LavalinkTrack> tracks;
            if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
                tracks = await this.Audio.GetTracksAsync(uri);
            else tracks = await this.Audio.GetTracksAsync(message);
            
            var lavalinkTracks = tracks as LavalinkTrack[] ?? tracks.ToArray();
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
    public async Task SetSongUpdatesCommand(CommandContext ctx, DiscordChannel? channel = null)
    {
        channel ??= ctx.Channel;
        await this.Data.SetSongUpdate(channel);
    }

    [Command("queueupdates"), Aliases("qu")]
    public async Task SetQueueUpdatesCommand(CommandContext ctx, DiscordChannel? channel = null)
    {
        channel ??= ctx.Channel;
        await this.Data.SetQueueUpdate(channel);
    }

    [Command("queue"), Aliases("q")]
    [Description("Enqueues a song or a playlist")]
    public async Task QueueCommand(CommandContext ctx,
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

            embed.WithTitle(":thumbsup:  |  Enqueued: ")
                .WithDescription(description)
                .WithColor(DiscordColor.Purple);

            this.Data.Enqueue(lavalinkTracks);
        }
        else throw new CommandException("Failed to enqueue");

        await ctx.Channel.SendMessageAsync(embed.Build());
    }

    [Command("queuemany"), Aliases("qm")]
    [Description("Enqueues multiple songs")]
    public async Task QueueManyCommand(CommandContext ctx,
        [RemainingText, Description("Multiple song names (ex. >>qm \"Doin Your Mom\" \"Burning memories\")")]
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

    [Command("playfile")]
    [Description("don't use it")]
    public async Task MoanCommand(CommandContext ctx, string file)
    {
        var result = await this.Audio.GetTracksAsync(new FileInfo($"Resources/{file}"));
        this.Data.Enqueue(result.Tracks.First());
        await this.Data.PlayAsync();
    }

    [Command("loop")]
    [Description("Loops the queue")]
    public async Task LoopCommand(CommandContext ctx)
    {
        var loop = this.Data.ChangeLoopingState();
        if (loop)
            await ctx.RespondAsync("Looping enabled");
        else await ctx.RespondAsync("Looping disabled");
    }

    [Command("shuffle")]
    [Description("Shuffles the queue")]
    public async Task ShuffleCommand(CommandContext ctx)
    {
        this.Data.Shuffle();
        await ctx.RespondAsync("Queue shuffled");
    }

    [Command("skip"), Aliases("s")]
    [Description("Skips tracks")]
    public async Task SkipCommand(CommandContext ctx, [Description("Number of tracks to skip")] int count = 1)
        => await this.Data.SkipAsync(count);

    [Command("remove"), Aliases("r")]
    [Description("Removes a song from the queue")]
    public async Task RemoveCommand(CommandContext ctx,
        [Description("Index of an enqueued song (see >>lq to list songs and their indexes)")]
        int index = 1, [Description("Number of tracks to be removed")] int count = 1)
    {
        switch (count)
        {
            case 1:
            {
                var track = this.Data.Remove(--index);

                if (track == null)
                    throw new CommandException("Could not remove the track");

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":thumbsup:  |  Track removed: ")
                    .WithDescription($"[{track.Title}]({track.Uri})\n")
                    .WithColor(DiscordColor.Purple);

                await ctx.Channel.SendMessageAsync(embed.Build());
                break;
            }
            case < 0:
                throw new CommandException("Count must be greater than 0");
            default:
            {
                var tracks = this.Data.RemoveRange(--index, count).Count();

                if (tracks == 0)
                    throw new CommandException("Could not remove the tracks");

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(":thumbsup:  |  Tracks removed: ")
                    .WithDescription($"Removed {tracks} tracks")
                    .WithColor(DiscordColor.Purple);

                await ctx.Channel.SendMessageAsync(embed.Build());
                break;
            }
        }
    }


    [Command("pause")]
    [Description("Pauses current track")]
    public async Task PauseLavaCommand(CommandContext ctx)
        => await this.Data.PauseAsync();

    [Command("nowplaying"), Aliases("current", "np")]
    [Description("Displays info about current song")]
    public async Task NowPlayingLavaCommand(CommandContext ctx)
    {
        if (this.Data.CurrentTrack != null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle(":musical_note:  |  Now playing: ")
                .WithDescription($"[{this.Data.CurrentTrack.Title}]({this.Data.CurrentTrack.Uri})\n" +
                                 $":play_pause: Current timestamp: {this.Data.GetTimestamp()}\n" +
                                 $":play_pause: Song length: {this.Data.CurrentTrack.Length}\n" +
                                 $":play_pause: Song Author: {this.Data.CurrentTrack.Author}\n")
                .WithColor(DiscordColor.Purple);
            await ctx.Channel.SendMessageAsync(embed.Build());
        }
        else throw new CommandException("Currently nothing is playing");
    }

    [Command("resume")]
    [Description("Resumes paused song")]
    public async Task ResumeLavaCommand(CommandContext ctx)
        => await this.Data.ResumeAsync();

    [Group("filters")]
    [Description("Track filter commands")]
    public class FilterModule : BaseCommandModule
    {
        private GuildAudioData Data { get; set; }
        private AudioService Audio { get; init; }

        public FilterModule(AudioService service) => this.Audio = service;

        public override async Task BeforeExecutionAsync(CommandContext ctx)
        {
            this.Data = this.Audio.GetOrAddData(ctx.Guild);
            await base.BeforeExecutionAsync(ctx);
        }
        
        public override async Task AfterExecutionAsync(CommandContext ctx)
        {
            // TODO: ADD "IgnoreFilterUpdate" ATTRIBUTE
            await this.Data.SetAudioFiltersAsync();
            await base.AfterExecutionAsync(ctx);
        }

        [Command("reset")]
        public async Task ResetFiltersCommand(CommandContext ctx)
            => this.Data.Filters = new AudioFilters();


        [Command("get")]
        public async Task GetFiltersCommand(CommandContext ctx)
            => await ctx.Channel.SendMessageAsync($"```json\n{this.Data.Filters.GetJson()}\n```");


        [Command("set")]
        public async Task SetFiltersCommand(CommandContext ctx, [RemainingText] string json)
        {
            int cs1 = json.IndexOf("```", StringComparison.Ordinal) + 5;
            cs1 = json.IndexOf('\n', cs1) + 1;
            int cs2 = json.LastIndexOf("```", StringComparison.Ordinal);

            if (cs1 is -1 || cs2 is -1)
            {
                cs1 = 0;
                cs2 = json.Length;
            }
            
            string filters_string = json.Substring(cs1, cs2 - cs1);
            
            var output = filters_string.GetAudioFilters();
            if (output != null)
                this.Data.Filters = output;
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
        public async Task KaraokeLavaCommand(CommandContext ctx) => this.Data.Filters.Karaoke = new Karaoke();        
        [Command("timescale"), Priority(0)]
        public async Task TimescaleLavaCommand(CommandContext ctx) => this.Data.Filters.Timescale = new TimeScale();        
        [Command("tremolo"), Priority(0)]
        public async Task TremoloLavaCommand(CommandContext ctx) => this.Data.Filters.Tremolo = new Tremolo();        
        [Command("vibrato"), Priority(0)]
        public async Task VibratoLavaCommand(CommandContext ctx) => this.Data.Filters.Vibrato = new Vibrato();        
        [Command("rotation"), Priority(0)]
        public async Task RotationLavaCommand(CommandContext ctx) => this.Data.Filters.Rotation = new Rotation();        
        [Command("lowpass"), Priority(0)]
        public async Task LowPassLavaCommand(CommandContext ctx) => this.Data.Filters.Lowpass = new LowPass();        
        [Command("channelmix"), Priority(0)]
        public async Task ChannelMixLavaCommand(CommandContext ctx) => this.Data.Filters.Channelmix = new ChannelMix();        
        
        [Command("karaoke")]
        public async Task KaraokeLavaCommand(CommandContext ctx, double level = 1, double monoLevel = 1, 
            double filterBand = 220, double filterWidth = 100)
        {
            this.Data.Filters.Karaoke = new Karaoke
            {
                Level = level,
                MonoLevel = monoLevel,
                FilterBand = filterBand,
                FilterWidth = filterWidth
            };
        }

        [Command("timescale")]
        public async Task TimescaleLavaCommand(CommandContext ctx, double speed = 1.0, double pitch = 1.0,
            double rate = 1.0)
        {
            this.Data.Filters.Timescale = new TimeScale
            {
                Pitch = pitch,
                Rate = rate,
                Speed = speed
            };
        }
        
        [Command("tremolo")]
        public async Task TremoloLavaCommand(CommandContext ctx, double frequency = 2.0, double depth = 0.5)
        {
            this.Data.Filters.Tremolo = new Tremolo
            {
                Frequency = frequency,
                Depth = depth,
            };
        }
        
        [Command("vibrato")]
        public async Task VibratoLavaCommand(CommandContext ctx, double frequency = 2.0, double depth = 0.5)
        {
            this.Data.Filters.Vibrato = new Vibrato
            {
                Frequency = frequency,
                Depth = depth
            };
        }

        [Command("rotation")]
        public async Task RotationLavaCommand(CommandContext ctx, double rotationFreq = 0.0)
        {
            this.Data.Filters.Rotation = new Rotation {RotationFreq = rotationFreq};
        }

        [Command("lowpass")]
        public async Task LowPassLavaCommand(CommandContext ctx, double smoothing = 2.0)
        {
            this.Data.Filters.Lowpass = new LowPass {Smoothing = smoothing};
        }

        [Command("channelmix")]
        public async Task ChannelMixLavaCommand(CommandContext ctx, double leftToLeft = 1, double leftToRight = 0,
            double rightToLeft = 0, double rightToRight = 1)
        {
            this.Data.Filters.Channelmix = new ChannelMix
            {
                LeftToLeft = leftToLeft,
                LeftToRight = leftToRight,
                RightToLeft = rightToLeft,
                RightToRight = rightToRight
            };
        }
    }

    [Command("volume")]
    [Description("Sets volume level of a command")]
    public async Task VolumeLavaCommand(CommandContext ctx, [Description("volume level (greater than 0)")] int level)
        => await this.Data.SetVolumeAsync(level);

    [Command("leave")]
    [Description("Leaves the voice channel")]
    public async Task LeaveLavaCommand(CommandContext ctx)
        => await this.Data.DestroyConnectionAsync();
}
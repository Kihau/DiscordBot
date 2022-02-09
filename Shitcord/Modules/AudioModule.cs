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

namespace Shitcord.Modules;

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

        if (!String.IsNullOrWhiteSpace(message))
        {
            IEnumerable<LavalinkTrack> tracks;
            if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
                tracks = await this.Audio.GetTracksAsync(uri);
            else tracks = await this.Audio.GetTracksAsync(message);

            this.Data.EnqueueFirst(tracks);
        }

        await this.Data.PlayAsync();

        var embed = new DiscordEmbedBuilder();
        if (this.Data.CurrentTrack != null) 
        {
            //var embed = new DiscordEmbedBuilder()
            embed.WithTitle(":musical_note:  |  Now playing: ")
                .WithDescription($"[{this.Data.CurrentTrack.Title}]({this.Data.CurrentTrack.Uri})")
                .WithColor(DiscordColor.Purple);
        }
        else throw new CommandException("Failed to play the song");

        await ctx.Channel.SendMessageAsync(embed.Build());
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
        if (lavalinkTracks.Any() /*&& this.Data.IsConnected*/)
        {
            //var embed = new DiscordEmbedBuilder()

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
        if (tracks.Any() /*&& this.Data.IsConnected*/)
        {
            string description = $"Enqueued {tracks.Count()} songs";

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
        for (int i = 0; i < tracks.Length && i < 10; i++)
            description += $"{i + 1}. [{tracks[i].Title}]({tracks[i].Uri})\n";

        if (tracks.Length > 10)
            description += $". . . and {tracks.Length - 10} more";

        if (tracks.Length == 0)
            description = "Queue is empty";

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
        => await this.Data.SkipAsync(1);

    // TODO: Remove by starting and ending index instead of index and count (maybe?)
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
                                 $":play_pause: Song length: {this.Data.CurrentTrack.Length}" +
                                 $":play_pause: Song Author: {this.Data.CurrentTrack.Author}")
                .WithColor(DiscordColor.Purple);
            await ctx.Channel.SendMessageAsync(embed.Build());
        }
        else throw new CommandException("Currently nothing is playing");
    }

    [Command("resume")]
    [Description("Resumes paused song")]
    public async Task ResumeLavaCommand(CommandContext ctx)
        => await this.Data.ResumeAsync();

    [Command("timescale")]
    public async Task TimescaleLavaCommand(CommandContext ctx, double speed = 1.0, double pitch = 1.0,
        double rate = 1.0)
    {
        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedNodes.Values.First();
        var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

        var scale = new TimeScale();
        scale.Speed = speed;
        scale.Pitch = pitch;
        scale.Rate = rate;

        await conn.SetTimescaleAsync(scale);
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
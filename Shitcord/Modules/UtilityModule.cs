using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Shitcord.Data;
using Shitcord.Services;

namespace Shitcord.Modules;

// TODO: Add snipe and editsnipe command

//[Group("utils")]
[Description("Bot utility commands")]
public class UtilityModule : BaseCommandModule
{
    public Discordbot Bot { get; }
    public TimeService Time { get; set; }

    public UtilityModule(Discordbot bot, TimeService timerService)
    {
        this.Bot = bot;
        this.Time = timerService;
    }

    public override async Task BeforeExecutionAsync(CommandContext ctx)
    {
        //this.Data = this.Time.GetOrAddData(ctx.Guild);
        await base.BeforeExecutionAsync(ctx);
    }

    // [Command("createdatechannel"), Aliases("cdc")]
    // public async Task CreateDateChannelCommand(CommandContext ctx)
    //     => await this.Data.CreateDateChannel();

    [Command("purge")]
    [Description("Removes messages in current (or specified) channel")]
    public async Task PurgeCommand(CommandContext ctx, uint count, DiscordChannel channel = null)
    {
        var member = ctx.Member;
        if ((member.Permissions & Permissions.ManageMessages) == 0)
            return;

        if (channel == null)
            count += 1;

        channel ??= ctx.Channel;
        var messeges = await channel.GetMessagesAsync((int) count);
        await channel.DeleteMessagesAsync(messeges);
    }

    [Command("ping")]
    [Description("pong?")]
    public async Task PingCommand(CommandContext ctx) =>
        await ctx.RespondAsync($"Current bot ping is: `{ctx.Client.Ping}ms`");

    [Command("uptime")]
    [Description("Displays bot uptime")]
    public async Task UptimeCommand(CommandContext ctx)
    {
        var uptime = DateTime.Now - this.Bot.StartTime;
        var d = uptime.Days > 0 ? uptime.Days + "d " : "";
        var h = uptime.Hours > 0 ? uptime.Hours + "h " : "";
        var m = uptime.Minutes > 0 ? uptime.Minutes + "m " : "";
        await ctx.RespondAsync($"Bot uptime: `{d}{h}{m}{uptime.Seconds}s`");
    }

    [Command("shutdown"), Aliases("exit")]
    [Description("literally don't even try")]
    public async Task ShutdownCommand(CommandContext ctx)
    {
        if (StaticData.AdminIds.Contains(ctx.User.Id))
        {
            await ctx.RespondAsync("Shutting down");
            System.Environment.Exit(0);
        }
    }

    [Command("avatar"), Description("Displays user avatar")]
    public async Task DisplayAvatarCommand(CommandContext context,
        [Description("User name (ex. `@Kihau` or `Kihau#3428`)")]
        DiscordUser req_user = null,
        [Description("Size in pixels")] ushort? req_size = null)
    {
        var user = req_user ?? context.User;
        var size = req_size ?? 2048;
        var embed = new DiscordEmbedBuilder()
            .WithTitle($"Avatar {user.Username}")
            .WithImageUrl(user.GetAvatarUrl(ImageFormat.Auto, size))
            .WithTimestamp(DateTime.UtcNow)
            .WithColor(DiscordColor.Purple);
        await context.RespondAsync(embed: embed);
    }

    [Command("emote"), Description("Displays given emote")]
    public async Task DisplayEmoteCommand(CommandContext context,
        [Description("Given emote")] DiscordEmoji req_emoji)
        => await context.RespondAsync(req_emoji.Url);

    [Command("clone"), Description("Clones specified channel")]
    public async Task CloneChannelAsync(CommandContext context, [Description("New channel name")] string name,
        [Description("Channel name (ex. `#channel`)")] DiscordChannel req_channel = null)
    {
        var channel = req_channel ?? context.Channel;
        var clone = await channel.CloneAsync();
        await clone.ModifyAsync(x => x.Name = name);
    }

    // TODO: EVAL
    // [Command("eval")]
    // public async Task EvaluateAsync(CommandContext, string)
}
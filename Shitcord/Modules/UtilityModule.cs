using System.Diagnostics;
using System.Globalization;
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
    public async Task PurgeCommand(CommandContext ctx, uint count, DiscordChannel? channel = null)
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

        var sys = new Process();
        sys.StartInfo.FileName = "uptime";
        sys.StartInfo.Arguments = "-s yyyy-mm-dd HH:MM:SS";
        sys.StartInfo.CreateNoWindow = true;
        sys.StartInfo.UseShellExecute = false;
        sys.StartInfo.RedirectStandardOutput = true;
        sys.Start();
        await sys.WaitForExitAsync();
        
        var sys_uptime = DateTime.Now - DateTime.ParseExact(
            (await sys.StandardOutput.ReadLineAsync())!,
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture);
        
        var sd = sys_uptime.Days > 0 ? sys_uptime.Days + "d " : "";
        var sh = sys_uptime.Hours > 0 ? sys_uptime.Hours + "h " : "";
        var sm = sys_uptime.Minutes > 0 ? sys_uptime.Minutes + "m " : "";

        await ctx.RespondAsync($"Bot uptime: `{d}{h}{m}{uptime.Seconds}s`\n" +
                               $"System uptime: `{sd}{sh}{sm}{sys_uptime.Seconds}s`\n");
    }

    [Command("shutdown"), Aliases("exit")]
    [Description("literally don't even try")]
    public async Task ShutdownCommand(CommandContext ctx)
    {
        if (StaticData.AdminIds.Contains(ctx.User.Id))
        {
            await ctx.RespondAsync("Shutting down");
            Environment.Exit(0);
        }
    }

    [Command("avatar"), Description("Displays user avatar")]
    public async Task DisplayAvatarCommand(CommandContext ctx,
        [Description("User name (ex. `@Kihau` or `Kihau#3428`)")]
        DiscordUser? reqUser = null,
        [Description("Size in pixels")] ushort? reqSize = null)
    {
        var user = reqUser ?? ctx.User;
        var size = reqSize ?? 2048;
        var embed = new DiscordEmbedBuilder()
            .WithTitle($"Avatar {user.Username}")
            .WithImageUrl(user.GetAvatarUrl(ImageFormat.Auto, size))
            .WithTimestamp(DateTime.UtcNow)
            .WithColor(DiscordColor.Purple);
        await ctx.RespondAsync(embed);
    }

    [Command("emote"), Description("Displays given emote")]
    public async Task DisplayEmoteCommand(CommandContext ctx,
        [Description("Given emote")] DiscordEmoji reqEmoji)
        => await ctx.RespondAsync(reqEmoji.Url);

    [Command("clone"), Description("Clones specified channel")]
    public async Task CloneChannelAsync(CommandContext ctx, [Description("New channel name")] string name,
        [Description("Channel name (ex. `#channel`)")] DiscordChannel? reqChannel = null)
    {
        var channel = reqChannel ?? ctx.Channel;
        var clone = await channel.CloneAsync();
        await clone.ModifyAsync(x => x.Name = name);
    }
    
    // TODO: move to owner commands - create require owner attribute
    [Command("memoryusage"), Aliases("memuse"), Description("Displays bot memory usage")]
    public async Task MemoryUsageCommand(CommandContext ctx)
    {
        const long MB = 1024 * 1024;
        var proc = Process.GetCurrentProcess();
        var priv_mem = $"Private memory: `{proc.PrivateMemorySize64 / MB}MB`\n";
        var page_mem = $"Paged memory: `{proc.PagedMemorySize64 / MB}MB`\n";
        var virt_mem = $"Virtual memory: `{proc.VirtualMemorySize64 / MB}MB`\n";
        var gctotal_mem = $"Total GC memory: `{GC.GetTotalMemory(false) / MB}MB`\n";
        var gcalloc_mem = $"Allocated GC memory: `{GC.GetTotalAllocatedBytes() / MB}MB`\n";
        await ctx.RespondAsync($"{priv_mem}{page_mem}{virt_mem}{gctotal_mem}{gcalloc_mem}");
    }
    
    [Command("garbagecollect"), Aliases("gc"), Description("Performs memory clean-up")]
    public async Task GarbageCollectCommand(CommandContext ctx)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await ctx.RespondAsync("Done :thumbsup: ");
    }
    
    [Command("debug"), Description("Enables/disables debug mode")]
    public async Task DebugCommand(CommandContext ctx)
    {
        StaticData.DebugEnabled = !StaticData.DebugEnabled;
        await ctx.RespondAsync($"Debug mode set to: `{StaticData.DebugEnabled}`");
    }
    
    // TODO: EVAL
    // [Command("eval")]
    // public async Task EvaluateAsync(CommandContext, string)
}
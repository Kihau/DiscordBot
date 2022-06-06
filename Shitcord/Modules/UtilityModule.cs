using System.Diagnostics;
using System.Globalization;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Shitcord.Services;

namespace Shitcord.Modules;

// TODO: Add snipe and editsnipe command

//[Group("utils")]
[Description("Bot utility commands")]
public class UtilityModule : BaseCommandModule
{
    public Discordbot Bot { get; }
    public TimeService Time { get; set; }

    public UtilityModule(Discordbot bot, TimeService timerService, ReplyService reply)
    {
        this.Bot = bot;
        this.Time = timerService;
    }

    public override async Task BeforeExecutionAsync(CommandContext ctx)
        => await base.BeforeExecutionAsync(ctx);

    [Command("purge")]
    [Description("Removes messages in current (or specified) channel")]
    public async Task PurgeCommand(CommandContext ctx, uint count, DiscordChannel? channel = null)
    {
        var member = ctx.Member;
        if ((member?.Permissions & Permissions.ManageMessages) == 0)
            return;

        if (channel == null)
            count += 1;

        channel ??= ctx.Channel;
        var messeges = await channel.GetMessagesAsync((int) count);
        await channel.DeleteMessagesAsync(messeges);
    }

    // TODO: Add more description to this module
    // TODO: Add matching options (agressive, whole string, etc.)
    [Group("reply"), Description("Reply commands")]
    public class ReplyModule : BaseCommandModule
    {
        public ReplyService Reply { get; }
        public ReplyModule(ReplyService reply) => this.Reply = reply;

        [Command("add")]
        [Description("Adds auto respose for a certain string in a message")]
        public async Task AddCommand(CommandContext ctx, string match, string response) 
            => this.Reply.AddReplyData(ctx.Guild, new ReplyData(match.ToLower(), response)); 

        [Command("remove")]
        [Description("Removes auto respose for a certain string in a message")]
        public async Task RemoveCommand(CommandContext ctx, string match) 
            => this.Reply.RemoveReplyData(ctx.Guild, match.ToLower());


        [Command("removeat")]
        [Description("Removes auto respose for a certain reply index")]
        public async Task RemoveAtCommand(CommandContext ctx, int index) 
            => this.Reply.RemoveReplyDataAt(ctx.Guild, index);

        [Command("list")]
        [Description("List all matchreply queries")]
        public async Task ListCommand(CommandContext ctx) 
        {
            var data = this.Reply.GetReplyData(ctx.Guild); 
            
            // TODO:
            // Create embed out of this data (or something)
            // Add interactions ???
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Added reply strings:")
                .WithColor(DiscordColor.Purple);
        }
    }

    // TODO: Check if request exist
    [Command("httpcat"), Aliases("http")]
    [Description("Get http error response")]
    public async Task PingCommand(CommandContext ctx, int reponse) =>
        await ctx.RespondAsync($"https://http.cat/{reponse}");


    [Command("ping")]
    [Description("pong?")]
    public async Task HttpErrorCommand(CommandContext ctx) =>
        await ctx.RespondAsync($"Current bot ping is: `{ctx.Client.Ping}ms`");

    [Command("info")]
    [Description("Displays info about the bot")]
    public async Task InfoCommand(CommandContext ctx) 
    {
        // TODO: Print info about the bot
        var embed = new DiscordEmbedBuilder()
            .WithTitle($"Shitcord V0.6")
            // Embed bot image url (small top right icon)
            //.WithImageUrl(user.GetAvatarUrl(ImageFormat.Auto, size))
            .WithTimestamp(DateTime.UtcNow)
            .WithFooter("Created by: Kihau")
            .WithColor(DiscordColor.Purple);
        await ctx.RespondAsync(embed);
    }

    //[Command("nuke"), Description("Complitely nukes a channel")]
    //public async Task NukeChannelAsync(CommandContext context,
        //[Description("Channel name (ex. `#channel`)")] DiscordChannel req_channel = null)
    //{
        //var channel = req_channel ?? context.Channel;

        //if (channel.Type == ChannelType.Text)
        //{
            //// Create comfirmation embed
            //var confirm = new DiscordEmbedBuilder()
                //.WithTitle($"__***DETONACJA KANAŁU:***__ `{channel.Name}`")
                //.AddField($"Czy jesteś pewny, że chcesz nieodwracalnie wyczyścić ten kanał?",
                    //":white_check_mark: - TAK, chcę wyczyścić ten kanał\n :x: - NIE, rozmyśliłem się")
                //.WithColor(DiscordColor.Violet).Build();
            //var message = await context.RespondAsync(embed: confirm);

            //// Add yes and no reactions
            //await message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":white_check_mark:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":x:"));

            //var result = await message.WaitForReactionAsync(context.User, TimeSpan.FromSeconds(10));
            //await message.DeleteAsync();
            
            //if (!result.TimedOut && result.Result.Emoji == DiscordEmoji.FromName(context.Client, ":white_check_mark:"))
            //{
                //await context.RespondAsync("__**TACTICAL NUKE INCOMING!**__\nhttps://tenor.com/view/explosion-mushroom-cloud-atomic-bomb-bomb-boom-gif-4464831");
            
                //await Task.Delay(1000);
            
                //await channel.CloneAsync();
                //await channel.DeleteAsync();
            //}
        //}
    //}
    
    
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
}

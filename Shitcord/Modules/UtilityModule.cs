using System.Diagnostics;
using System.Globalization;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Shitcord.Extensions;
using Shitcord.Services;

namespace Shitcord.Modules;

[Description("Bot utility commands")]
public class UtilityModule : BaseCommandModule
{
    public DiscordBot Bot { get; }
    public WeatherService Weather { get; }
    public ModerationService Mod { get; }

    public UtilityModule(DiscordBot bot, WeatherService weather, ModerationService moderation)
    {
        Bot = bot;
        Weather = weather;
        Mod = moderation;
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

    [Group("reply")] 
    [Description("Set of commands allowing to set automatic response to certain messages")]
    public class ReplyModule : BaseCommandModule
    {
        public ReplyService Reply { get; }
        public ReplyModule(ReplyService reply) => Reply = reply;

        [Command("addany")]
        [Description("Adds auto reply that responds to messages." +
            "Tries to match any message part with provided match string")]
        public async Task AddAnyCommand(
            CommandContext ctx, string match, string reply, bool match_case = false
        ) { 
            this.Reply.AddReplyData(
                ctx.Guild, new ReplyData(match.ToLower(), reply, MatchMode.Any, match_case)
            ); 
            await ctx.RespondAsync("Successfully added to the reply list 👍");
        }

        [Command("addfirst")]
        [Description("Adds auto reply that responds to messages." +
            "Tries to match first message characters with provided match string")]
        public async Task AddFirstCommand(
            CommandContext ctx, string match, string reply, bool match_case = false
        ) { 
            this.Reply.AddReplyData(
                ctx.Guild, new ReplyData(match.ToLower(), reply, MatchMode.First, match_case)
            ); 
            await ctx.RespondAsync("Successfully added to the reply list 👍");
        }

        [Command("addexact")]
        [Description("Adds auto reply that responds to messages." +
            "Tries to match all message with provided match string")]
        public async Task AddExactCommand(
            CommandContext ctx, string match, string reply, bool match_case = true
        ) { 
            this.Reply.AddReplyData(
                ctx.Guild, new ReplyData(match.ToLower(), reply, MatchMode.Exact, match_case)
            ); 
            await ctx.RespondAsync("Successfully added to the reply list 👍");
        }

        [Command("remove"), Aliases("rm")]
        [Description("Removes auto reply for a certain string in a message")]
        public async Task RemoveCommand(CommandContext ctx, string match) 
        {
            this.Reply.RemoveReplyData(ctx.Guild, match.ToLower());
            await ctx.RespondAsync("Successfully removed from the reply list 👍");
        }

        [Command("removeat"), Aliases("rmat")]
        [Description("Removes auto reply for a certain reply index")]
        public async Task RemoveAtCommand(CommandContext ctx, int index) 
        {
            this.Reply.RemoveReplyDataAt(ctx.Guild, index);
            await ctx.RespondAsync("Successfully removed from the reply list 👍");
        }

        [Command("removeall"), Aliases("rmall")]
        [Description("Removes all reply for the datalist")]
        public async Task RemoveallCommand(CommandContext ctx) 
        {
            this.Reply.RemoveAllReplyData(ctx.Guild);
            await ctx.RespondAsync("Successfully removed everything from the reply list 👍");
        }

        [Command("list"), Aliases("ls")]
        [Description("List all matchreply queries")]
        public async Task ListCommand(CommandContext ctx) 
        {
            var data = Reply.GetReplyData(ctx.Guild); 
            var string_builder = new StringBuilder();

            if (data is not null) {
                for (int i = 0; i < data.Count; i++)
                    string_builder.Append($"{i + 1}. {data[i].match} - {data[i].reply}\n");
            } else string_builder.Append("Autoreply list is empty");

            var interactivity = ctx.Client.GetInteractivity();
            var list = string_builder.ToString();
            var pages = interactivity.GeneratePagesInEmbed(
                String.IsNullOrWhiteSpace(list) ? "Autoreply list is empty" : list  
            );

            await ctx.Channel.SendPaginatedMessageAsync(
                ctx.Member, pages, PaginationBehaviour.Ignore, 
                ButtonPaginationBehavior.DeleteMessage
            );
        }
    }

    [Command("ping")]
    [Description("pong?")]
    public async Task PingCommand(CommandContext ctx) =>
        await ctx.RespondAsync($"Current bot ping is: `{ctx.Client.Ping}ms`");

    [Command("info")]
    [Description("Displays info about the bot (I can't bother to update this)")]
    public async Task InfoCommand(CommandContext ctx) 
    {
        var embed = new DiscordEmbedBuilder()
            .WithTitle($"Shitcord V0.6")
            .WithThumbnail(
                ctx.Client.CurrentUser.GetAvatarUrl(ImageFormat.Png), 20, 20
            ).WithTimestamp(DateTime.UtcNow)
            .WithDescription(
                "Written in C#: https://dotnet.microsoft.com/en-us/\n" + 
                "Bot source code: https://github.com/Kihau/DiscordBot\n" +
                "Discord API lib: https://github.com/kihau/DSharpPlus\n" +
                "Audio service: https://github.com/freyacodes/Lavalink\n" 
            )
            .WithFooter("Created by: Kihau & Frisk")
            .WithColor(DiscordColor.Purple);
        await ctx.RespondAsync(embed);
    }

    [RequireAuthorized]
    [Command("nuke"), Description("Complitely nukes a channel")]
    async Task NukeChannelAsync(CommandContext ctx,
        [Description("Channel name (ex. `#channel`)")] DiscordChannel? req_channel = null)
    {
        var member = ctx.Member;
        if ((member?.Permissions & Permissions.ManageChannels) == 0)
            return;

        var channel = req_channel ?? ctx.Channel;

        if (channel.Type == ChannelType.Text) {
            // Create comfirmation embed
            var confirm = new DiscordEmbedBuilder()
                .WithTitle($"__***CHANNEL DETONATION:***__ `{channel.Name}`")
                .AddField($"Are you sure you want to irreversibly clear this channel?",
                    ":white_check_mark: - YES, I want to nuke this channel\n" +
                     ":x: - NO, I changed my mind")
                .WithColor(DiscordColor.Violet).Build();
            var message = await ctx.RespondAsync(embed: confirm);

            // Add yes and no reactions
            await message.CreateReactionAsync(
                    DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));

            await message.CreateReactionAsync(
                    DiscordEmoji.FromName(ctx.Client, ":x:"));

            var result = await message
                .WaitForReactionAsync(ctx.User, TimeSpan.FromSeconds(10));
            await message.DeleteAsync();

            if (!result.TimedOut && result.Result.Emoji
                    == DiscordEmoji.FromName(ctx.Client, ":white_check_mark:")) {
                await ctx.RespondAsync("__**TACTICAL NUKE INCOMING!**__\nhttps://tenor.com" +
                        "/view/explosion-mushroom-cloud-atomic-bomb-bomb-boom-gif-4464831");

                await Task.Delay(1000);

                await channel.CloneAsync();
                await channel.DeleteAsync();
            }
        }
    }
    
    [Command("uptime")]
    [Description("Displays bot uptime")]
    public async Task UptimeCommand(CommandContext ctx)
    {
        // TODO: Make this an extension method "HumanizeTimespan" that returns string
        var uptime = DateTime.Now - this.Bot.StartTime;
        var d = uptime.Days > 0 ? uptime.Days + "d " : "";
        var h = uptime.Hours > 0 ? uptime.Hours + "h " : "";
        var m = uptime.Minutes > 0 ? uptime.Minutes + "m " : "";
        //                              vvvvvvvvvvvvvvvvvvvvvvvvv this should be returned string
        var bot_uptime = $"Bot uptime: `{d}{h}{m}{uptime.Seconds}s`\n";

        string system_uptime = "";
        if (System.OperatingSystem.IsLinux()) {
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

            system_uptime = $"System uptime: `{sd}{sh}{sm}{sys_uptime.Seconds}s`\n";
        } 

        await ctx.RespondAsync(bot_uptime + system_uptime);
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
    public async Task CloneChannelAsync(
        CommandContext ctx, [Description("New channel name")] string name,
        [Description("Channel name (ex. `#channel`)")] DiscordChannel? reqChannel = null
    ) {
        var channel = reqChannel ?? ctx.Channel;
        var clone = await channel.CloneAsync();
        await clone.ModifyAsync(x => x.Name = name);
    }

    [Command("rmsnipe"), Description("Snipes last deleted message")]
    public async Task RemoveSnipeCommand(CommandContext ctx, int index = 0) 
    {
        var data = Mod.GetOrAddDeleteData(ctx.Guild);

        if (data.Count == 0)
            throw new CommandException("There is nothing to snipe");

        if (index < 0 && index >= data.Count - 1)
            throw new CommandException("Incorrect index");

        var mess = data[data.Count - index - 1];
        var embed = new DiscordEmbedBuilder()
            .WithAuthor(mess.Author.Username, null , mess.Author.AvatarUrl)
            .WithDescription(mess.Content)
            .WithFooter($"#{mess.Channel.Name} - {mess.CreationTimestamp}")
            .WithColor(DiscordColor.Purple);

        await ctx.RespondAsync(embed);
    }

    [Command("editsnipe"), Description("Snipes last message edit")]
    public async Task EditSnipeCommand(CommandContext ctx, int index = 0) 
    {
        var data = Mod.GetOrAddEditData(ctx.Guild);

        if (data.Count == 0)
            throw new CommandException("There is nothing to snipe");

        if (index < 0 && index >= data.Count - 1)
            throw new CommandException("Incorrect index");

        var tup = data[data.Count - index - 1];
        var embed = new DiscordEmbedBuilder()
            .WithAuthor(tup.Item1.Author.Username, null , tup.Item1.Author.AvatarUrl)
            .WithDescription(tup.Item2)
            .WithFooter($"#{tup.Item1.Channel.Name}")
            .WithColor(DiscordColor.Purple);

        await ctx.RespondAsync(embed);
    }
}

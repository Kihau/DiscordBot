using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Shitcord.Data;
using Shitcord.Database;
using Shitcord.Database.Queries;
using Shitcord.Extensions;
using Shitcord.Services;

namespace Shitcord.Modules;

[Group("markov")]
[Description(
    "**Markov. Must. Consume.**\n Only users in authorized guilds can execute markov commands"
)]
public class MarkovModule : BaseCommandModule 
{
    private MarkovService Markov { get; }
    private GuildMarkovData Data { get; set; }
    private DatabaseService Database { get; }

    #pragma warning disable CS8618
    public MarkovModule(MarkovService service, DatabaseService database) {
        Markov = service;
        Database = database;
    }
    #pragma warning restore CS8618

    public override async Task BeforeExecutionAsync(CommandContext ctx)
    {
        var user_is_auth = Database.ExistsInTable(AuthUsersTable.TABLE_NAME, 
            Condition.New(AuthUsersTable.USER_ID).Equals(ctx.User.Id)
        );

        if (!user_is_auth && !Markov.AuthorizedGuilds.Contains(ctx.Guild.Id)) {
            throw new CommandException(
                "Members of this guild are not authorized to execute markov commands"
            );
        }

        Data = Markov.GetOrAddData(ctx.Guild);
        await base.BeforeExecutionAsync(ctx);
    }

    [RequireAuthorized]
    [Command("clearcorupted"), Aliases("cc"), Description("Clears corupted markov strings")]
    public async Task ClearCoruptedCommand(CommandContext ctx) { 
        Markov.ClearCoruptedStrings();
        await ctx.RespondAsync($"Corupted strings cleared");
    }

    [RequireAuthorized]
    [Command("addguild"), Aliases("ag"), Description("Adds guild to authorized list.")]
    public async Task AddAuthGuildCommand(CommandContext ctx, DiscordGuild guild) {
        Markov.AddAuthorizedGuild(guild);
        await ctx.RespondAsync("Guild added to the authorized list");
    }

    [RequireAuthorized]
    [Command("listguilds"), Aliases("lg"), Description("Lists all authorized guilds.")]
    public async Task ListAuthGuildCommand(CommandContext ctx) {
        StringBuilder builder = new();

        builder.Append("```\n");

        for (int i = 0; i < Markov.AuthorizedGuilds.Count; i++) {
            var id = Markov.AuthorizedGuilds[i];
            builder.Append($"{i}. {id}\n");
        }

        builder.Append("```\n");
        await ctx.RespondAsync(builder.ToString());
    }

    [RequireAuthorized]
    [Command("removeguild"), Aliases("rg"), Description("Removes guild from authorized list.")]
    public async Task RemoveAuthGuildCommand(CommandContext ctx, DiscordGuild guild) {
        Markov.RemoveAuthorizedGuild(guild);
        await ctx.RespondAsync("Guild removed from the authorized list");
    }


    [Command("remove"), Aliases("rm"), Description("Removes selected markov strings")]
    public async Task RemoveStringCommand(CommandContext ctx, string to_be_removed)
    {
        Markov.RemoveAnyString(to_be_removed);
        await ctx.RespondAsync($"Removed any matching strings");
    }

    [Command("disable"), Description("Disables markov service")]
    public async Task DisableCommand(CommandContext ctx) { 
        Data.IsEnabled = false;
        Data.UpdateEnabledFlag();
        await ctx.RespondAsync($"Markov service is now disabled");
    }

    [Command("enable"), Description("Enables markov service")]
    public async Task EnableCommand(CommandContext ctx) {
        Data.IsEnabled = true; 
        Data.UpdateEnabledFlag();
        await ctx.RespondAsync($"Markov service is now enabled");
    }

    [Command("autoresponse"), Description("Sets markov autoresponse (input nothing to switch it)")]
    public async Task ResponseSetCommand(CommandContext ctx, bool? enabled = null) 
    {
        if (enabled is null)
            Data.ResponseEnabled = !Data.ResponseEnabled;
        else Data.ResponseEnabled = enabled.Value;
        Data.UpdateAutoResponse();

        await ctx.RespondAsync($"Markov auto reposonse is now set to: `{Data.ResponseEnabled}`");
    }

    [Command("chance"), Description("Sets markov autoresponse chance")] 
    public async Task ResponseChanceCommand(CommandContext ctx, int chance) 
    {
        if (chance < 0 || chance > GuildMarkovData.MAX_CHANCE)
            throw new CommandException(
                $"Auto reponse chance my be between 0 and {GuildMarkovData.MAX_CHANCE}"
            );

        Data.ResponseChance = chance;
        Data.UpdateAutoResponse();
        await ctx.RespondAsync($"Markov chance is now set to: `{Data.ResponseChance}`");
    }

    [Command("timeout"), Description("Sets markov auto response timeout")]
    public async Task ResponseTimeoutCommand(CommandContext ctx, TimeSpan timeout) 
    {
        Data.ResponseTimeout = timeout;
        Data.UpdateAutoResponse();
        await ctx.RespondAsync($"Markov timeout is now set to: `{Data.ResponseTimeout}`");
    }

    [Command("chainlength"), Description("Sets markov chain length (-1 or null for default)")]
    public async Task ChainLenCommand(CommandContext ctx, int? min, int? max) 
    {
        if (min > max) throw new CommandException("Min length cannot be greater than max");

        Data.MinChainLength = min is null || min.Value < 0 
            ? GuildMarkovData.DEFAULT_MIN : min.Value;

        Data.MaxChainLength = max is null || max.Value <= 0 
            ? GuildMarkovData.DEFAULT_MAX : max.Value;

        Data.UpdateChainLength();

        await ctx.RespondAsync(
            $"Markov chain min is now set to: `{Data.MinChainLength}`," +
            $"and max to: `{Data.MaxChainLength}`"
        );
    }

    [Command("info"), Description("Displays markov data for the current guild")]
    public async Task PrintInfoCommand(CommandContext ctx) 
    {
        double chance = (double)Data.ResponseChance / GuildMarkovData.MAX_CHANCE;
        var description = 
            $"Markov enabled: `{Data.IsEnabled}`\n" +
            $"Min chain length: `{Data.MinChainLength}`\n" +
            $"Max chain length: `{Data.MaxChainLength}`\n" +  
            $"Auto response enabled: `{Data.ResponseEnabled}`\n" +
            $"Auto response timeout: `{Data.ResponseTimeout}`\n" + 
            $"Last response time: `{Data.LastResponse}`\n" + 
            $"Number of excluded channels: `{Data.ExcludedChannelIDs.Count}`\n" + 
            $"Auto response chance: `{chance * 100}%`\n";

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Markov info for the current guild:")
            .WithDescription(description)
            .WithColor(DiscordColor.Purple)
            .Build();

        await ctx.Channel.SendMessageAsync(embed);
    }

    [Command("exclude"), Description("Excludes a channel to not gather data from")]
    public async Task ExcludeChannelCommand(CommandContext ctx, DiscordChannel channel)
    {
        if (Data.ExcludedChannelIDs.Contains(channel.Id)) {
            Data.DeleteExcludeChannel(channel.Id);
            await ctx.RespondAsync(
                $"Channel `{channel.Name}` is no longer excluded from data gathering"
            );
        } else {
            Data.InsertNewExcludeChannel(channel.Id);
            await ctx.RespondAsync(
                $"Channel `{channel.Name}` is now excluded from data gathering"
            );
        }
    }

    [Command("excludeall"), Description("Excludes all channels from data gathering")]
    public async Task ExcludeAllChannelsCommand(CommandContext ctx)
    {
        var channels_to_add = ctx.Guild.Channels
            .Where(x => !Data.ExcludedChannelIDs.Contains(x.Key))
            .Select(x => x.Key)
            .ToArray();

        foreach (var channel_id in channels_to_add)
            Data.InsertNewExcludeChannel(channel_id);

        await ctx.RespondAsync("All channels in the guild are now excluded");
    }

    [Command("excludeclear"), Description("Removes all channels from exclusion list")]
    public async Task ExcludeClearChannelsCommand(CommandContext ctx)
    {
        Data.DeleteAllExcludeChannel();
        await ctx.RespondAsync("Removed all channels from the *exclude* list");
    }
}

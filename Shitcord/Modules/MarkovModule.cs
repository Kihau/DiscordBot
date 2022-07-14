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

[Group("markov")]
[Description("Markov. Must. Consume.")]
public class MarkovModule : BaseCommandModule 
{
    private MarkovService Markov { get; init; }
    private GuildMarkovData Data { get; set; }

    public MarkovModule(MarkovService service) => this.Markov = service;

    public override async Task BeforeExecutionAsync(CommandContext ctx)
    {
        this.Data = this.Markov.GetOrAddData(ctx.Guild);
        await base.BeforeExecutionAsync(ctx);
    }

    [Command("disable"), Description("Disables markov service")]
    public async Task DisableCommand(CommandContext ctx) { 
        Data.IsEnabled = false;
        await ctx.RespondAsync($"Markov service is now disabled");
    }

    [Command("enable"), Description("Enables markov service")]
    public async Task EnableCommand(CommandContext ctx) {
        Data.IsEnabled = true; 
        await ctx.RespondAsync($"Markov service is now enabled");
    }

    [Command("autoresponse"), Description("Sets markov autoresponse (input nothing to switch it)")]
    public async Task ResponseSetCommand(CommandContext ctx, bool? input) 
    {
        if (input is null)
            Data.ResponseEnabled = !Data.ResponseEnabled;
        else Data.ResponseEnabled = input.Value;

        await ctx.RespondAsync($"Markov auto reposonse is not set to: `{Data.ResponseEnabled}`");
    }

    [Command("chance"), Description("Sets markov autoresponse chance")] 
    public async Task ResponseChanceCommand(CommandContext ctx, int chance) 
    {
        if (chance < 0 || chance > GuildMarkovData.MAX_CHANCE)
            throw new CommandException(
                $"Auto reponse chance my be between 0 and {GuildMarkovData.MAX_CHANCE}"
            );

        Data.ResponseChance = chance;
        await ctx.RespondAsync($"Markov chance is now set to: `{Data.ResponseChance}`");
    }

    [Command("timeout"), Description("Sets markov auto response timeout")]
    public async Task ResponseTimeoutCommand(CommandContext ctx, TimeSpan timeout) 
    {
        Data.ResponseTimeout = timeout;
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

        await ctx.RespondAsync(String.Format(
            $"Markov chain min is now set to: `{0}`, and max to: `{1}`",
            Data.MinChainLength, Data.MaxChainLength
        ));
    }

    [Command("info"), Description("Displays markov data for the current guild")]
    public async Task PrintInfoCommand(CommandContext ctx) 
    {
        double chance = (double)Data.ResponseChance / GuildMarkovData.MAX_CHANCE;
        var description = 
            $"Markov enabled: `{Data.IsEnabled}`\n" +
            $"Min chain length: `{Data.MinChainLength}`\n" +
            $"Max chain length: `{Data.MaxChainLength}`\n" +  
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
            Data.ExcludedChannelIDs.Remove(channel.Id);
            await ctx.RespondAsync(
                $"Channel `{channel.Name}` is now excluded from data gathering"
            );
        } else {
            Data.ExcludedChannelIDs.Add(channel.Id);
            await ctx.RespondAsync(
                $"Channel `{channel.Name}` is no longer excluded from data gathering"
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

        foreach (var channel in channels_to_add)
            Data.ExcludedChannelIDs.Add(channel);

        await ctx.RespondAsync("All channels in the guild are now excluded");
    }

    [Command("excludeclear"), Description("Removes all channels from exclusion list")]
    public async Task ExcludeClearChannelsCommand(CommandContext ctx)
    {
        Data.ExcludedChannelIDs.Clear();
        await ctx.RespondAsync("Removed all channels from the *excluded* list");
    }

    [Command("save")]
    public async Task SaveCommand(CommandContext ctx)
        => Markov.SaveMarkovBinaryData();

    [Command("load")]
    public async Task LoadCommand(CommandContext ctx)
        => Markov.LoadMarkovBinaryData();
}

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
    public async Task MarkovDisableCommand(CommandContext ctx) { 
        Data.IsEnabled = false;
        await ctx.RespondAsync($"Markov service is now disabled");
    }

    [Command("enable"), Description("Enables markov service")]
    public async Task MarkovEnableCommand(CommandContext ctx) {
        Data.IsEnabled = true; 
        await ctx.RespondAsync($"Markov service is now enabled");
    }

    [Command("autoresponse"), Description("Sets markov autoresponse (input nothing to switch it)")]
    public async Task MarkovResponseSetCommand(CommandContext ctx, bool? input) 
    {
        if (input is null)
            Data.ResponseEnabled = !Data.ResponseEnabled;
        else Data.ResponseEnabled = input.Value;

        await ctx.RespondAsync($"Markov auto reposonse is not set to: `{Data.ResponseEnabled}`");
    }

    [Command("chance"), Description("Sets markov autoresponse chance")] 
    public async Task MarkovResponseChanceCommand(CommandContext ctx, int chance) 
    {
        if (chance < 0 || chance > GuildMarkovData.MAX_CHANCE)
            throw new CommandException(
                $"Auto reponse chance my be between 0 and {GuildMarkovData.MAX_CHANCE}"
            );

        Data.ResponseChance = chance;
        await ctx.RespondAsync($"Markov chance is now set to: `{Data.ResponseChance}`");
    }

    [Command("timeout"), Description("Sets markov auto response timeout")]
    public async Task MarkovResponseTimeoutCommand(CommandContext ctx, TimeSpan timeout) 
    {
        Data.ResponseTimeout = timeout;
        await ctx.RespondAsync($"Markov timeout is now set to: `{Data.ResponseTimeout}`");
    }

    [Command("chainlength"), Description("Sets markov chain length (-1 or null for default)")]
    public async Task MarkovChainLenCommand(CommandContext ctx, int? min, int? max) 
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

    [Command("save")]
    public async Task MarkovSaveCommand(CommandContext ctx)
        => Markov.SaveMarkovBinaryData();

    [Command("load")]
    public async Task MarkovLoadCommand(CommandContext ctx)
        => Markov.LoadMarkovBinaryData();
}

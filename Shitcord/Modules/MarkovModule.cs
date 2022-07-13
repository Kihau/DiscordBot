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

    [Command("disable"), Description("Disables markov data collection")]
    public async Task MarkovDisableCommand(CommandContext ctx) => Data.IsEnabled = false;

    [Command("enable"), Description("Enables markov data collection")]
    public async Task MarkovEnableCommand(CommandContext ctx) => Data.IsEnabled = true; 

    // TODO: Change name of this command (also respond when someone taggs the bot)
    //[Command("markov")]
    //public async Task MarkovCommand(CommandContext ctx, [RemainingText] string question) 
    //    => await ctx.RespondAsync(Markov.GenerateMarkovString());

    //[Command("markovfeed")]
    //public async Task MarkovFeedCommand(CommandContext ctx)
    //{
    //    Markov.GatherData = !Markov.GatherData;
    //    await ctx.RespondAsync($"Markov learning is now set to: `{Markov.GatherData}`");
    //}

    [Command("save")]
    public async Task MarkovSaveCommand(CommandContext ctx)
        => Markov.SaveMarkovBinaryData();

    [Command("load")]
    public async Task MarkovLoadCommand(CommandContext ctx)
        => Markov.LoadMarkovBinaryData();
}

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

//[Group("audio")]
[Description("Fun and silly commands")]
public class FunModule : BaseCommandModule 
{
    private MarkovService Markov { get; init; }

    public FunModule(MarkovService service) => this.Markov = service;

    // TODO: Change name of this command (also respond when someone taggs the bot)
    [Command("markov")]
    public async Task MarkovCommand(CommandContext ctx, [RemainingText] string question) 
        => await ctx.RespondAsync(Markov.GenerateMarkovString());

    [Command("markovfeed")]
    public async Task MarkovFeedCommand(CommandContext ctx)
    {
        Markov.GatherData = !Markov.GatherData;
        await ctx.RespondAsync($"Markov learning is now set to: `{Markov.GatherData}`");
    }

    [Command("markovsave")]
    public async Task MarkovSaveCommand(CommandContext ctx)
        => Markov.SaveMarkovBinaryData();

    [Command("markovload")]
    public async Task MarkovLoadCommand(CommandContext ctx)
        => Markov.LoadMarkovBinaryData();

    [Command("sex")]
    public async Task SexCommand(CommandContext ctx) 
        => await ctx.RespondAsync("Sex is not enabled on this server.");

    [Command("hlep")]
    public async Task HlepCommand(CommandContext ctx) 
        => await ctx.RespondAsync("https://tenor.com/view/falling-bread-bread-gif-19081960");
}

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

[Description("Fun and silly commands")]
public class FunModule : BaseCommandModule 
{
    [Command("sex")]
    public async Task SexCommand(CommandContext ctx) 
        => await ctx.RespondAsync("Sex is not enabled on this server.");

    [Command("hlep")]
    public async Task HlepCommand(CommandContext ctx) 
        => await ctx.RespondAsync("https://tenor.com/view/falling-bread-bread-gif-19081960");
}

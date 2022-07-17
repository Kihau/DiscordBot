using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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
    
    // TODO: Shake command (that randomly throws user across all voice channels)
}

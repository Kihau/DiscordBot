using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Shitcord.Extensions;
using ExtensionMethods = Shitcord.Extensions.ExtensionMethods;

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

    [Command("httpcat"), Aliases("http")]
    [Description("Get http error reply")]
    public async Task HttpErrorCommand(CommandContext ctx, int code) {
        if (ExtensionMethods.WebConnectionOk($"https://http.cat/{code}"))
            await ctx.RespondAsync($"https://http.cat/{code}");
        else throw new CommandException("404 - HttpCat not found");
    }

    [Command("mcseed")]
    public async Task MCSeedCommand(CommandContext ctx) 
        => await ctx.RespondAsync(new Random().NextInt64().ToString());
    
    // TODO: Shake command (that randomly throws user across all voice channels)
}

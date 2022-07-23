using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
    
    [Command("shakeuser"), Aliases("shake")]
    public async Task ShakeUserCommand(CommandContext ctx, DiscordMember member) 
    {
        var vchannels = ctx.Guild.Channels
            .Where(x => x.Value.Type == ChannelType.Voice)
            .Select(x => x.Value)
            .ToArray();

        if (vchannels.Length == 0)
            throw new CommandException("There is no voice channels?");

        var rng = new Random();
        for (int i = 0; i < 10; i++) {
            var rng_index = rng.Next(vchannels.Length);
            await member.ModifyAsync(x => x.VoiceChannel = vchannels[rng_index]);
        }
    }
}

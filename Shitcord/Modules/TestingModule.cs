using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Shitcord.Extensions;

namespace Shitcord.Modules;

//[Group("testing")]
[Description("Memes and command testing")]
public class TestingModule : BaseCommandModule
{
    private DiscordBot Bot { get; }

    public TestingModule(DiscordBot bot)
    {
        this.Bot = bot;
    }

    [Command("edit")]
    [Description("No clue why its here, but I don't want to remove it")]
    public async Task NotAPingCommand(CommandContext ctx, ulong id, [RemainingText] string text)
    {
        var message = await this.Bot.LastChannel.GetMessageAsync(id);
        await message.ModifyAsync(text);
    }

    [Command("write")]
    [Description("Why did I add this command?")]
    public async Task WriteCommand(
        CommandContext ctx, uint count = 1, DiscordChannel? channel = null
    ) {
        channel ??= ctx.Channel;
        for (var i = 0; i < count; i++)
            await channel.SendMessageAsync("hello");
    }


    //[Command("playfile")]
    //[Description("don't use it")]
    //public async Task MoanCommand(CommandContext ctx, string file)
    //{
    //    var result = await this.Audio.GetTracksAsync(new FileInfo($"Resources/{file}"));
    //    this.Data.Enqueue(result.Tracks.First());
    //    await this.Data.PlayAsync();
    //}


    [Command("test")]
    [Description("Nothing to see here, just a simple test")]
    public async Task LoopCommand(CommandContext ctx)
    {
        await ctx.RespondAsync("works i guess");
        throw new Exception("shit happened");
    }

    [Command("throw")]
    [Description("YEEEEET")]
    public Task ThrowCommand(CommandContext ctx, [RemainingText] string message = "null")
    {
        throw new CommandException(message);
    }

    [Command("embed")]
    [Description("embed testing and stuff")]
    public async Task EmbedCommand(CommandContext ctx, [RemainingText] string message = "null")
    {
        var embed = new DiscordEmbedBuilder()
            .WithTitle("Halo frisk?")
            .WithDescription($"o taki [link](https://duckduckgo.com)?")
            .WithColor(DiscordColor.Purple)
            .Build();
        
        await ctx.Channel.SendMessageAsync(embed);
    }

    [Command("button")]
    public async Task ButtonCommand(CommandContext ctx)
    {
        var myButton = new DiscordButtonComponent(ButtonStyle.Primary, "first", "Shuffle");
        var myButton2 = new DiscordButtonComponent(ButtonStyle.Success, "second", "Random");

        var builder = new DiscordMessageBuilder()
            .WithContent("Test message")
            .AddComponents(myButton)
            .AddComponents(myButton2);

        await ctx.Channel.SendMessageAsync(builder);

        this.Bot.Client.ComponentInteractionCreated += async (sender, e) =>
        {
            var message = new DiscordFollowupMessageBuilder()
                .WithContent("STOP IT");
            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().WithContent("Update"));
        };
    }
}

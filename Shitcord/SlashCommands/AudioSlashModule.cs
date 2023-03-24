using DSharpPlus.SlashCommands;

namespace Shitcord.SlashCommands;

public class AudioSlashModule : ApplicationCommandModule
{
    [SlashCommand("test", "A slash command made to test the DSharpPlusSlashCommands library!")]
    public async Task TestCommand(InteractionContext ctx) { }
}

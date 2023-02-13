using DSharpPlus;
using DSharpPlus.EventArgs;

namespace Shitcord.Services;

// TODO: Don't do that. Do this the dumb way instead.
// NOTE: This service is called from the Markov Service since determining the bot output is
//       strictly related to it (more about this in notes in MarkovService class).
public class OpenAIService
{
    private DiscordClient Client { get; }
    private OpenAIConfig Config { get; }

    public OpenAIService(DiscordBot bot) {
        Client = bot.Client;
        Config = bot.Config.OpenAI;

        Client.MessageCreated += OpenAIMessageHandler;
    }

    public string CreateAIRequest(string input) {
        throw new NotImplementedException();
    }

    private Task OpenAIMessageHandler(DiscordClient client, MessageCreateEventArgs e) {
        // Task.Run(async () => {
        Task.Run(() => {
            if (e.Author.IsBot)
                return;

            if (!Config.AllowedUsers.Contains(e.Author.Id)) 
                return;

            // When string is more than just a mention create the openai request
        });
        return Task.CompletedTask;
    }
}

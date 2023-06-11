using Shitcord;

// TODO: Custom help handler
//
// NOTE: We could store custom prefix for each guild and use Config.Prefix
//       as a default prefix.
static class Program
{
    static void Main(string[] args)
    {
        #if DEBUG
            BotConfig config = new BotConfig("Resources/config-example.json");
        #else
            BotConfig config = new BotConfig("Resources/config.json");
        #endif

        try {
            var shitcord = new DiscordBot(config);
            shitcord.RunAsync().GetAwaiter().GetResult();
        } catch {
            // Dispose the bot here
            // Save stuff to file (logging)
            throw;
        }
    }
}

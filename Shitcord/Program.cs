using Shitcord;

// TODO: Add a way to create global commands at runtime (might require custom help handler)
// TODO: Custom help handler
// TODO: Create one discord message (a terminal) and print actual terminal output (add interaction button)
//
// NOTE: We could store custom prefix for each guild and use Config.Prefix
//       as a default prefix.
static class Program
{
    static void Main(string[] args)
    {
        #if DEBUG
            BotConfig config = new BotConfig("Resources/config-debug.json");
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

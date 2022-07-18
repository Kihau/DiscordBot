using Shitcord;

// TODO: Create one discord message (a terminal) and print actual terminal output (add interaction button)
// TODO: In config file store default prefix - guild prefixes store in database
// TODO: Add option to set timeout time, pause or leave on timeout, enabled disabled - store then in database
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

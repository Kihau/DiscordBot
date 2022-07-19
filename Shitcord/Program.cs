using System.Diagnostics;
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

    static void Main2(string[] args) 
    {
        // ssh user@host -p port -i keyfile
        var start_info = new ProcessStartInfo 
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            FileName = "ssh",
            Arguments = String.Join(' ', args),
        };

        var ssh_process = Process.Start(start_info);

        if (ssh_process == null) {
            Console.WriteLine("Could not start the process");
            return;
        }

        Thread.Sleep(1000);

        Console.WriteLine(ssh_process.StandardOutput.ReadToEnd());
        Console.WriteLine(ssh_process.StandardError.ReadToEnd());

        /*
         * Example SshService message payload:
         *
         * ```bash       <- \n
         * $ ls          <- \n
         * <some stuff>  <- \n
         * ```           <- \n
         *
         */

        // vvvvvvvvvvvvvvvvvvvvvvv The limit might be larger for bots
        // discord character limit - ``` - ``` - bash - 2x newline char
        const int buffer_size = 2000 - 3 - 3 - 4 - 2;
        var buffer = new char[buffer_size];
        buffer[0] = 'h';
        buffer[1] = 'e';
        buffer[2] = 'y';
        buffer[3] = '\n';
        int cursor_pos = 0;

        while (true) {
            var chars_read = ssh_process.StandardOutput.ReadBlock(
               buffer, cursor_pos, buffer_size - 1
            );

            Console.WriteLine(new String(buffer));

            cursor_pos += chars_read;
            var input_line = Console.ReadLine();
            ssh_process.StandardInput.WriteLine(input_line);

            Thread.Sleep(1000);
        }
    }
}

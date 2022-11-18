using System.Diagnostics;
using Shitcord;
using Shitcord.Data;

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

    static void Main1(string[] args) {
        GlobalData.StaticInitalize();
        // var data = new List<WhitelistEntry>();
        // data.Add(new WhitelistEntry {
        //     userid = 123123,
        //     username = "Michal"
        // });
        // string json = JsonSerializer.Serialize(data);
        // File.WriteAllText(GlobalData.whitelist_path, json);
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
         * ```bash             <- \n   
         * $ touch grass       <- \n   <─┐  
         * $ ls                <- \n     │ Buffer
         * Stuff/ Temp/ grass  <- \n   <─┘
         * ```           
         *
         */

        // vvvvvvvvvvvvvvvvvvvvvvv The limit might be larger for bots
        // discord character limit - ``` - ``` - bash - newline char
        const int buffer_size = 2000 - 3 - 3 - 4 - 1;
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

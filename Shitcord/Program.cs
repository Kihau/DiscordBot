using System.Reflection;
using System.Threading.Channels;
using System.Xml;
using Shitcord;
using Shitcord.Data;

// TODO: Create one discord message (a terminal) and print actual terminal output (add interaction button)
// TODO: In config file store default prefix - guild prefixes store in database
// TODO: Add option to set timeout time, pause or leave on timeout, enabled disabled - store then in database
class Program
{
    static void Main(string[] args)
    {
        var shitcord = new Discordbot();
        shitcord.RunAsync().GetAwaiter().GetResult();
    }    
}

//var files = Directory.GetFiles("Resources");
//foreach(var file_path in files) {
// Console.WriteLine(Path.GetFileNameWithoutExtension(file_path));
//}

// var config = new Config("Resources/config.json");
// Console.WriteLine(config.Discord.Token);

//File.Exists("");
using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Shitcord.Data;

namespace Shitcord.Services;

public class LavalinkService
{
    public LavalinkNodeConnection Node { get; set; }
    public DiscordClient Client { get; }
    private LavalinkConfig Config { get; }
    public bool IsEnabled => Config.IsEnabled;

    #pragma warning disable CS8618
    public LavalinkService(Discordbot bot)
    #pragma warning restore CS8618
    {
        Config = bot.Config.Lava;
        Client = bot.Client;
        
        if (IsEnabled)
            Client.Ready += Client_Ready;
    }
    
    private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
    {
        var lava = sender.GetLavalink();
        var config = ConfigureLavalink();

        try {
            Node = lava.ConnectAsync(config).GetAwaiter().GetResult();
        } catch {
            Environment.Exit(-1);
        }

        return Task.CompletedTask;
    }

    private LavalinkConfiguration ConfigureLavalink()
    {
        var endPoint = new ConnectionEndpoint() {
            Hostname = Config.Hostname,
            Port = Config.Port
        };

        var lavaConfig = new LavalinkConfiguration() {
            Password = Config.Password,
            SocketEndpoint = endPoint,
            RestEndpoint = endPoint,
            SocketAutoReconnect = false
        };

        return lavaConfig;
    }
}

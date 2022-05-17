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
    public DiscordClient Client { get; init; }
    private LavalinkConfig Config { get; init; }
    public bool IsEnabled => Config.IsEnabled;

    public LavalinkService(Discordbot bot)
    {
        this.Config = bot.Config.Lava;
        this.Client = bot.Client;
        
        if (this.IsEnabled)
            this.Client.Ready += Client_Ready;
    }
    
    private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
    {
        var lava = sender.GetLavalink();
        var config = this.ConfigureLavalink();

        try
        {
            this.Node = lava.ConnectAsync(config).GetAwaiter().GetResult();
        }
        catch
        {
            Environment.Exit(-1);
        }

        return Task.CompletedTask;
    }

    private LavalinkConfiguration ConfigureLavalink()
    {
        var endPoint = new ConnectionEndpoint()
        {
            Hostname = this.Config.Hostname,
            Port = this.Config.Port,
        };

        var lavaConfig = new LavalinkConfiguration()
        {
            Password = this.Config.Password,
            SocketEndpoint = endPoint,
            RestEndpoint = endPoint,
            SocketAutoReconnect = false,
        };

        return lavaConfig;
    }
}

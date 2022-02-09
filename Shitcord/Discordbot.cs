using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Shitcord.Modules;
using Shitcord.Data;
using Shitcord.Services;

namespace Shitcord;

public class Discordbot
{
    public DiscordClient Client { get; private set; }
    public Config Config { get; }

    public DiscordChannel? LastChannel { get; set; }
    public DiscordGuild? LastGuild { get; set; }
    public DateTime StartTime { get; }

    public Discordbot()
    {
        this.StartTime = DateTime.Now;
        //var exec_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#if DEBUG 
        this.Config = new Config($"Resources/config-kihau.json");
#else 
        this.Config = new Config($"Resources/config.json");
#endif
        this.ConfigureClient();
        this.ConfigureCommands();
    }
    
    private void ConfigureClient()
    {
        var botConf = this.Config.Discord;
        var clientConfig = new DiscordConfiguration()
        {
            Token = botConf.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged,
            MinimumLogLevel = LogLevel.Information,
            AutoReconnect = true,
            MessageCacheSize = botConf.CacheSize,
            
            // TODO: Custom logger
            //LoggerFactory = new SickAssCustomLogger();
        };

        Client = new DiscordClient(clientConfig);
        Client.MessageCreated += PrintMessage;
        
        if (this.Config.Lava.IsEnabled)
            Client.UseLavalink();
    }
    
    private Task PrintMessage(DiscordClient client, MessageCreateEventArgs e)
    {
        Console.WriteLine($"[{e.Guild.Name}] {e.Author.Username}@{e.Channel.Name}: {e.Message.Content}");
        
        if (e.Author.Id == 278778540554715137)
        {
            LastChannel = e.Channel;
            LastGuild = e.Guild;
        }

        return Task.CompletedTask;
    }

    private void ConfigureCommands()
    {
        var services = this.CreateServices();

        var cmdConfig = new CommandsNextConfiguration()
        {
            StringPrefixes = new[] {this.Config.Discord.Prefix},
            EnableDms = false,
            CaseSensitive = false,
            EnableMentionPrefix = false,
            EnableDefaultHelp = true,
            UseDefaultCommandHandler = true,
            Services = services
        };

        var commands = Client.UseCommandsNext(cmdConfig);

        //commands.RegisterCommands(Assembly.GetExecutingAssembly());

        if (this.Config.Lava.IsEnabled)
            commands.RegisterCommands<AudioModule>();
        commands.RegisterCommands<UtilityModule>();
#if DEBUG
        commands.RegisterCommands<TestingModule>();
#endif

        commands.CommandErrored += async (sender, e) =>
        {
#if !DEBUG
            if (e.Exception is CommandException)
#endif
            {

                var embed = new DiscordEmbedBuilder();
                embed.WithTitle("<:angerysad:690223823936684052>  |  A Wild Error Occurred: ")
                    .WithDescription(e.Exception.Message)
                    .WithColor(DiscordColor.Red);
                await e.Context.Channel.SendMessageAsync(embed.Build());
            }

            Console.WriteLine($"Exception thrown: {e.Exception}");
        };
    }

    private IServiceProvider CreateServices()
    {
        var collection = new ServiceCollection()
            .AddSingleton<AudioService>()
            .AddSingleton<LavalinkService>()
            .AddSingleton<SshService>()
            .AddSingleton<TimeService>()
            .AddSingleton(this);

        var services = collection.BuildServiceProvider();
        return services;
    }

    public async Task RunAsync()
    {
        if (this.Config.Lava.IsEnabled && this.Config.Lava.AutoStart)
        {
            var startInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                FileName = this.Config.Lava.JavaPath,
                Arguments = "-jar -Djava.io.tmpdir=temp/ lavalink.jar"
            };

            Directory.CreateDirectory("temp");
            Process.Start(startInfo);
        }
        
        await Task.Delay(this.Config.Discord.StartDelay);
        var activity = new DiscordActivity(this.Config.Discord.Status, ActivityType.ListeningTo);
        await Client.ConnectAsync(activity, UserStatus.DoNotDisturb);

        while (true)
        {
            string message = Console.ReadLine() ?? "";

            string cmd = message;
            if (message.Length > 4)
                cmd = message.TrimStart().Substring(0, 4);

            switch (cmd)
            {
                case ">>cc":
                    if (LastGuild == null) continue;
                    var channel_name = message.TrimStart().Substring(4).Trim();
                    foreach (var channel in this.LastGuild.Channels.Values)
                        if (channel.Name.Contains(channel_name, StringComparison.OrdinalIgnoreCase) &&
                            channel.Type == ChannelType.Text)
                            this.LastChannel = channel;
                    if (LastChannel != null)
                        Console.WriteLine($"Current channel set to: {LastChannel.Name}");
                    break;
                
                case ">>cg":
                    var guild_name = message.TrimStart().Substring(4).Trim();
                    foreach (var guild in this.Client.Guilds.Values)
                        if (guild.Name.Contains(guild_name, StringComparison.OrdinalIgnoreCase))
                            this.LastGuild = guild;
                    if (LastGuild != null)
                        Console.WriteLine($"Current guild set to: {LastGuild.Name}");
                    break;
                
                case ">>lg":
                    int i = 0;
                    foreach (var guild in this.Client.Guilds.Values)
                        Console.WriteLine($"{i++}. {guild.Name}");
                    break;
                
                case ">>lc":
                    if (this.LastGuild == null) continue;
                    int j = 1;
                    foreach (var channel in this.LastGuild.Channels.Values)
                        if (channel.Type == ChannelType.Text)
                            Console.WriteLine($"{j++}. {channel.Name}");
                    break;
                
                default:
                    if (LastChannel != null && !String.IsNullOrWhiteSpace(message))
                        await LastChannel.SendMessageAsync(message);
                    break;
            }
        }
    }
}
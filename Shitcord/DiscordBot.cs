using System.Diagnostics;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Shitcord.Modules;
using Shitcord.Services;
using Shitcord.Extensions;

namespace Shitcord;

public class DiscordBot
{
    public DiscordClient Client { get; private set; }
    public BotConfig Config { get; }

    public DiscordChannel LastChannel { get; set; }
    public DiscordGuild LastGuild { get; set; }

    public DateTime StartTime { get; }

#if DEBUG
    public bool DebugEnabled { get; set; } = true;
#else
    public bool DebugEnabled { get; set; } = false;
#endif

#pragma warning disable CS8618
    public DiscordBot(BotConfig config)
#pragma warning restore CS8618
    {
        StartTime = DateTime.Now;
        Config = config;

        ConfigureClient();
        ConfigureCommands();
    }

    private void ConfigureClient()
    {
        var botConf = Config.Discord;
        var clientConfig = new DiscordConfiguration {
            Token = botConf.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged,
            MinimumLogLevel = LogLevel.Information,
            AutoReconnect = true,
            MessageCacheSize = botConf.CacheSize,
            LoggerFactory = new BotLoggerFactory(Config),
        };

        Client = new DiscordClient(clientConfig);

        Client.MessageCreated += PrintMessage;
        Client.GuildDownloadCompleted += (_, _) => {
            Task.Run(StartBotConsoleInput);
            return Task.CompletedTask;
        };

        if (Config.Lava.IsEnabled)
            Client.UseLavalink();

        Client.UseInteractivity(new InteractivityConfiguration { 
            PollBehaviour = PollBehaviour.KeepEmojis,
            Timeout = TimeSpan.FromSeconds(30)
        });
    }

    private Task PrintMessage(DiscordClient client, MessageCreateEventArgs e)
    {
        if (DebugEnabled || e.Author == client.CurrentUser) {
            string message_content = String.IsNullOrWhiteSpace(e.Message.Content) 
                ? "<Empty message>" : e.Message.Content;

            Console.WriteLine(
                $"[{e.Guild.Name}] {e.Author.Username}@{e.Channel.Name}: {message_content}"
            );
        }

        return Task.CompletedTask;
    }

    private async Task StartBotConsoleInput() 
    {
        ulong debug_channel = 928054033741152307;
        
        try {
            LastChannel = await Client.GetChannelAsync(debug_channel);
            LastGuild = LastChannel.Guild;
        } catch {
            LastGuild = Client.Guilds.First().Value;
            LastChannel = LastGuild.Channels
                .First(x => x.Value.Type == ChannelType.Text).Value;
        }
        
        Console.WriteLine($"Current guild set to: {LastGuild.Name}");
        Console.WriteLine($"Current channel set to: {LastChannel.Name}");

        while (true) {
            string? message = Console.ReadLine()?.Trim();

            if (message is null)
                continue;

            var success = ConsoleCommandHandler(message);
            if (!success && DebugEnabled)
                await LastChannel.SendMessageAsync(message);
        }
    }

    private bool ConsoleCommandHandler(string msg)
    {
        var cnext = Client.GetCommandsNext();

        msg = msg.Trim();
        if (!msg.StartsWith(Config.Discord.Prefix))
            return false;

        var cmd_name = msg.Remove(0, Config.Discord.Prefix.Length).Split(' ').First();
        var command = cnext.FindCommand(cmd_name, out var args);

        if (command is null) {
            Client.Logger.LogError(new EventId(1, "CCHandler"), "Command not found");
            return true;
        }

        args = msg.Substring(Config.Discord.Prefix.Length + cmd_name.Length).Trim();
        var ctx = cnext.CreateFakeContext(
            Client.CurrentUser,
            LastChannel,
            msg,
            Config.Discord.Prefix,
            command,
            args
        );

        var _ = Task.Run(async () => await cnext.ExecuteCommandAsync(ctx));
        return true;
    }

    private void ConfigureCommands()
    {
        var services = CreateServices();

        var cmdConfig = new CommandsNextConfiguration {
            StringPrefixes = new[] { Config.Discord.Prefix },
            EnableDms = false,
            CaseSensitive = false,
            EnableMentionPrefix = false,
            EnableDefaultHelp = true,
            UseDefaultCommandHandler = true,
            Services = services
        };

        var commands = Client.UseCommandsNext(cmdConfig);

        if (Config.Lava.IsEnabled)
            commands.RegisterCommands<AudioModule>();
        commands.RegisterCommands<UtilityModule>();
        commands.RegisterCommands<AuthModule>();
        commands.RegisterCommands<FunModule>();
        commands.RegisterCommands<MarkovModule>();

        if (DebugEnabled)
            commands.RegisterCommands<TestingModule>();

        commands.CommandExecuted += (sender, e) => {
            Client.Logger.LogInformation(new EventId(2, "Executed"),
                $"Called: {e.Command.Name} by {e.Context.User.Id} in " +
                $"{e.Context.Guild.Id}@{e.Context.Channel.Id}"
            ); 
            return Task.CompletedTask;
        };

        commands.CommandErrored += async (sender, e) => {
            Client.Logger.LogError(new EventId(0, "Exception"), $"{e.Exception}"); 
                
            if (!DebugEnabled && e.Exception is CommandException)
                return;

            var embed = new DiscordEmbedBuilder();
            embed.WithTitle("<:angerysad:690223823936684052>  |  A Wild Error Occurred: ")
                .WithDescription(e.Exception.Message)
                .WithColor(DiscordColor.Red);
            await e.Context.Channel.SendMessageAsync(embed.Build());
        };
    }

    private IServiceProvider CreateServices()
    {
        var collection = new ServiceCollection()
            .AddSingleton<DatabaseService>()
            .AddSingleton<SshService>()
            .AddSingleton<WeatherService>()
            .AddSingleton<AutoReplyService>()
            .AddSingleton<MarkovService>()
            .AddSingleton<ModerationService>()
            .AddSingleton(this);

        if (Config.Lava.IsEnabled) {
            collection.AddSingleton<AudioService>();
            collection.AddSingleton<LavalinkService>();
        }

        var services = collection.BuildServiceProvider();
        return services;
    }

    public async Task RunAsync()
    {
        if (Config.Lava.IsEnabled && Config.Lava.AutoStart) {
            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                FileName = Config.Lava.JavaPath,
                Arguments = "-jar -Djava.io.tmpdir=temp/ lavalink.jar"
            };

            Directory.CreateDirectory("temp");
            Process.Start(startInfo);
        }

        await Task.Delay(Config.Discord.StartDelay);
        var activity = new DiscordActivity(Config.Discord.Status, ActivityType.ListeningTo);
        await Client.ConnectAsync(activity, UserStatus.DoNotDisturb);

        Thread.Sleep(Timeout.Infinite);
    }
}

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
using Shitcord.Data;

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
    public DiscordBot(BotConfig config) {
#pragma warning restore CS8618
        StartTime = DateTime.Now;
        Config = config;

        GlobalData.StaticInitalize();
        ConfigureClient();
        ConfigureCommands();
    }

    private void ConfigureClient()
    {
        var clientConfig = new DiscordConfiguration {
            Token = Config.Discord.Token,
            TokenType = TokenType.Bot,
            // TokenType = TokenType.User,
            Intents = DiscordIntents.All,
            // Intents = DiscordIntents.AllUnprivileged,
            // TODO: Change(?) it vvvvvvvvvvv
            // NOTE: This does nothing - the log level is set in the BotLogger class
            MinimumLogLevel = Config.Logging.MinLogLevel,
            AutoReconnect = true,
            MessageCacheSize = Config.Discord.CacheSize,
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

        // Timestamp parser for the seek command (in AudioModule)
        commands.RegisterConverter(new SeekstampArgumentConverter());

        // Registering all bot commands
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
                // TODO: Check: Changed this to print entire command (does this work?)
                $"Called: {e.Command.Name} with {e.Context.RawArgumentString} by {e.Context.User.Id} in " +
                $"{e.Context.Guild.Id}@{e.Context.Channel.Id}"
            ); 
            return Task.CompletedTask;
        };

        commands.CommandErrored += async (sender, e) => {
            Client.Logger.LogError(new EventId(0, "Exception"), $"{e.Exception}"); 
               
            // When Could not find suitable overload - print required arguments
            // TODO: Improve this and don't catch obvious exceptions?
            // if (!DebugEnabled && e.Exception is not CommandException)
            //     return;
            
            var embed = new DiscordEmbedBuilder();
            switch (e.Exception) {
                // TODO: When Cound not find suitable overload - print required arguments
                default: 
                    embed.WithTitle("<:angerysad:690223823936684052>  |  A Wild Error Occurred: ")
                        .WithDescription(e.Exception.Message)
                        .WithColor(DiscordColor.Red);
                    break;
            };

            await e.Context.Channel.SendMessageAsync(embed.Build());
        };
    }

    private IServiceProvider CreateServices()
    {
        var collection = new ServiceCollection()
            .AddSingleton<DatabaseService>()
            .AddSingleton<SshService>()
            .AddSingleton<WeatherService>()
            .AddSingleton<ReplyService>()
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
            if (!File.Exists("lavalink.jar")) {
                throw new FileNotFoundException(
                    "Cannot autostart lavalink - please place lavalink.jar file in the same " +
                    "directory as the bot executable file"
                );
            }

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

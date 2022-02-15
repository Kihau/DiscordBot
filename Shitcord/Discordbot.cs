using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

	public TimeSpan TotalRuntime => DateTime.Now - this.StartTime;
	public TimeSpan TotalUptime => this.TotalRuntime - this.TotalDowntime;
	private TimeSpan TotalDowntime { get; set; }

	public float UptimePercentage => this.TotalUptime.Ticks / (float) this.TotalRuntime.Ticks * 10.0f;

	public DateTime LastDisconnect { get; private set; }
	public bool IsDisconnected { get; private set; } = false;

	public Discordbot()
	{
		this.StartTime = DateTime.Now;
		//var exec_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#if DEBUG
		this.Config = new Config("Resources/config-kihau.json");
#else
        this.Config = new Config("Resources/config.json");
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

		// Client.SocketClosed += (o, sender) =>
		// {
		//     this.LastDisconnect = DateTime.Now;
		//     this.IsDisconnected = true;
		//     return Task.CompletedTask;
		// };
		//
		// Client.ClientErrored += (o, sender) =>
		// {
		//     Console.WriteLine($"errored at: {sender.Exception.Message}");
		//     return Task.CompletedTask;
		// };
		//
		// Client.SocketErrored += (o, sender) =>
		// {
		//     Console.WriteLine($"shit happened: {sender.Exception.Message}");
		//     return Task.CompletedTask;            
		// };
		//
		// Client.SocketOpened+= (o, sender) =>
		// {
		//     if (this.IsDisconnected)
		//         this.TotalDowntime += DateTime.Now - this.LastDisconnect;
		//     this.IsDisconnected = false;
		//     return Task.CompletedTask;
		// };
		//

		if (this.Config.Lava.IsEnabled)
			Client.UseLavalink();
	}

	private Task PrintMessage(DiscordClient client, MessageCreateEventArgs e)
	{
		Console.WriteLine($"[{e.Guild.Name}] {e.Author.Username}@{e.Channel.Name}: {e.Message.Content}");

		if (e.Author.Id != 278778540554715137)
			return Task.CompletedTask;

		LastChannel = e.Channel;
		LastGuild = e.Guild;

		return Task.CompletedTask;
	}

	private void ConfigureCommands()
	{
		var services = this.CreateServices();

		var cmdConfig = new CommandsNextConfiguration
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
			if (!StaticData.DebugEnabled && e.Exception is not CommandException)
				return;

			var embed = new DiscordEmbedBuilder();
			embed.WithTitle("<:angerysad:690223823936684052>  |  A Wild Error Occurred: ")
				.WithDescription(e.Exception.Message)
				.WithColor(DiscordColor.Red);
			await e.Context.Channel.SendMessageAsync(embed.Build());

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

	// TODO: Finalize
	private async Task ConsoleCommandHandler(string msg)
	{
		if (this.LastChannel is null)
			return;
		
		var cnext = this.Client.GetCommandsNext();

		msg = msg.Trim();
		if (!msg.StartsWith(this.Config.Discord.Prefix))
			return;
		
		var cmd_name = msg.Remove(0, this.Config.Discord.Prefix.Length).Split(' ').First();
		var command = cnext.FindCommand(cmd_name, out var args);
		
		// TODO: if command is null check for internal console commands
		
		var ctx = cnext.CreateFakeContext(
			this.Client.CurrentUser, 
			this.LastChannel, 
			msg, 
			this.Config.Discord.Prefix, 
			command,
			args
		);

		var _ = Task.Run(async () => await cnext.ExecuteCommandAsync(ctx));
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
		//await Client.ConnectAsync(activity, UserStatus.DoNotDisturb);

		while (true)
		{
			string message = Console.ReadLine()?.Trim() ?? "";

			if (!message.StartsWith(this.Config.Discord.Prefix))
			{
				if (LastChannel != null && !String.IsNullOrWhiteSpace(message))
					await LastChannel.SendMessageAsync(message);
				continue;
			}

			string cmd = message.Remove(0, this.Config.Discord.Prefix.Length);
			var cmd_args = cmd.Split(' ');

			if (cmd_args.Length == 0)
				continue;

			switch (cmd_args[0])
			{
				case "cc":
					if (LastGuild == null || cmd_args.Length < 2) continue;
					var channel_name = cmd_args[1];
					foreach (var channel in this.LastGuild.Channels.Values)
						if (channel.Name.Contains(channel_name, StringComparison.OrdinalIgnoreCase) &&
						    channel.Type == ChannelType.Text)
							this.LastChannel = channel;
					if (LastChannel != null)
						Console.WriteLine($"Current channel set to: {LastChannel.Name}");
					break;

				case "cg":
					if (cmd_args.Length < 2) continue;
					var guild_name = cmd_args[1];
					foreach (var guild in this.Client.Guilds.Values)
						if (guild.Name.Contains(guild_name, StringComparison.OrdinalIgnoreCase))
							this.LastGuild = guild;
					if (LastGuild != null)
						Console.WriteLine($"Current guild set to: {LastGuild.Name}");
					break;

				case "lg":
					int i = 0;
					foreach (var guild in this.Client.Guilds.Values)
						Console.WriteLine($"{i++}. {guild.Name}");
					break;

				case "lc":
					if (this.LastGuild == null) continue;
					int j = 1;
					foreach (var channel in this.LastGuild.Channels.Values)
						if (channel.Type == ChannelType.Text)
							Console.WriteLine($"{j++}. {channel.Name}");
					break;

				// default:
				//     if (LastChannel != null && !String.IsNullOrWhiteSpace(message))
				//         await LastChannel.SendMessageAsync(message);
				//     break;
			}
		}
	}
}
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Loader;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shitcord.Extensions;
using Shitcord.Data;
using Shitcord.Modules;
using Shitcord.Services;

namespace Shitcord;

public class Discordbot
{
	public DiscordClient Client { get; private set; }
	public Config Config { get; }

	public DiscordChannel LastChannel { get; set; }
	public DiscordGuild LastGuild { get; set; }

	public DateTime StartTime { get; }

	public TimeSpan TotalRuntime => DateTime.Now - this.StartTime;
	public TimeSpan TotalUptime => this.TotalRuntime - this.TotalDowntime;
	private TimeSpan TotalDowntime { get; set; }

	public float UptimePercentage => this.TotalUptime.Ticks / (float) this.TotalRuntime.Ticks * 10.0f;

	public DateTime LastDisconnect { get; private set; }
	public bool IsDisconnected { get; private set; } = false;

#if DEBUG
	public bool DebugEnabled { get; set; } = true;
#else
    public bool DebugEnabled { get; set; } = false;
#endif

	
	public Discordbot()
	{
		this.StartTime = DateTime.Now;
		//var exec_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#if DEBUG
		this.Config = new Config("Resources/config-debug.json");
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

		if (this.Config.Lava.IsEnabled)
			Client.UseLavalink();
	}

	private Task PrintMessage(DiscordClient client, MessageCreateEventArgs e)
	{
		if(this.DebugEnabled)
			Console.WriteLine($"[{e.Guild.Name}] {e.Author.Username}@{e.Channel.Name}: {e.Message.Content}");

		// if (e.Author.Id != 278778540554715137)
		// 	return Task.CompletedTask;
		//
		// LastChannel = e.Channel;
		// LastGuild = e.Guild;

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
		commands.RegisterCommands<AuthModule>();
#if DEBUG
		commands.RegisterCommands<TestingModule>();
#endif

		commands.CommandErrored += async (sender, e) =>
		{
			Console.WriteLine($"Exception thrown: {e.Exception}");

			if (!this.DebugEnabled && e.Exception is not CommandException)
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
			.AddSingleton<AudioService>()
			.AddSingleton<LavalinkService>()
			.AddSingleton<SshService>()
			.AddSingleton<TimeService>()
			.AddSingleton(this);

		var services = collection.BuildServiceProvider();
		return services;
	}

	private async Task<bool> ConsoleCommandHandler(string msg)
	{
		var cnext = this.Client.GetCommandsNext();

		msg = msg.Trim();
		if (!msg.StartsWith(this.Config.Discord.Prefix))
			return false;
		
		var cmd_name = msg.Remove(0, this.Config.Discord.Prefix.Length).Split(' ').First();
		var command = cnext.FindCommand(cmd_name, out var args);

		if (command is null)
			return false;

		args = msg.Substring(this.Config.Discord.Prefix.Length + cmd_name.Length).Trim();
		var ctx = cnext.CreateFakeContext(
			this.Client.CurrentUser, 
			this.LastChannel, 
			msg, 
			this.Config.Discord.Prefix, 
			command,
			args
		);

		var _ = Task.Run(async () => await cnext.ExecuteCommandAsync(ctx));

		return true;
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
		
		ulong debug_channel = 928054033741152307;
		try
		{
			this.LastChannel = await this.Client.GetChannelAsync(debug_channel);
			await Task.Delay(3000);
			this.LastGuild = this.LastChannel.Guild;
		}
		catch
		{
			this.LastGuild = this.Client.Guilds.First().Value;
			this.LastChannel = this.LastGuild.Channels
				.First(x => x.Value.Type == ChannelType.Text).Value;
		}

		Console.WriteLine($"Current guild set to: {this.LastGuild.Name}");
		Console.WriteLine($"Current channel set to: {this.LastChannel.Name}");
		
		while (true)
		{
			string? message = Console.ReadLine()?.Trim();
			
			if (message is null) 
				continue;
			
			var success = await this.ConsoleCommandHandler(message);
			if (!success && this.DebugEnabled)
				await this.LastChannel.SendMessageAsync(message);
		}
	}
}
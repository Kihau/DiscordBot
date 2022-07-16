//using System.ComponentModel;

using System.Diagnostics;
using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Shitcord.Extensions;
using Shitcord.Services;

namespace Shitcord.Modules;

[RequireAuthorized]
public class AuthModule : BaseCommandModule
{
	public Discordbot Bot { get; }
    public DatabaseService Db { get; }

	public AuthModule(Discordbot bot, DatabaseService db) {
		this.Bot = bot;
        this.Db = db;
    }

	public override async Task BeforeExecutionAsync(CommandContext ctx)
	{
		//this.Data = this.Audio.GetOrAddData(ctx.Guild);
		await base.BeforeExecutionAsync(ctx);
	}

	[Command("shutdown"), Aliases("exit")]
	[Description("literally don't even try")]
	public async Task ShutdownCommand(CommandContext ctx)
	{
		await ctx.RespondAsync("Shutting down");
		Environment.Exit(0);
	}

	[Command("memoryusage"), Aliases("memuse"), Description("Displays bot memory usage")]
	public async Task MemoryUsageCommand(CommandContext ctx)
	{
		const long MB = 1024 * 1024;
		var proc = Process.GetCurrentProcess();
		var priv_mem = $"Private memory: `{proc.PrivateMemorySize64 / MB}MB`\n";
		var page_mem = $"Paged memory: `{proc.PagedMemorySize64 / MB}MB`\n";
		var virt_mem = $"Virtual memory: `{proc.VirtualMemorySize64 / MB}MB`\n";
		var gctotal_mem = $"Total GC memory: `{GC.GetTotalMemory(false) / MB}MB`\n";
		var gcalloc_mem = $"Allocated GC memory: `{GC.GetTotalAllocatedBytes() / MB}MB`\n";
		await ctx.RespondAsync($"{priv_mem}{page_mem}{virt_mem}{gctotal_mem}{gcalloc_mem}");
	}

	[Command("garbagecollect"), Aliases("gc"), Description("Performs memory clean-up")]
	public async Task GarbageCollectCommand(CommandContext ctx)
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		await ctx.RespondAsync("Done :thumbsup: ");
	}

	[Command("debug"), Description("Enables/disables debug mode")]
	public async Task DebugCommand(CommandContext ctx)
	{
		Bot.DebugEnabled = !Bot.DebugEnabled;
		await ctx.RespondAsync($"Debug mode set to: `{Bot.DebugEnabled}`");
	}

	[Command("listchannels"), Aliases("lc"), Description("List all channels for current guild")]
	public async Task ListChannelsCommand(CommandContext ctx, bool console = true)
	{
		var voice_channels = "[Voice Channels]\n";
		var text_channels = "[Text Channels]\n";

		int i = 1;
		int j = 1;
		foreach (var channel in Bot.LastGuild.Channels.Values)
			if (channel.Type == ChannelType.Text)
				text_channels += $"{i++}. {channel.Name}\n";
			else if (channel.Type == ChannelType.Voice)
				voice_channels += $"{j++}. {channel.Name}\n";

		var all_channels = $"{voice_channels}\n{text_channels}";
		if (console)
			Console.WriteLine(all_channels);
		else await ctx.RespondAsync(all_channels);
	}

	[Command("listguilds"), Aliases("lg"), Description("List all guilds")]
	public async Task ListGuildsCommand(CommandContext ctx, bool console = true)
	{
		int i = 1;
		string guilds_str = "";
		foreach (var guild in ctx.Client.Guilds.Values)
			guilds_str += $"{i++}. {guild.Name}\n";

		if (console)
			Console.WriteLine(guilds_str);
		else await ctx.RespondAsync(guilds_str);
	}

	[Command("changechannel"), Aliases("cc"), Description("Changes channel for fake command context")]
	public async Task ChangeChannelCommand(CommandContext ctx, string? channelName = null, bool console = true)
	{
		if (channelName == null)
			Bot.LastChannel = ctx.Channel;
		else
		{
			foreach (var channel in Bot.LastGuild.Channels.Values)
			{
				if (channel.Name.Contains(channelName, StringComparison.OrdinalIgnoreCase) &&
				    channel.Type == ChannelType.Text)
					Bot.LastChannel = channel;
			}
		}

		if (console)
			Console.WriteLine($"Current channel set to: {Bot.LastChannel.Name}");
		else await ctx.RespondAsync($"Current channel set to: {Bot.LastChannel.Name}");
	}

	[Command("changeguild"), Aliases("cg"), Description("Changes guild for fake command context")]
	public async Task ChangeGuildCommand(CommandContext ctx, string guildName, bool console = true)
	{
		foreach (var guild in ctx.Client.Guilds.Values)
			if (guild.Name.Contains(guildName, StringComparison.OrdinalIgnoreCase))
				Bot.LastGuild = guild;

		if (console)
			Console.WriteLine($"Current guild set to: {Bot.LastGuild.Name}");
		else await ctx.RespondAsync($"Current guild set to: {Bot.LastGuild.Name}");
	}

	[Command("printdatabase"), Aliases("printdb"), Description("Prints database used by the bot")]
	public async Task PrintDatabaseCommand(CommandContext ctx, bool console = true)
	{
        string tables = Db.Tables();
        await ctx.RespondAsync($"```\n{tables}```\n");
	}

	[Command("printtable"), Aliases("printt"), Description("Prints table contained inside db")]
	public async Task PrintTableCommand(CommandContext ctx, string table, bool console = true)
	{
		bool exists = Db.DoesTableExist(table);
		if (!exists) {
			await ctx.RespondAsync($"\nTable doesn't exist\n");
			return;
		}
		var cols = Db.RetrieveColumns("SELECT * FROM " + table);
		if (cols == null) {
			await ctx.RespondAsync("\nTable is empty\n");
			return;
		}
		string tables = Db.QueryResultToString(cols, table);
		await ctx.RespondAsync($"```\n{tables}```\n");
	}

	[Command("eval")]
	public async Task EvalCommand(CommandContext ctx, [RemainingText] string code)
	{
		DiscordMessage msg;

		int cs1 = code.IndexOf("```", StringComparison.Ordinal) + 3;
		cs1 = code.IndexOf('\n', cs1) + 1;
		int cs2 = code.LastIndexOf("```", StringComparison.Ordinal);

		if (cs1 is -1 || cs2 is -1)
		{
			cs1 = 0;
			cs2 = code.Length;
		}

		string cs = code.Substring(cs1, cs2 - cs1);

		msg = await ctx.RespondAsync("", new DiscordEmbedBuilder()
				.WithColor(new("#FF007F"))
				.WithDescription("Evaluating...")
				.Build())
			.ConfigureAwait(false);

		try
		{
			var globals = new TestVariables(ctx.Message, ctx.Client, ctx);

			var sopts = ScriptOptions.Default;
			// sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text",
			// 	"System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Entities", "Shitcord",
			// 	"DSharpPlus.CommandsNext", "DSharpPlus.Interactivity", "Microsoft.Extensions.Logging");
			sopts = sopts.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text",
				"System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Entities", "Shitcord",
				"DSharpPlus.CommandsNext", "Microsoft.Extensions.Logging");
			IEnumerable<Assembly> asm = AppDomain.CurrentDomain.GetAssemblies()
				.Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location));


			sopts = sopts.WithReferences(asm);

			Script<object> script = CSharpScript.Create(cs, sopts, typeof(TestVariables));
			script.Compile();

			ScriptState<object> result = await script.RunAsync(globals).ConfigureAwait(false);
			if (result?.ReturnValue is DiscordEmbedBuilder or DiscordEmbed)
				await msg.ModifyAsync(m =>
					m.WithEmbed(result.ReturnValue as DiscordEmbedBuilder ?? result.ReturnValue as DiscordEmbed));
			else if (result?.ReturnValue is not null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
				await msg.ModifyAsync(new DiscordEmbedBuilder
					{
						Title = "Evaluation Result",
						Description = result.ReturnValue.ToString(),
						Color = new DiscordColor("#007FFF")
					}.Build())
					.ConfigureAwait(false);
			else
				await msg.ModifyAsync(new DiscordEmbedBuilder
					{
						Title = "Evaluation Successful", Description = "No result was returned.",
						Color = new DiscordColor("#007FFF")
					}.Build())
					.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			await msg.ModifyAsync(new DiscordEmbedBuilder
				{
					Title = "Evaluation Failure",
					Description = $"**{ex.GetType()}**: {ex.Message.Split('\n')[0]}",
					Color = new DiscordColor("#FF0000")
				}.Build())
				.ConfigureAwait(false);
		}

	}

	public record TestVariables
	{
		public TestVariables(DiscordMessage msg, DiscordClient client, CommandContext ctx)
		{
			Client = client;
			Context = ctx;
			Message = msg;
			Channel = msg.Channel;
			Guild = Channel.Guild;
			User = Message.Author;
			Reply = Message.ReferencedMessage;

			if (Guild != null) 
				Member = Guild.GetMemberAsync(User.Id)
					.ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public DiscordMessage Message { get; }
		public DiscordMessage Reply { get; }
		public DiscordChannel Channel { get; }
		public DiscordGuild Guild { get; }
		public DiscordUser User { get; }
		public DiscordMember Member { get; }
		public CommandContext Context { get; }
		public DiscordClient Client { get; }
	}

}

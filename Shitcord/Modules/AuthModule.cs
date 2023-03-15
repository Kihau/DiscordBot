using System.Diagnostics;
using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Shitcord.Extensions;
using Shitcord.Services;
using Shitcord.Database;
using Shitcord.Database.Queries;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Data.Sqlite;

namespace Shitcord.Modules;

[RequireAuthorized]
public class AuthModule : BaseCommandModule
{
    public DiscordBot Bot { get; }
    public DatabaseService Db { get; }

    public AuthModule(DiscordBot bot, DatabaseService db) {
        this.Bot = bot;
        this.Db = db;
    }

    public override async Task BeforeExecutionAsync(CommandContext ctx) {
        //this.Data = this.Audio.GetOrAddData(ctx.Guild);
        await base.BeforeExecutionAsync(ctx);
    }

    [Command("shrinkdatabase"), Aliases("shrinkdb")]
    [Description("Shrinks sqlite database file")]
    public Task ShrinkDatabaseCommand(CommandContext ctx) {
        Db.ShrinkSqliteDBFile();
        return Task.CompletedTask;
    }

    [Command("execute"), Aliases("exec")]
    [Description("Executes a given command and displays its output (timeout is set to 10 sec)")]
    public async Task ExecuteCommand(CommandContext ctx,
        [Description("Specified command"), RemainingText] string command 
    ) => await ExecuteCommandAsync(ctx, command, 10);

    [Command("executetimed"), Aliases("exect")]
    [Description("Executes a given command and displays its output")]
    public async Task ExecuteTimedCommand(CommandContext ctx,
        [Description("Specified command")] string command, 
        [Description("Maximium execution time (in seconds)")] int timeout
    ) => await ExecuteCommandAsync(ctx, command, timeout);

    private async Task ExecuteCommandAsync(CommandContext ctx, string command, int timeout)
    {
        if (command.Length == 0)
            throw new CommandException("Command cannot be an empty string");

        // Constructing the command
        string file_name = "";
        string arguments = "";

        if (System.OperatingSystem.IsLinux()) {
            file_name = "bash";
            arguments = $"-c \"{command}\""; 
        } else {
            // Not sure it this works correctly, but also don't care
            var split = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 0)
                throw new CommandException("Incorrect input command");

            file_name = split[0];

            if (split.Length > 1)
                arguments = String.Join(' ', split.Skip(1));
        }

        var startInfo = new ProcessStartInfo {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = file_name,
            Arguments = arguments
        };

        var start_time = DateTime.Now;
        var process = Process.Start(startInfo);
        if (process is null) throw new CommandException("Incorrect input parameters");

        var embed = new DiscordEmbedBuilder() {
            Title = "Execution in progress",
            Description = "The command is currently running. . .",
            Color = DiscordColor.Purple
        };

        var message_builder = new DiscordMessageBuilder()
            .WithEmbed(embed.Build())
            .AddComponents(
                new DiscordButtonComponent(
                    ButtonStyle.Primary, "stdoutput", "Standard Output", true),
                new DiscordButtonComponent(
                    ButtonStyle.Danger, "stderror", "Standard Error", true)
            );

        var message = await ctx.Channel.SendMessageAsync(message_builder);

        // Executing the command
        var completed = process.WaitForExit(timeout * 1000);
        if (!completed) {
            embed.Title = "Execution failed";
            embed.Color = DiscordColor.Red;
            embed.Description = 
                "Process has not finished execution in specified time. Terminating.";

            process.Kill(true);
        } else {
            var time = DateTime.Now - start_time;

            embed.Title = "Execution completed";
            embed.Color = DiscordColor.Green;
            embed.Description = process.ExitCode == 0 
                ? $"Process has finished successfuly. Execution time: `{time}`\n" 
                : $"Process has finished with an error. Execution time: `{time}`\n"; 
        }

        // Preparing output to update the message
        message_builder = new DiscordMessageBuilder {
            Embed = embed.Build(),
        };
        message_builder.AddComponents(
            new DiscordButtonComponent(ButtonStyle.Primary, "stdoutput", "Standard Output"),
            new DiscordButtonComponent(ButtonStyle.Danger, "stderror", "Standard Error"),
            new DiscordButtonComponent(
                ButtonStyle.Secondary, "clearexec", null, false, new DiscordComponentEmoji("🗑️")
            )
        );

        await message.ModifyAsync(message_builder);

        // Handling the buttons, displaying standard output/error when a button was pressed
        var output = process.StandardOutput.ReadToEnd(); 
        output = String.IsNullOrEmpty(output) ? "<No standard output>" : output;

        var error = process.StandardOutput.ReadToEnd(); 
        error = String.IsNullOrEmpty(error) ? "<No standard error>" : error;

        while (true) {
            var interactivity = ctx.Client.GetInteractivity();
            var result = await message.WaitForButtonAsync(ctx.User, TimeSpan.FromSeconds(300));

            if (result.TimedOut || result.Result.Id == "clearexec") {
                await message.DeleteAsync();
                break;
            } 

            IEnumerable<Page>? pages = null;
            if (result.Result.Id == "stdoutput") 
                pages = interactivity.GeneratePagesInEmbed(output);
            else if (result.Result.Id == "stderror")
                pages = interactivity.GeneratePagesInEmbed(error);

            await result.Result.Interaction.CreateResponseAsync(
                InteractionResponseType.DeferredMessageUpdate
            );

            await ctx.Channel.SendPaginatedMessageAsync(
                ctx.Member, pages, PaginationBehaviour.WrapAround, 
                ButtonPaginationBehavior.DeleteMessage
            );
        }
    }

    [Command("shutdown"), Aliases("exit")]
    [Description("literally don't even try")]
    public async Task ShutdownCommand(CommandContext ctx, bool console = true)
    {
        if (console) Console.WriteLine("Shutting down");
        else await ctx.RespondAsync("Shutting down");
        Environment.Exit(0);
    }

    [Group("command")]
    [Description("Custom command management")]
    public class CustomCommandModule : BaseCommandModule {
        private CustomCommandService Custom { get; }

        public CustomCommandModule(CustomCommandService custom) => Custom = custom;

        [Command("add")]
        public async Task AddCommand(
            CommandContext ctx, string cmd_name, [RemainingText] string lua_script
        ) {
            lua_script = lua_script.Trim();

            int code_start = lua_script.IndexOf("```");
            if (code_start != -1)
                code_start = lua_script.IndexOf("lua", code_start + 3);

            code_start = lua_script.IndexOf('\n', code_start + 3);

            int code_end = lua_script.LastIndexOf("```");

            if (code_start == -1)
                code_start = 0;
            else code_start += 1;

            if (code_end == -1)
                code_end = lua_script.Length;
            else code_end -= 1;

            System.Console.WriteLine($"start: {code_start}, end: {code_end}");
            lua_script = lua_script.Substring(code_start, code_end - code_start);
            System.Console.WriteLine(lua_script);
            Custom.AddCommand(ctx.Guild, cmd_name, lua_script);

            await ctx.RespondAsync("Successfully added custom command 👍");
        }

        [Command("edit")]
        public async Task EditCommand(CommandContext ctx, string cmd_name, string lua_script) {
            lua_script = lua_script.Trim();

            int code_start = lua_script.IndexOf("```");
            if (code_start != -1)
                code_start = lua_script.IndexOf("lua", code_start + 3);

            code_start = lua_script.IndexOf('\n', code_start + 3);

            int code_end = lua_script.LastIndexOf("```");

            if (code_start == -1)
                code_start = 0;
            else code_start += 1;

            if (code_end == -1)
                code_end = lua_script.Length;
            else code_end -= 1;

            System.Console.WriteLine($"start: {code_start}, end: {code_end}");
            lua_script = lua_script.Substring(code_start, code_end - code_start);
            System.Console.WriteLine(lua_script);
            Custom.EditCommand(ctx.Guild, cmd_name, lua_script);

            await ctx.RespondAsync("Successfully edited the command 👍");   
        }

        [Command("remove")]
        public async Task RemoveCommand(CommandContext ctx, string cmd_name) {
            Custom.RemoveCommand(ctx.Guild, cmd_name);
            await ctx.RespondAsync("Successfully removed specified command 👍");
        }

        // Should be a part of the custom helper extension
        [Command("list")]
        public Task ListCommand(CommandContext ctx) {
            // TODO: List custom commands
            return Task.CompletedTask;
        }

        [Command("showcode"), Aliases("show")]
        public async Task ShowcodeCommand(CommandContext ctx, string cmd_name) {
            string lua_script = Custom.GetLuaScript(ctx.Guild, cmd_name);
            // lua_script = lua_script.Insert(0, "```lua\n") + "\n```";
            await ctx.Channel.SendMessageAsync(
                $"Lua code for command `{cmd_name}`: ```lua\n{lua_script}\n```"
            );
        }
    }

    [Command("authlist")]
    [Description("Lists ids of all users in authorized list")]
    public async Task AuthListCommand(CommandContext ctx)
    {
        var columns = Db.RetrieveColumns(QueryBuilder.New()
            .Retrieve(AuthUsersTable.USER_ID)
            .From(AuthUsersTable.TABLE_NAME)
            .Build()
        );

        if (columns is null) throw new CommandException("No authorized users in the list");

        var message = "```\n";
        for (int i = 0; i < columns[0].Count; i++)
            message += $"{i}. {columns[0][i]}\n";
        message += "```";

        await ctx.Channel.SendMessageAsync(message);
    }

    [Command("authadd")]
    [Description("Adds authorized user to the database")]
    public async Task AuthAddCommand(CommandContext ctx, DiscordUser user)
    {
        var user_exists = Db.ExistsInTable(AuthUsersTable.TABLE_NAME, Condition
            .New(AuthUsersTable.USER_ID).Equals(user.Id)
        );

        if (user_exists) throw new CommandException("User is already in the authorized list");

        Db.executeUpdate(QueryBuilder
            .New().Insert().Into(AuthUsersTable.TABLE_NAME).Values(user.Id).Build()
        );

        await ctx.RespondAsync($"User `{user.Id}` successfuly added to the auth list");
    }

    [Command("authrm")]
    [Description("Removes authorized user from the database")]
    public async Task AuthRemoveCommand(CommandContext ctx, DiscordUser user)
    {
        var user_exists = Db.ExistsInTable(AuthUsersTable.TABLE_NAME, Condition
            .New(AuthUsersTable.USER_ID).Equals(user.Id)
        );

        if (!user_exists) throw new CommandException("User is not in the authorized list");

        Db.executeUpdate(QueryBuilder
            .New().Delete()
            .From(AuthUsersTable.TABLE_NAME)
            .WhereEquals(AuthUsersTable.USER_ID, user.Id)
            .Build()
        );

        await ctx.RespondAsync("Successfuly removed user from the auth list");
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

    [Command("sql"), Description("Executes sql update query against db")]
    public async Task ExecuteSQLQuery(CommandContext ctx, string query, bool console = true)
    {
        int rowsAffected;
        try {
            rowsAffected = Db.executeUpdate(query);
        }
        catch (SqliteException sqlExc) {
            await ctx.RespondAsync($"\n{sqlExc.Message}\n");
            return;
        }
        await ctx.RespondAsync($"\nSuccessfully executed, affected {rowsAffected} rows\n");
    }

    [Command("eval")]
    public async Task EvalCommand(CommandContext ctx, [RemainingText] string code)
    {
        int cs1 = code.IndexOf("```", StringComparison.Ordinal) + 3;
        cs1 = code.IndexOf('\n', cs1) + 1;
        int cs2 = code.LastIndexOf("```", StringComparison.Ordinal);

        if (cs1 is -1 || cs2 is -1) {
            cs1 = 0;
            cs2 = code.Length;
        }

        string cs = code.Substring(cs1, cs2 - cs1);

        DiscordMessage msg = await ctx.RespondAsync("", 
            new DiscordEmbedBuilder()
                .WithColor(new("#FF007F"))
                .WithDescription("Evaluating...")
                .Build()
        );

        try {
            var globals = new TestVariables(ctx.Message, ctx.Client, ctx);

            var sopts = ScriptOptions.Default;
            sopts = sopts.WithImports(
                "System", "System.Collections.Generic", "System.Linq", "System.Text",
                "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Entities", 
                "DSharpPlus.CommandsNext", "Microsoft.Extensions.Logging", "Shitcord", 
                "Shitcord.Extensions", "Shitcord.Database", "Shitcord.Database.Queries",
                "Shitcord.Data"
            );

            IEnumerable<Assembly> asm = AppDomain.CurrentDomain.GetAssemblies()
                .Where(xa => !xa.IsDynamic);
            sopts = sopts.WithReferences(asm);

            Script<object> script = CSharpScript.Create(cs, sopts, typeof(TestVariables));
            script.Compile();

            ScriptState<object> result = await script.RunAsync(globals);

            if (result?.ReturnValue is DiscordEmbedBuilder or DiscordEmbed) {
                await msg.ModifyAsync(m => m.WithEmbed(
                    result.ReturnValue as DiscordEmbedBuilder ??
                    result.ReturnValue as DiscordEmbed
                ));
            } else if (result?.ReturnValue is not null &&
                !string.IsNullOrWhiteSpace(result.ReturnValue.ToString())) {
                await msg.ModifyAsync(new DiscordEmbedBuilder {
                    Title = "Evaluation Result",
                    Description = result.ReturnValue.ToString(),
                    Color = new DiscordColor("#007FFF")
                }.Build());
            } else {
                await msg.ModifyAsync(new DiscordEmbedBuilder {
                    Title = "Evaluation Successful", 
                    Description = "No result was returned.",
                    Color = new DiscordColor("#007FFF")
                }.Build());
            }    

        } catch (Exception ex) {
            await msg.ModifyAsync(new DiscordEmbedBuilder {
                Title = "Evaluation Failure",
                Description = $"**{ex.GetType()}**: {ex.Message.Split('\n')[0]}",
                Color = new DiscordColor("#FF0000")
            }.Build());
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
                Member = Guild.GetMemberAsync(User.Id).GetAwaiter().GetResult();
        }

        public DiscordMessage Message { get; }
        public DiscordMessage Reply { get; }
        public DiscordChannel Channel { get; }
        public DiscordGuild? Guild { get; }
        public DiscordUser User { get; }
        public DiscordMember? Member { get; }
        public CommandContext Context { get; }
        public DiscordClient Client { get; }
    }

}

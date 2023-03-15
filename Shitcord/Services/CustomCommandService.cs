using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using NLua;
using Shitcord.Database;
using Shitcord.Database.Queries;
using Shitcord.Extensions;

namespace Shitcord.Services;

public class CustomCommand {
    public string CommandName;
    public string LuaScript;

    public CustomCommand(string cmd_name, string lua_script) {
        CommandName = cmd_name;
        LuaScript = lua_script;
    }
}

public class CustomCommandService
{
    public DatabaseService Database { get; }
    public DiscordClient Client { get; init; }
    public DiscordConfig Config { get; }
    private Dictionary<ulong, Dictionary<string, string>> CommandSet { get; }

    public CustomCommandService(DiscordBot bot, DatabaseService database) {
        Database = database;
        Client = bot.Client;
        Config = bot.Config.Discord;

        CommandSet = new();
        LoadCommandsFromDatabase();

        // TODO: Check for collisions with hardcoded bot commands.
        //       If collision occurred - log error and exit.

        // Client.MessageCreated += MessageCreatedHandler;
    }

    private void LoadCommandsFromDatabase() {
        var data = Database.RetrieveColumns(QueryBuilder
            .New().Retrieve(
                CustomCommandTable.GUILD_ID,
                CustomCommandTable.COMMAND_NAME,
                CustomCommandTable.LUA_SCRIPT
                ).From(CustomCommandTable.TABLE_NAME)
            .Build()
        );

        if (data is null) 
            return;

        for (int i = 0; i < data[0].Count; i++) {
            var guild_id = (ulong)(long)data[0][i]!;
            var cmd_name = (string)data[1][i]!;
            var lua_script = (string)data[2][i]!;

            var command =  new Dictionary<string, string>();
            if (!CommandSet.ContainsKey(guild_id)) {
                CommandSet.Add(guild_id, command);
            } else command = CommandSet[guild_id];

            if (!command.ContainsKey(cmd_name)) {
                command.Add(cmd_name, lua_script);
            } 
        }
    }

    public void EditCommand(DiscordGuild guild, string cmd_name, string lua_script) {
        if (!CommandSet.ContainsKey(guild.Id))
            return;
        var commands = CommandSet[guild.Id];

        // TODO: Also check for colisions with builtin commands.
        cmd_name = cmd_name.ToLower();
        if (!commands.ContainsKey(cmd_name))
            throw new CommandException($"Command called {cmd_name} doesn't exists.");
        commands[cmd_name] = lua_script;

        Database.executeUpdate(QueryBuilder
            .New().Update(CustomCommandTable.TABLE_NAME)
            .Set(CustomCommandTable.LUA_SCRIPT, lua_script)
            .Where(Condition
                .New(CustomCommandTable.GUILD_ID).Equals(guild.Id)
                .And(CustomCommandTable.COMMAND_NAME).Equals(cmd_name)
            ).Build()
        );
    }

    public bool CommandExist(DiscordGuild guild, string cmd_name) {
        if (!CommandSet.ContainsKey(guild.Id))
            return false;
        var commands = CommandSet[guild.Id];

        cmd_name = cmd_name.ToLower();
        return commands.ContainsKey(cmd_name);
    }

    public void AddCommand(DiscordGuild guild, string cmd_name, string lua_script) {
        if (!CommandSet.ContainsKey(guild.Id))
            CommandSet.Add(guild.Id, new());
        var commands = CommandSet[guild.Id];

        // TODO: Also check for colisions with builtin commands.
        cmd_name = cmd_name.ToLower();
        if (commands.ContainsKey(cmd_name))
            throw new CommandException($"Command called {cmd_name} already exists.");
        commands.Add(cmd_name, lua_script);

        Database.executeUpdate(QueryBuilder
            .New().Insert().Into(CustomCommandTable.TABLE_NAME)
            .Values(guild.Id, cmd_name, lua_script)
            .Build()
        );
    }

    public string GetLuaScript(DiscordGuild guild, string cmd_name) {
        if (!CommandSet.ContainsKey(guild.Id))
            throw new CommandException("There are no custom commands added to this guild");
        var commands = CommandSet[guild.Id];

        cmd_name = cmd_name.ToLower();
        if (!commands.ContainsKey(cmd_name))
            throw new CommandException($"Command `{cmd_name}` does not exist");
        return commands[cmd_name];
    }

    public void RemoveCommand(DiscordGuild guild, string cmd_name) {
        if (!CommandSet.ContainsKey(guild.Id))
            throw new CommandException("Command list is already empty");
        var commands = CommandSet[guild.Id];

        // TODO: Also check if a builtin command was provieded and throw
        //       "You cannot remove a builtin bot command"
        cmd_name = cmd_name.ToLower();
        if (!commands.ContainsKey(cmd_name))
            throw new CommandException($"Command `{cmd_name}` does not exist");
        commands.Remove(cmd_name);

        Database.executeUpdate(QueryBuilder
            .New().Delete().From(CustomCommandTable.TABLE_NAME)
            .Where(Condition
                .New(CustomCommandTable.GUILD_ID).Equals(guild.Id)
                .And(CustomCommandTable.COMMAND_NAME).Equals(cmd_name)
            ).Build()
        );
    }

    public CustomCommand? FindCommand(DiscordGuild guild, string? cmd_name) {
        if (cmd_name == null)
            return null;

        if (!CommandSet.ContainsKey(guild.Id))
            return null;
        var commands = CommandSet[guild.Id];

        // cmd_name to lower ?????
        if (!commands.ContainsKey(cmd_name))
            return null;

        var lua_script = commands[cmd_name];
        var command = new CustomCommand(cmd_name, lua_script);

        return command;
    }

    public void ExecuteCommand(CommandContext? context, CustomCommand cmd, string[]? args) {
        if (context == null)
            return;

        Lua lua = new();
        lua.LoadCLRPackage();

        // catch and do some shit on fail
        var cnext = Client.GetCommandsNext();
        lua["ctx"] = context;

        // TODO: Insert frequent imports into the lua code
        // Executing the code
        lua.DoString(cmd.LuaScript);

        var lua_func = lua[cmd.CommandName] as LuaFunction;
        if (lua_func == null) {
            return;
            // Do some shit of fail
        }
        
        // var args = context.RawArguments.ToArray();
        lua_func.Call(args);

        // var args = context.RawArguments.ToArray();
        // System.Console.WriteLine("ARGS ARE: " + args);
        // var debug = lua_func.Call(args);
        // System.Console.WriteLine("DEBUG: " + debug);
    }

    private Task MessageCreatedHandler(DiscordClient sender, MessageCreateEventArgs e) {
        if (e.Message.Author.IsBot) 
            return Task.CompletedTask;

        var msg = e.Message.Content.Trim();
        if (!msg.StartsWith(Config.Prefix)) 
            return Task.CompletedTask;

        msg = msg.Substring(Config.Prefix.Length);
        var msg_args = msg.Split();

        if (!CommandSet.ContainsKey(e.Guild.Id))
            throw new CommandException("Get fixed boi");
        var commands = CommandSet[e.Guild.Id];

        var cmd_name = msg_args[0].ToLower();
        if (!commands.ContainsKey(cmd_name))
            throw new CommandException("Get fixed boi");

        var lua_script = commands[cmd_name];

        Lua lua = new();
        lua.LoadCLRPackage();
        var cnext = Client.GetCommandsNext();
        // var command = cnext.FindCommand("luacommand", out var ignored);
        // if (command == null)
        //     throw new CommandException("Get fixed boi");
        //
        // var context = cnext.CreateFakeContext(
        //     actor: e.Author,
        //     channel: e.Channel,
        //     messageContents: msg,
        //     prefix: Config.Prefix,
        //     cmd: command
        // );
        var context = cnext.CreateContext(e.Message, Config.Prefix, null);
        lua["ctx"] = context;

        // TODO: Insert frequent imports into the lua code
        // Executing the code
        lua.DoString(lua_script);

        var lua_func = lua[cmd_name] as LuaFunction;
        if (lua_func == null) {
            throw new CommandException("Get fixed boi");
        }

        // Does this thing throw? yes, catch it
        var args = msg_args.Skip(1).ToArray();
        System.Console.WriteLine("ARGS ARE: " + args);
        var debug = lua_func.Call(args);
        System.Console.WriteLine("DEBUG: " + debug);

        return Task.CompletedTask;
    }
}

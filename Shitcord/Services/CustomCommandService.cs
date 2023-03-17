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
    // -1 should be global
    private Dictionary<string, string> CommandSet { get; }

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
                CustomCommandTable.COMMAND_NAME,
                CustomCommandTable.LUA_SCRIPT
                ).From(CustomCommandTable.TABLE_NAME)
            .Build()
        );

        if (data is null) 
            return;

        for (int i = 0; i < data[0].Count; i++) {
            var cmd_name = (string)data[0][i]!;
            var lua_script = (string)data[1][i]!;

            if (!CommandSet.ContainsKey(cmd_name)) {
                CommandSet.Add(cmd_name, lua_script);
            } 
        }
    }

    public void EditCommand(string cmd_name, string lua_script) {
        cmd_name = cmd_name.ToLower();
        if (!CommandSet.ContainsKey(cmd_name))
            throw new CommandException($"Command called {cmd_name} doesn't exists.");
        CommandSet[cmd_name] = lua_script;

        var cnext = Client.GetCommandsNext();
        var found = cnext.FindCommand(cmd_name, out var ignored);
        if (found != null) {
            throw new CommandException(
                $"You cannot edit commmand: `{cmd_name}`. Builtin commands are not modifiable."
            );
        }

        Database.executeUpdate(QueryBuilder
            .New().Update(CustomCommandTable.TABLE_NAME)
            .Set(CustomCommandTable.LUA_SCRIPT, lua_script)
            .WhereEquals(CustomCommandTable.COMMAND_NAME, cmd_name)
            .Build()
        );
    }

    public bool CommandExist(string cmd_name) {
        cmd_name = cmd_name.ToLower();
        return CommandSet.ContainsKey(cmd_name);
    }

    public void AddCommand(string cmd_name, string lua_script) {
        cmd_name = cmd_name.ToLower();

        var cnext = Client.GetCommandsNext();
        var found = cnext.FindCommand(cmd_name, out var ignored);
        if (found != null) {
            throw new CommandException(
                $"You cannot add commmand named: `{cmd_name}`. " +
                "Builtin command with such name already exists."
            );
        }

        if (CommandSet.ContainsKey(cmd_name))
            throw new CommandException($"Command called {cmd_name} already exists.");
        CommandSet.Add(cmd_name, lua_script);

        Database.executeUpdate(QueryBuilder
            .New().Insert().Into(CustomCommandTable.TABLE_NAME)
            .Values(cmd_name, lua_script)
            .Build()
        );
    }

    public string GetLuaScript(DiscordGuild guild, string cmd_name) {
        cmd_name = cmd_name.ToLower();
        if (!CommandSet.ContainsKey(cmd_name))
            throw new CommandException($"Command `{cmd_name}` does not exist");
        return CommandSet[cmd_name];
    }

    public void RemoveCommand(DiscordGuild guild, string cmd_name) {
        cmd_name = cmd_name.ToLower();

        var cnext = Client.GetCommandsNext();
        var found = cnext.FindCommand(cmd_name, out var ignored);
        if (found != null) {
            throw new CommandException(
                $"`{cmd_name}` is a builtin command. Only commands added at runtime can be removed"
            );
        }

        if (!CommandSet.ContainsKey(cmd_name))
            throw new CommandException($"Command `{cmd_name}` does not exist");
        CommandSet.Remove(cmd_name);

        Database.executeUpdate(QueryBuilder
            .New().Delete().From(CustomCommandTable.TABLE_NAME)
            .WhereEquals(CustomCommandTable.COMMAND_NAME, cmd_name)
            .Build()
        );
    }

    public CustomCommand? FindCommand(DiscordGuild guild, string? cmd_name) {
        if (cmd_name == null)
            return null;

        cmd_name = cmd_name.ToLower();
        if (!CommandSet.ContainsKey(cmd_name))
            return null;

        var lua_script = CommandSet[cmd_name];
        var command = new CustomCommand(cmd_name, lua_script);

        return command;
    }

    public async Task ExecuteCommandAsync(CommandContext? context, CustomCommand cmd, string[]? args) {
        if (context == null)
            return;

        Lua lua = new();
        var cnext = Client.GetCommandsNext();

        // lua.HookException += async (sender, e) => {
        //     System.Console.WriteLine("SOMETHING: " + e.Exception.Message);
        //     await cnext.Error.InvokeAsync(
        //         cnext, new CommandErrorEventArgs {
        //             Context = context,
        //             Exception = e.Exception,
        //         }
        //     );
        // };

        try {
            // catch and do some shit on fail
            lua.LoadCLRPackage();
            lua["ctx"] = context;

            // TODO: Insert frequent imports into the lua code
            // Executing the code
            lua.DoString(cmd.LuaScript);

            var lua_func = lua["command"] as LuaFunction;
            // var lua_func = lua[cmd.CommandName] as LuaFunction;
            if (lua_func == null) {
                return;
            }

            // var args = context.RawArguments.ToArray();
            lua_func.Call(args);

            await cnext.Executed.InvokeAsync(
                cnext, new CommandExecutionEventArgs { 
                    Context = context 
                }
            );
        } catch (Exception e) {
            await cnext.Error.InvokeAsync(
                cnext, new CommandErrorEventArgs {
                    Context = context,
                    Exception = e,
                }
            );
        }

        // var args = context.RawArguments.ToArray();
        // System.Console.WriteLine("ARGS ARE: " + args);
        // var debug = lua_func.Call(args);
        // System.Console.WriteLine("DEBUG: " + debug);
    }

    // private Task MessageCreatedHandler(DiscordClient sender, MessageCreateEventArgs e) {
    //     if (e.Message.Author.IsBot) 
    //         return Task.CompletedTask;
    //
    //     var msg = e.Message.Content.Trim();
    //     if (!msg.StartsWith(Config.Prefix)) 
    //         return Task.CompletedTask;
    //
    //     msg = msg.Substring(Config.Prefix.Length);
    //     var msg_args = msg.Split();
    //
    //     if (!CommandSet.ContainsKey(e.Guild.Id))
    //         throw new CommandException("Get fixed boi");
    //     var commands = CommandSet[e.Guild.Id];
    //
    //     var cmd_name = msg_args[0].ToLower();
    //     if (!commands.ContainsKey(cmd_name))
    //         throw new CommandException("Get fixed boi");
    //
    //     var lua_script = commands[cmd_name];
    //
    //     Lua lua = new();
    //     lua.LoadCLRPackage();
    //     var cnext = Client.GetCommandsNext();
    //     // var command = cnext.FindCommand("luacommand", out var ignored);
    //     // if (command == null)
    //     //     throw new CommandException("Get fixed boi");
    //     //
    //     // var context = cnext.CreateFakeContext(
    //     //     actor: e.Author,
    //     //     channel: e.Channel,
    //     //     messageContents: msg,
    //     //     prefix: Config.Prefix,
    //     //     cmd: command
    //     // );
    //     var context = cnext.CreateContext(e.Message, Config.Prefix, null);
    //     lua["ctx"] = context;
    //
    //     // Executing the code
    //     lua.DoString(lua_script);
    //
    //     var lua_func = lua[cmd_name] as LuaFunction;
    //     if (lua_func == null) {
    //         throw new CommandException("Get fixed boi");
    //     }
    //
    //     // Does this thing throw? yes, catch it
    //     var args = msg_args.Skip(1).ToArray();
    //     System.Console.WriteLine("ARGS ARE: " + args);
    //     var debug = lua_func.Call(args);
    //     System.Console.WriteLine("DEBUG: " + debug);
    //
    //     return Task.CompletedTask;
    // }
}

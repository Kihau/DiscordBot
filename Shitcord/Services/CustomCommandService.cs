using NLua;

using DSharpPlus;
using DSharpPlus.CommandsNext;

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
        DetectCollisions();
    }

    private void DetectCollisions() {
        var cnext = Client.GetCommandsNext();
        foreach (var key in CommandSet.Keys) {
            var found = cnext.FindCommand(key, out var ignored);
            if (found != null)
                throw new Exception($"Builtin and runtime commands collision for command `{key}`");
        }
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

    public string GetLuaScript(string cmd_name) {
        cmd_name = cmd_name.ToLower();
        if (!CommandSet.ContainsKey(cmd_name))
            throw new CommandException($"Command `{cmd_name}` does not exist");
        return CommandSet[cmd_name];
    }

    public void RenameCommand(string old_name, string new_name) {
        old_name = old_name.ToLower();

        if (!CommandSet.ContainsKey(old_name))
            throw new CommandException($"Command `{old_name}` does not exist");
        CommandSet.Remove(old_name);
        var lua_script = CommandSet[old_name];
        CommandSet.Add(new_name, lua_script);

        Database.executeUpdate(QueryBuilder
            .New().Update(CustomCommandTable.TABLE_NAME)
            .WhereEquals(CustomCommandTable.COMMAND_NAME, old_name)
            .Set(CustomCommandTable.COMMAND_NAME, new_name)
            .Build()
        );
    }

    public void RemoveCommand(string cmd_name) {
        cmd_name = cmd_name.ToLower();

        // var cnext = Client.GetCommandsNext();
        // var found = cnext.FindCommand(cmd_name, out var ignored);
        // if (found != null) {
        //     throw new CommandException(
        //         $"`{cmd_name}` is a builtin command. Only commands added at runtime can be removed"
        //     );
        // }

        if (!CommandSet.ContainsKey(cmd_name))
            throw new CommandException($"Command `{cmd_name}` does not exist");
        CommandSet.Remove(cmd_name);

        Database.executeUpdate(QueryBuilder
            .New().Delete().From(CustomCommandTable.TABLE_NAME)
            .WhereEquals(CustomCommandTable.COMMAND_NAME, cmd_name)
            .Build()
        );
    }

    public CustomCommand? FindCommand(string? cmd_name) {
        if (cmd_name == null)
            return null;

        cmd_name = cmd_name.ToLower();
        if (!CommandSet.ContainsKey(cmd_name))
            return null;

        var lua_script = CommandSet[cmd_name];
        var command = new CustomCommand(cmd_name, lua_script);

        return command;
    }

    public async Task ExecuteCommandAsync(CommandContext? context, CustomCommand cmd) {
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
                await cnext.Executed.InvokeAsync(
                    cnext, new CommandExecutionEventArgs { 
                        Context = context 
                    }
                );
                return;
            }

            // var args = context.RawArguments.ToArray();
            lua_func.Call(context.RawArguments.ToArray());

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
    }
}

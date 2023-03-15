namespace Shitcord.Database;

public class CustomCommandTable
{
    public const string TABLE_NAME = "custom_command_data";

    // public static readonly Column GUILD_ID      = new("guild_id", "bigint", false);
    public static readonly Column COMMAND_NAME  = new("cmd_name", "varchar(255)", false);
    public static readonly Column LUA_SCRIPT    = new("lua_script", "varchar(2048)", false);
    public static readonly List<Column> COLUMNS = new() { COMMAND_NAME, LUA_SCRIPT };
}

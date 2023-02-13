
namespace Shitcord.Database;

public class AutoReplyTable
{
    public const string TABLE_NAME = "auto_reply_data";
    
    public static readonly Column GUILD_ID      = new("guild_id", "bigint", false);
    public static readonly Column MATCH         = new("match", "varchar(255)", false);
    public static readonly Column REPLY         = new("reply", "varchar(255)", false);
    public static readonly Column MODE          = new("mode", "int", false);
    public static readonly Column CASE          = new("match_case", "boolean", false);
    public static readonly List<Column> COLUMNS = new() { GUILD_ID, MATCH, REPLY, MODE, CASE };
}

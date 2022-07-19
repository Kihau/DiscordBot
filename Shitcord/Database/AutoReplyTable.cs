
namespace Shitcord.Database;

public class AutoReplyTable
{
    public const string TABLE_NAME = "auto_reply_data";
    
    public static readonly Column GUILD_ID      = new("guild_id", "binint", false);
    public static readonly Column MATCH         = new("match", "varchar(255)", false);
    public static readonly Column RESPONSE      = new("response", "varchar(255)", false);
    public static readonly List<Column> COLUMNS = new() { GUILD_ID, MATCH, RESPONSE };
}

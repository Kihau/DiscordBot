namespace Shitcord.Database;

public class AuthMarkovTable 
{
    public const string TABLE_NAME = "auth_markov";
    
    public static readonly Column CHANNEL_ID = new("channel_id", "bigint", false, true);
    public static readonly List<Column> COLUMNS = new() { CHANNEL_ID };
}

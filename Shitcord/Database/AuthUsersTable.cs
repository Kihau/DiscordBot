namespace Shitcord.Database;

public class AuthUsersTable 
{
    public const string TABLE_NAME = "auth_users";
    
    public static readonly Column USER_ID = new("user_id", "bigint", false, true);
    public static readonly List<Column> COLUMNS = new() { USER_ID };
}

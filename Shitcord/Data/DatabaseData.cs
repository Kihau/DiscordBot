using System.Text;
using Microsoft.Data.Sqlite;

namespace Shitcord.Data;

public class DatabaseData
{
    private const string DATABASE_NAME = "Resources/BotDatabase.sqlite";
    private readonly SqliteConnection connection;
    public DatabaseData()
    {
        //create database if it doesn't exist
        if (!File.Exists(DATABASE_NAME))
        {
            File.Create(DATABASE_NAME);
        }
        connection = new SqliteConnection("Data Source=" + DATABASE_NAME);

        CreateTableIfNotExists();

        //TODO:for every connected guild IsGuildInTable if not contained InsertRow
    }

    private void CreateTableIfNotExists()
    {
        string createStatement = @"CREATE TABLE IF NOT EXISTS Songs(
                               guild_id bigint PRIMARY KEY,
                               qu       bigint,
                               su       bigint
                               );";
        var createCommand = new SqliteCommand(createStatement, connection);
        connection.Open();
        createCommand.ExecuteNonQuery();
        connection.Close();
    }

    public bool InsertRow(long guild_id, long qu, long su)
    {
        if (IsGuildInTable(guild_id))
        {
            return false;
        }
        string statement = $@"INSERT INTO Songs VALUES
                        ({guild_id}, {qu}, {su});";
        var insertCommand = new SqliteCommand(statement, connection);
        connection.Open();
        int rowsInserted = insertCommand.ExecuteNonQuery();
        connection.Close();
        return rowsInserted == 1;
    }
    
    
    public override String ToString()
    {
        StringBuilder builder = new StringBuilder(64);
        string statement = "SELECT * FROM Songs";
        var command = new SqliteCommand(statement, connection);
        connection.Open();
        var reader = command.ExecuteReader();
        builder.Append("guild_id").Append(' ');
        builder.Append("qu").Append(' ');
        builder.Append("su").Append('\n');

        while (reader.Read())
        {
            string str1 = reader.GetString(0);
            string str2 = reader.GetString(1);
            string str3 = reader.GetString(2);
            builder.Append(str1).Append(' ');
            builder.Append(str2).Append(' ');
            builder.Append(str3).Append('\n');
        }

        connection.Close();
        return builder.ToString();
    }
    public bool IsGuildInTable(long guild_id)
    {
        string existsStatement = "SELECT guild_id FROM Songs WHERE guild_id = " + guild_id;
        var cmd = new SqliteCommand(existsStatement, connection);
        connection.Open();
        SqliteDataReader reader = cmd.ExecuteReader();
        bool rows = reader.HasRows;
        connection.Close();
        return rows;
    }

    public bool UpdateQU(long guild_id, long qu)
        => UpdateValue(guild_id, "qu", qu);

    public bool UpdateSU(long guild_id, long su)
        => UpdateValue(guild_id, "su", su);

    private bool UpdateValue(long guild_id, string valueName, long value)
    {
        string statement = $"UPDATE Songs SET {valueName} = {value} WHERE guild_id = {guild_id}";
        var updateCommand = new SqliteCommand(statement, connection);
        connection.Open();
        int rowsUpdated = updateCommand.ExecuteNonQuery();
        connection.Close();
        return rowsUpdated == 1;
    }

    public void DeleteAllRows()
    {
        string delStatement = "DELETE FROM Songs";
        var delCommand = new SqliteCommand(delStatement, connection);
        connection.Open();
        delCommand.ExecuteNonQuery();
        connection.Close();
    }
    
    public bool DeleteRow(long guild_id)
    {
        string delStatement = "DELETE FROM Songs WHERE guild_id = " + guild_id;
        var delCommand = new SqliteCommand(delStatement, connection);
        connection.Open();
        int rowsAffected = delCommand.ExecuteNonQuery();
        connection.Close();
        return rowsAffected > 0;
    }

    public long ReadSU(long guild_id)
        => ReadValue(guild_id, "su");

    public long ReadQU(long guild_id)
        => ReadValue(guild_id, "qu");

    //-1 is returned if value wasn't found
    private long ReadValue(long guild_id, string valueName)
    {
        string selectStatement = "SELECT " + valueName + " FROM Songs WHERE guild_id = " + guild_id;
        var readCommand = new SqliteCommand(selectStatement, connection);
        connection.Open();
        SqliteDataReader reader = readCommand.ExecuteReader();
        
        if (!reader.HasRows)
            return -1;
        
        if (!reader.Read())
            return -1;
        
        long val2 = reader.GetInt64(0);
        connection.Close();
        return val2;
    }
}

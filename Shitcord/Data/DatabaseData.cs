using System.Text;
using Microsoft.Data.Sqlite;

namespace Shitcord.Data;

public class DatabaseData
{
    private const string DATABASE_NAME = "Resources/BotDatabase.sqlite";
    private readonly SqliteConnection connection;
    private const int COLUMNS = 5;
    private const int ID_LENGTH = 18;
    private readonly short[] columnLengths = {8,13,13,9,9};

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
                               qu_channel_id   bigint,
                               su_channel_id   bigint,
                               qu_msg_id       bigint,
                               su_msg_id       bigint
                               );";
        var createCommand = new SqliteCommand(createStatement, connection);
        connection.Open();
        createCommand.ExecuteNonQuery();
        connection.Close();
    }

    public bool InsertRow(long guild_id, long qu_channel, long su_channel, long qu_msg, long su_msg)
    {
        if (IsGuildInTable(guild_id))
        {
            return false;
        }
        string statement = $@"INSERT INTO Songs VALUES
                        ({guild_id}, {qu_channel}, {su_channel}, {qu_msg}, {su_msg});";
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
        builder.Append(Spaces(ID_LENGTH - columnLengths[0])).Append("guild_id").Append(' ');
        builder.Append(Spaces(ID_LENGTH - columnLengths[1])).Append("qu_channel_id").Append(' ');
        builder.Append(Spaces(ID_LENGTH - columnLengths[2])).Append("su_channel_id").Append(' ');
        builder.Append(Spaces(ID_LENGTH - columnLengths[3])).Append("qu_msg_id").Append(' ');
        builder.Append(Spaces(ID_LENGTH - columnLengths[4])).Append("su_msg_id").Append('\n');
        
        while (reader.Read())
        {
            for (int i = 0; i < COLUMNS; i++)
            {
                string column = reader.GetString(i);
                builder.Append(column).Append(' ');
            }
            builder.Append('\n');
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

    public bool UpdateQUChannel(long guild_id, long qu_channel)
        => UpdateValue(guild_id, "qu_channel_id", qu_channel);
    public bool UpdateSUChannel(long guild_id, long su_channel)
        => UpdateValue(guild_id, "su_channel_id", su_channel);
    public bool UpdateQUMessage(long guild_id, long qu_msg)
        => UpdateValue(guild_id, "qu_msg_id", qu_msg);
    public bool UpdateSUMessage(long guild_id, long su_msg)
        => UpdateValue(guild_id, "su_msg_id", su_msg);

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

    public long ReadQUChannel(long guild_id)
        => ReadValue(guild_id, "qu_channel_id");
    public long ReadSUChannel(long guild_id)
        => ReadValue(guild_id, "su_channel_id");
    public long ReadQUMessage(long guild_id)
        => ReadValue(guild_id, "qu_msg_id");
    public long ReadSUMessage(long guild_id)
        => ReadValue(guild_id, "su_msg_id");

    

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

    private StringBuilder Spaces(int len)
    {
        StringBuilder spaces = new StringBuilder(len, len);
        for (int i = 0; i < len; i++)
        {
            spaces.Append(' ');
        }
        return spaces;
    }
}
using System.Text;
using Microsoft.Data.Sqlite;
using Shitcord.Extensions;

namespace Shitcord.Services;

public class DatabaseService
{
    private const string DATABASE_NAME = "Resources/BotDatabase.sqlite";
    private readonly SqliteConnection connection;
    private const int COLUMNS = 5;
    private const int ID_LENGTH = 18;
    private readonly short[] columnLengths = {8,13,13,9,9};

    public DatabaseService()
    {
        //create database if it doesn't exist
        if (!File.Exists(DATABASE_NAME))
            File.Create(DATABASE_NAME);

        connection = new SqliteConnection("Data Source=" + DATABASE_NAME);

        connection.Open();
        CreateTableIfNotExists();
    }

    ~DatabaseService() => connection.Close();

    private void CreateTableIfNotExists()
    {
        const string createStatement = @"CREATE TABLE IF NOT EXISTS Songs(
                guild_id       bigint  not null  PRIMARY KEY,
                qu_channel_id  bigint,
                su_channel_id  bigint,
                qu_msg_id      bigint,
                su_msg_id      bigint
            );";

        var createCommand = new SqliteCommand(createStatement, connection);
        createCommand.ExecuteNonQuery();
    }

    public bool InsertRow(
            ulong guild_id, ulong? qu_channel, ulong? su_channel, ulong? qu_msg, ulong? su_msg
    ) {
        if (IsGuildInTable(guild_id))
            return false;

        long guild_map = (long)guild_id;
        string qu_channel_map = qu_channel?.ToString() ?? "null";
        string su_channel_map = su_channel?.ToString() ?? "null";
        string qu_msg_map = qu_msg?.ToString() ?? "null";
        string su_msg_map = su_msg?.ToString() ?? "null";
        string statement = 
            $@"INSERT INTO Songs VALUES (
                {guild_map}, {qu_channel_map}, {su_channel_map}, {qu_msg_map}, {su_msg_map}
            );";

        var insertCommand = new SqliteCommand(statement, connection);
        int rowsInserted = insertCommand.ExecuteNonQuery();
        return rowsInserted == 1;
    }
    
    public override String ToString()
    {
        StringBuilder builder = new StringBuilder(64);
        const string statement = "SELECT * FROM Songs";
        var command = new SqliteCommand(statement, connection);
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
                string column = reader.IsDBNull(i) ? "null" : reader.GetString(i);
                int spaces = ID_LENGTH - column.Length;
                if (spaces > 0)
                {
                    builder.Append(Spaces(spaces));
                }
                builder.Append(column).Append(' ');
            }
            builder.Append('\n');
        }

        return builder.ToString();
    }

    public bool IsGuildInTable(ulong guild_id)
    {
        long mapped = (long)guild_id;
        string existsStatement = "SELECT guild_id FROM Songs WHERE guild_id = " + mapped;
        var cmd = new SqliteCommand(existsStatement, connection);
        SqliteDataReader reader = cmd.ExecuteReader();
        bool rows = reader.HasRows;
        return rows;
    }

    public bool UpdateQUChannel(ulong guild_id, ulong? qu_channel)
        => UpdateValue(guild_id, "qu_channel_id", qu_channel);
    public bool UpdateSUChannel(ulong guild_id, ulong? su_channel)
        => UpdateValue(guild_id, "su_channel_id", su_channel);
    public bool UpdateQUMessage(ulong guild_id, ulong? qu_msg)
        => UpdateValue(guild_id, "qu_msg_id", qu_msg);
    public bool UpdateSUMessage(ulong guild_id, ulong? su_msg)
        => UpdateValue(guild_id, "su_msg_id", su_msg);

    private bool UpdateValue(ulong guild_id, string valueName, ulong? value)
    {
        long guild_map = (long)guild_id;
        string val_map = value?.ToString() ?? "null";
        string statement = $"UPDATE Songs SET {valueName} = {val_map} WHERE guild_id = {guild_map}";
        var updateCommand = new SqliteCommand(statement, connection);
        int rowsUpdated = updateCommand.ExecuteNonQuery();
        return rowsUpdated == 1;
    }

    public void DeleteAllRows()
    {
        const string delStatement = "DELETE FROM Songs";
        var delCommand = new SqliteCommand(delStatement, connection);
        delCommand.ExecuteNonQuery();
    }

    public bool DeleteRow(ulong guild_id)
    {
        long mapped = (long)guild_id;
        string delStatement = "DELETE FROM Songs WHERE guild_id = " + mapped;
        var delCommand = new SqliteCommand(delStatement, connection);
        int rowsAffected = delCommand.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    public ulong? ReadQUChannel(ulong guild_id)
        => ReadValue(guild_id, "qu_channel_id");
    public ulong? ReadSUChannel(ulong guild_id)
        => ReadValue(guild_id, "su_channel_id");
    public ulong? ReadQUMessage(ulong guild_id)
        => ReadValue(guild_id, "qu_msg_id");
    public ulong? ReadSUMessage(ulong guild_id)
        => ReadValue(guild_id, "su_msg_id");

    //0 is returned if value wasn't found
    public ulong? ReadValue(ulong guild_id, string valueName)
    {
        long mapped = (long)guild_id;
        string selectStatement = "SELECT " + valueName + " FROM Songs WHERE guild_id = " + mapped;
        var readCommand = new SqliteCommand(selectStatement, connection);
        SqliteDataReader reader = readCommand.ExecuteReader();
        
        // TODO: Throw instead
        if (!reader.HasRows)
            return null;
        
        if (!reader.Read())
            return null;
        
        long val2 = reader.GetInt64(0);
        return (ulong)val2;
    }

    private StringBuilder Spaces(int len)
    {
        StringBuilder spaces = new StringBuilder(len, len);
        for (int i = 0; i < len; i++)
            spaces.Append(' ');

        return spaces;
    }
}

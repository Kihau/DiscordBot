using Microsoft.Data.Sqlite;

namespace Shitcord.Data;

public class DatabaseData{
    private const string DATABASE_NAME = "Resources/BotDatabase.sqlite";
    private SqliteConnection connection;
    public DatabaseData(){
      //create database if it doesn't exist
      if (!File.Exists(DATABASE_NAME)){
        File.Create(DATABASE_NAME);
      }
      connection = new SqliteConnection("Data Source=" + DATABASE_NAME);
      
      CreateTableIfNotExists();
      
      //TODO:for every connected guild IsGuildInTable if not contained InsertRow
    }

    private void CreateTableIfNotExists(){
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

    public bool InsertRow(long guild_id, long qu, long su){
      string statement = $@"INSERT INTO Songs VALUES
                        ({guild_id}, {qu}, {su});";
      var insertCommand = new SqliteCommand(statement, connection);
      connection.Open();
      int rowsInserted = insertCommand.ExecuteNonQuery();
      connection.Close();
      return rowsInserted == 1;
    }

    public bool IsGuildInTable(long guild_id){
      string existsStatement = "SELECT guild_id FROM songs WHERE guild_id = " + guild_id;
      var cmd = new SqliteCommand(existsStatement, connection);
      connection.Open();
      SqliteDataReader reader = cmd.ExecuteReader();
      connection.Close();
      return reader.HasRows;
    }
    
    public bool updateQU(long guild_id, long qu){
      return updateValue(guild_id, "qu", qu);
    }
    public bool updateSU(long guild_id, long su){
      return updateValue(guild_id, "su", su);
    }
    private bool updateValue(long guild_id, string valueName, long value){
      string statement = $"UPDATE Songs SET {valueName} = {value} WHERE guild_id = {guild_id}";
      var updateCommand = new SqliteCommand(statement, connection);
      connection.Open();
      int rowsUpdated = updateCommand.ExecuteNonQuery();
      connection.Close();
      return rowsUpdated == 1;
    }
    public bool deleteRow(long guild_id) {
      string delStatement = "DELETE FROM Songs WHERE guild_id = " + guild_id;
      var delCommand = new SqliteCommand(delStatement, connection);
      connection.Open();
      int rowsAffected = delCommand.ExecuteNonQuery();
      connection.Close();
      return rowsAffected > 0;
    }
    
    public long ReadSU(long guild_id){
      return ReadValue(guild_id, "su");
    }
    public long ReadQU(long guild_id){
      return ReadValue(guild_id, "qu");
    }

    private long ReadValue(long guild_id, string valueName){
      string selectStatement = "SELECT " + valueName + " FROM Songs WHERE guild_id = " + guild_id;
      var readCommand = new SqliteCommand(selectStatement, connection);
      connection.Open();
      SqliteDataReader reader = readCommand.ExecuteReader();
      connection.Close();
      if (reader.HasRows){
        return reader.GetInt64(0);
      }

      return -1;
    }
}

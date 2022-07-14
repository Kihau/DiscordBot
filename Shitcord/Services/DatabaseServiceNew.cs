using System.Text;
using Microsoft.Data.Sqlite;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Services;

public class DatabaseServiceNew
{
    private const string DATABASE_NAME = "Resources/BotDatabase.sqlite";
    private readonly SqliteConnection connection;

    public DatabaseServiceNew()
    {
        // Create database if it doesn't exist
        if (!File.Exists(DATABASE_NAME))
            File.Create(DATABASE_NAME);

        connection = new SqliteConnection("Data Source=" + DATABASE_NAME);

        connection.Open();
        CreateTableIfNotExists(MarkovTable.TABLE_NAME, MarkovTable.COLUMNS);
        CreateTableIfNotExists(GuildAudioTable.TABLE_NAME, GuildAudioTable.COLUMNS);
    }

    ~DatabaseServiceNew() => connection.Close();

    private void CreateTableIfNotExists(string tableName, List<Column> tableColumns)
    {
        var createCommand = new SqliteCommand(ProduceCreateTableQuery(tableName, tableColumns), connection);
        createCommand.ExecuteNonQuery();
    }

    public String QueryResultToString(List<List<object>> data, params Column[] columns) {
        if (columns.Length < 1)
            return "";
        
        StringBuilder builder = new StringBuilder(64);
        int cols = data.Count;
        if (cols != columns.Length) {
            //throw Exception?
            Console.WriteLine($@"Warning: the number of retrieved columns differs from target table column count: 
                                  data.Count {cols}, columns.Length {columns.Length} ");
        }
        int rows = data[0].Count;
        
        //look for max offsets in values
        int[] maxOffsets = new int[cols];
        for (int c = 0; c < cols; c++) {
            List<object> columnData = data[c];
            for (int r = 0; r < rows; r++) {
                object val = columnData[r];
                if (val is null) {
                    maxOffsets[c] = Math.Max(maxOffsets[c], 4);
                    continue;
                }
                maxOffsets[c] = Math.Max(maxOffsets[c], val.ToString().Length);
            }
        }

        //look for max offsets in column names
        for (int c = 0; c < cols; c++) {
            maxOffsets[c] = Math.Max(maxOffsets[c], columns[c].name.Length);
        }
        
        //begin by building column names
        for (int c = 0; c < cols; c++) {
            string columnName = columns[c].name;
            builder.Append(Spaces(maxOffsets[c] - columnName.Length)).Append(columnName).Append(' ');
        }
        builder.Append('\n');
        
        //append values
        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                object val = data[c][r];
                if (val is null) {
                    val = "null";
                    builder.Append(Spaces(maxOffsets[c] - 4)).Append(val).Append(' ');
                    if (c == cols - 1) {
                        builder.Append('\n');
                        break;
                    }
                    continue;
                }
                builder.Append(Spaces(maxOffsets[c] - val.ToString().Length)).Append(val).Append(' ');
                if (c == cols - 1) {
                    builder.Append('\n');
                    break;
                }
            }
        }

        return builder.ToString();
    }
    public String TableToString(string tableName, List<Column> columns)
    {
        string statement = $"SELECT * FROM {tableName}";
        var reader = executeRead(statement);
        List<List<object>> data = GatherData(reader);
        return data == null ? "Empty set" : QueryResultToString(data, columns.ToArray());
    }

    //tests if a record exists in the specified table which satisfies given condition
    public bool ExistsInTable(string tableName, Condition condition)
    {
        string existsStatement = QueryBuilder.New().Retrieve("*").From(tableName).Where(condition).Build();
        var reader = executeRead(existsStatement);
        bool exists = reader.HasRows;
        reader.Close();
        return exists;
    }
    
    public List<List<object>>? GatherData(string selectStatement)
    {
        Console.WriteLine(selectStatement);
        Console.WriteLine("length: " + selectStatement.Length);
        var reader = executeRead(selectStatement);
        return GatherData(reader);
    }
    
    private SqliteDataReader executeRead(string selectStatement)
    {
        var readCommand = new SqliteCommand(selectStatement, connection);
        return readCommand.ExecuteReader();
    }
    
    //returns the number of affected rows
    public int executeUpdate(string statement)
    {
        var updateCommand = new SqliteCommand(statement, connection);
        return updateCommand.ExecuteNonQuery();
    }
    
    //returns the number of affected rows
    public int DeleteAllRows(string tableName)
    {
        const string delStatement = "DELETE FROM tableName";
        var delCommand = new SqliteCommand(delStatement, connection);
        return delCommand.ExecuteNonQuery();
    }

    //TODO return tuple for results consisting of two columns
    //TODO return single list for singular columns
    public List<List<object>>? GatherData(SqliteDataReader reader)
    {
        int columns = reader.FieldCount;
        //empty result set?
        if (!reader.HasRows || columns < 0)
            return null;
        
        List<List<object>> dataList = new ();
        for (int i = 0; i < columns; i++)
        {
            //fill resulting list
            List<object> column = new();
            dataList.Add(column);
        }

        while (reader.Read()) {
            for (int i = 0; i < columns; i++) {
                List<object> column = dataList[i];
                object val = reader.GetValue(i);
                if (val is DBNull)
                {
                    column.Add(null);
                    continue;
                }
                column.Add(val);
            }
        }
        reader.Close();
        return dataList;
    }

    private static StringBuilder Spaces(int len)
    {
        StringBuilder spaces = new StringBuilder(len);
        for (int i = 0; i < len; i++)
            spaces.Append(' ');

        return spaces;
    }

    public static string ProduceCreateTableQuery(string tableName, List<Column> columns)
    {
        StringBuilder query = new StringBuilder($"CREATE TABLE IF NOT EXISTS {tableName} (");
        for (int i = 0; ; i++){
            Column column = columns[i];
            string identifiers = "";
            if (!column.nullable)
            {
                identifiers = " not null";
            }
            if (column.primaryKey)
            {
                identifiers = " not null PRIMARY KEY";
            }
            query.Append($"{column.name} {column.type}{identifiers}");
            if (i == columns.Count - 1){
                break;
            }
            query.Append(',');
        }
        
        query.Append(");");
        return query.ToString();
    }
}

using System.Text;
using Microsoft.Data.Sqlite;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Services;

public class DatabaseService
{
    private const string DATABASE_NAME = "Resources/BotDatabase.sqlite";
    private readonly SqliteConnection connection;

    public DatabaseService()
    {
        // Create database if it doesn't exist
        if (!File.Exists(DATABASE_NAME))
            File.Create(DATABASE_NAME);

        connection = new SqliteConnection("Data Source=" + DATABASE_NAME);

        connection.Open();
        CreateTableIfNotExists(GuildAudioTable.TABLE_NAME, GuildAudioTable.COLUMNS);
        CreateTableIfNotExists(MarkovTable.TABLE_NAME, MarkovTable.COLUMNS);
        CreateTableIfNotExists(GuildMarkovTable.TABLE_NAME, GuildMarkovTable.COLUMNS);
        CreateTableIfNotExists(
            MarkovExcludedChannelsTable.TABLE_NAME, MarkovExcludedChannelsTable.COLUMNS
        );
        CreateTableIfNotExists(AuthUsersTable.TABLE_NAME, AuthUsersTable.COLUMNS);
        CreateTableIfNotExists(AutoReplyTable.TABLE_NAME, AutoReplyTable.COLUMNS);
        CreateTableIfNotExists(AuthMarkovTable.TABLE_NAME, AuthMarkovTable.COLUMNS);
    }

    ~DatabaseService() => connection.Close();

    private void CreateTableIfNotExists(string tableName, List<Column> tableColumns)
    {
        var createCommand = new SqliteCommand(
            ProduceCreateTableQuery(tableName, tableColumns), connection
        );
        createCommand.ExecuteNonQuery();
    }

    public void ShrinkSqliteDBFile()
    {
        var updateCommand = new SqliteCommand("VACUUM", connection);
        updateCommand.ExecuteNonQuery();
    }

    public static string ProduceCreateTableQuery(string tableName, List<Column> columns)
    {
        StringBuilder query = new StringBuilder($"CREATE TABLE IF NOT EXISTS {tableName} (");
        for (int i = 0; ; i++){
            Column column = columns[i];
            string identifiers = "";
            if (!column.nullable)
                identifiers = " not null";

            if (column.primaryKey)
                identifiers = " not null PRIMARY KEY";

            query.Append($"{column.name} {column.type}{identifiers}");
            if (i == columns.Count - 1)
                break;

            query.Append(',');
        }
        
        query.Append(");");
        return query.ToString();
    }

    public String QueryResultToString(List<List<object?>>? data, params Column[] columns) {
        if (columns.Length < 1 || data is null)
            return "";
        
        StringBuilder builder = new StringBuilder(64);
        int cols = data.Count;
        if (cols != columns.Length) {
            //throw Exception?
            Console.WriteLine(
                $@"Warning: the number of retrieved columns differs from target table column count: 
                data.Count {cols}, columns.Length {columns.Length} "
            );
        }
        int rows = data[0].Count;
        
        //look for max offsets in values
        int[] maxOffsets = new int[cols];
        for (int c = 0; c < cols; c++) {
            List<object?> columnData = data[c];
            for (int r = 0; r < rows; r++) {
                object? val = columnData[r];
                if (val is null) {
                    maxOffsets[c] = Math.Max(maxOffsets[c], 4);
                    continue;
                }
                //val cannot possibly be null here
                maxOffsets[c] = Math.Max(maxOffsets[c], val.ToString()!.Length); 
            }
        }

        //look for max offsets in column names
        for (int c = 0; c < cols; c++) {
            maxOffsets[c] = Math.Max(maxOffsets[c], columns[c].name.Length);
        }
        
        //begin by building column names
        for (int c = 0; c < cols; c++) {
            string columnName = columns[c].name;
            builder.Append(Spaces(maxOffsets[c] - columnName.Length))
                .Append(columnName)
                .Append(' ');
        }
        builder.Append('\n');
        
        //append values
        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                object? val = data[c][r];
                if (val is null) {
                    val = "null";
                    builder.Append(Spaces(maxOffsets[c] - 4)).Append(val).Append(' ');
                    if (c == cols - 1) {
                        builder.Append('\n');
                        break;
                    }
                    continue;
                }
                //val cannot possibly be null here
                builder.Append(Spaces(maxOffsets[c] - val.ToString()!.Length))
                    .Append(val).Append(' ');
                if (c == cols - 1) {
                    builder.Append('\n');
                    break;
                }
            }
        }
        return builder.ToString();
    }

    public String QueryResultToString(List<List<object?>>? data, string tableName)
    {
        //it is assumed that table exists here
        string descQuery = $"PRAGMA table_info({tableName})";
        List<List<object?>>? descColumns = RetrieveColumns(descQuery);
        //merge into pattern
        if (descColumns == null || descColumns.Count < 1) {
            return "Unexpected result";
        }
        List<Column> targetTableCols = new();
        for (int i = 0; i < descColumns[1].Count; i++) {
            if (descColumns[1][i] is string colName) {
                targetTableCols.Add(new Column(colName, "N/A"));
            }
        }
        return QueryResultToString(data, targetTableCols.ToArray());
    }

    public String TableToString(string tableName, List<Column> columns)
    {
        string statement = $"SELECT * FROM {tableName}";
        var reader = executeRead(statement);
        List<List<object?>>? data = RetrieveColumns(reader);
        return data == null ? "Empty set" : QueryResultToString(data, columns.ToArray());
    }
    
    //tests if a record exists in the specified table which satisfies given condition
    public bool ExistsInTable(string tableName, Condition condition)
    {
        string existsStatement = QueryBuilder.New()
            .Retrieve("*").From(tableName).Where(condition).Build();
        var reader = executeRead(existsStatement);
        bool exists = reader.HasRows;
        reader.Close();
        return exists;
    }
    
    public List<List<object?>>? RetrieveColumns(string selectStatement)
    {
        var reader = executeRead(selectStatement);
        return RetrieveColumns(reader);
    }
    public int RetrieveNumberOfRows(string tableName)
    {
        var reader = executeRead(QueryBuilder.New()
            .Retrieve("COUNT(*)").From(tableName)
            .Build());
        
        if (!reader.Read()) {
            reader.Close();
            return 0;
        }
        
        int res = reader.GetInt32(0);
        reader.Close();
        return res;
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
        string delStatement = $"DELETE FROM {tableName}";
        var delCommand = new SqliteCommand(delStatement, connection);
        return delCommand.ExecuteNonQuery();
    }

    public bool DoesTableExist(string tableName)
    {
        string retrieve = QueryBuilder.New()
            .Retrieve("name").From("sqlite_master")
            .Where(
                Condition.New("type").Equals("table")
                .And("name").Equals($"{tableName}"))
            .Build();
        
        var reader = executeRead(retrieve);
        if (!reader.Read()) {
            reader.Close();
            return false;
        }
        reader.Close();
        return true;
    }

    // NOTE: We could return tuple for results consisting of two columns
    //       and single list for singular columns
    public static List<List<object?>>? RetrieveColumns(SqliteDataReader reader)
    {
        int columns = reader.FieldCount;
        //empty result set?
        if (!reader.HasRows || columns < 0)
            return null;
        
        List<List<object?>> dataList = new ();
        for (int i = 0; i < columns; i++) {
            //fill resulting list
            List<object?> column = new();
            dataList.Add(column);
        }

        while (reader.Read()) {
            for (int i = 0; i < columns; i++) {
                List<object?> column = dataList[i];
                object val = reader.GetValue(i);
                if (val is DBNull) {
                    column.Add(null);
                    continue;
                }
                column.Add(val);
            }
        }
        reader.Close();
        return dataList;
    }
    public static List<T?> CastColumnToList<T>(List<object?>? aColumn)
    {
        IEnumerable<T?> enumerable = CastColumn<T>(aColumn);
        return enumerable.ToList();
    }
    public static T?[] CastColumnToArray<T>(List<object?>? aColumn)
    {
        IEnumerable<T?> enumerable = CastColumn<T>(aColumn);
        return enumerable.ToArray();
    }

    private static IEnumerable<T?> CastColumn<T>(List<object?>? aColumn)
    {
        if (aColumn == null){
            throw new NullReferenceException();
        }
        try {
            return aColumn.Cast<T?>();
        }
        catch (Exception exc) {
            throw exc switch {
                InvalidCastException => new InvalidCastException("Invalid type was given at CastColumn"),
                NullReferenceException => new NullReferenceException("Given type is not nullable at CastColumn"),
                _ => exc
            };
        }
    }

    private static StringBuilder Spaces(int len)
    {
        StringBuilder spaces = new StringBuilder(len);
        for (int i = 0; i < len; i++)
            spaces.Append(' ');

        return spaces;
    }

    public string Tables()
    {
        string retrieveTables = QueryBuilder.New()
            .Retrieve("name").From("sqlite_master")
            .WhereEquals("type", "table")
            .Build();
        var reader = executeRead(retrieveTables);
        var columns = RetrieveColumns(reader);
        string res = QueryResultToString(columns, new Column("type", "text"));
        return res;
    }
}

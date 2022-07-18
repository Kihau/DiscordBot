using System.Diagnostics;
using Shitcord.Database;
using Shitcord.Database.Queries;
using Shitcord.Services;

namespace Shitcord.Tests;

public class DatabaseTests
{
    static DatabaseService service = new();
    //custom table name to avoid accidental table drop
    static string TABLE = "markov";
    public static void runDBTests()
    {
        var testTimer = Stopwatch.StartNew();
        //low:58ms
        service = new ();
        //low:43ms
        testSelectInDatabase_1();
        //low:26ms
        variousInserts_2();
        //low:25ms
        testMultipleOperations_3();

        testEscapeValues();

        SQLiteTests();
        
        Console.WriteLine("[Completed] Time elapsed: " + testTimer.ElapsedMilliseconds);
        testTimer.Stop();
        //createTableQueries();
    }
    private static void SQLiteTests()
    {
        var cols = service.RetrieveColumns("SELECT * FROM " + TABLE);
        string tableBasedOnSQL = service.QueryResultToString(cols, TABLE);
        Console.WriteLine("TABLE retrieved:");
        Console.WriteLine(tableBasedOnSQL);

        string s1 = new QueryBuilder().Insert().Into(TABLE).Columns(MarkovTable.BASE, MarkovTable.CHAIN).Values(null, null).Build();
        string s2 = new QueryBuilder().Insert().Into(TABLE).Columns(MarkovTable.CHAIN, MarkovTable.FREQUENCY).Values(null, null).Build();
        service.executeUpdate(s1);
        service.executeUpdate(s2);
        var singleCol1 = service.RetrieveColumns($"SELECT {MarkovTable.BASE.name} FROM " + TABLE);
        if (singleCol1?[0] != null) {
            List<string?> strings = DatabaseService.CastColumn<string>(singleCol1[0]);
            Console.WriteLine("STRINGS"); 
            foreach (var VARIABLE in strings) {
                Console.Write(VARIABLE + ", ");
            }
        }
        
        var singleCol2 = service.RetrieveColumns($"SELECT {MarkovTable.FREQUENCY.name} FROM " + TABLE);
        if (singleCol2?[0] != null) {
            List<long?> ints = DatabaseService.CastColumn<long?>(singleCol2[0]);
            Console.WriteLine("INTS"); 
            foreach (var VARIABLE in ints) {
                Console.Write(VARIABLE + ", ");
            }
        }
    }
    private static void testEscapeValues()
    {
        deleteIfExistsCreateNew();
        service.executeUpdate(QueryBuilder.New().Insert()
            .Into(TABLE)
            .Values("A", "B", 9).Build());
        
        service.executeUpdate(
            QueryBuilder.New()
                .Insert().Into(TABLE)
                .Values("A", "B'", "3").Build());
        service.executeUpdate(
            QueryBuilder.New()
                .Insert().Into(TABLE)
                .Values("Sheep", "\"getBytes()\"", "3").Build());
        service.executeUpdate(
            QueryBuilder.New()
                .Insert().Into(TABLE)
                .Values("Random crap", "\"' 'd'wd''()\\''\"", 23).Build());
        service.executeUpdate(
            QueryBuilder.New()
                .Insert().Into(TABLE)
                .Values("*\f robust design", "Bobby');DROP TABLES", 1337).Build());
        string statement = QueryBuilder.New()
            .Insert().Into(TABLE)
            .Values("Sheep", "\"getBytes()\"", "3").Build();

        Console.WriteLine($"Insert statement: {statement}");
        service.executeUpdate(QueryBuilder.New().Insert()
            .Into(TABLE)
            .Values("D", "E", 2).Build());
        var dbStringed = service.TableToString(TABLE, MarkovTable.COLUMNS);
        Console.WriteLine("STRINGED DB: ");
        Console.WriteLine(dbStringed);
    }

    private static void testDB()
    {
        string query = "SHOW TABLES";
        service.QueryResultToString(service.RetrieveColumns(query));
    }

    private static void testMultipleOperations_3()
    {
        Console.WriteLine("testMultipleOperations_3:");
        deleteIfExistsCreateNew();
        
        //INSERT queries
        service.executeUpdate(QueryBuilder.New().Insert()
            .Into(TABLE)
            .Values(null, "B", null).Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.CHAIN.name)
            .Into(TABLE)
            .Values("E").Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.BASE.name, MarkovTable.CHAIN.name)
            .Into(TABLE)
            .Values("G", "H").Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Into(TABLE)
            .Values("L", "M", null).Build());
        
        string table1 = service.TableToString(TABLE, MarkovTable.COLUMNS);
        Console.WriteLine("result of inserts");
        Console.WriteLine(table1);
        
        //UPDATE queries
        string updateQ = QueryBuilder.New().Update(TABLE)
            .Set(MarkovTable.CHAIN.name, "freshchain")
            .Where(Condition.New(MarkovTable.CHAIN.name).IsDiffFrom("H")).Build();
        int rowsAffected = service.executeUpdate(updateQ);

        string table2 = service.TableToString(TABLE, MarkovTable.COLUMNS);
        Console.WriteLine($"affected {rowsAffected} rows as a result");
        Console.WriteLine(table2);
        
        bool exists1 = service.DoesTableExist(TABLE);
        bool exists2 = service.DoesTableExist("ttt");
        Console.WriteLine($"Does {TABLE} exist {exists1}");
        Console.WriteLine($"Does ttt exist {exists2}");

        Console.WriteLine("Tables in DB:");
        Console.WriteLine($"{service.Tables()}");

    }

    private static void variousInserts_2()
    {
        Console.WriteLine("variousInserts_2:");
        deleteIfExistsCreateNew();

        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.CHAIN.name)
            .Into(TABLE)
            .Values("B").Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.BASE.name, MarkovTable.CHAIN.name)
            .Into(TABLE)
            .Values("B", "C").Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.BASE.name, MarkovTable.CHAIN.name, MarkovTable.FREQUENCY.name)
            .Into(TABLE)
            .Values("X", "Y", 32).Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Into(TABLE)
            .Values("F", "U", 999).Build());
        
        //to investigate - ExistsInTable causes table lock
        //targeting Y
        var CONDITION = Condition.New(MarkovTable.FREQUENCY.name).IsLessThan(333)
                                 .And(MarkovTable.FREQUENCY.name).IsMoreThan(10);

        bool existsInTable = service.ExistsInTable(TABLE, CONDITION);
        Console.WriteLine("[Exists] Y scuffed: " + existsInTable);
        
        string q = QueryBuilder.New().Retrieve("*")
            .From(TABLE).Where(CONDITION).Build();
        bool exists;
        if (service.RetrieveColumns(q) == null) 
            exists = false;
        else
            exists = true;
        
        Console.WriteLine($"[Exists Y (not scuffed)] {exists}");

        bool existsY = service.ExistsInTable(TABLE, 
            Condition.New(MarkovTable.CHAIN.name).IsDiffFrom("B")
                .And(MarkovTable.CHAIN.name).IsDiffFrom("C")
                .And(MarkovTable.CHAIN.name).IsDiffFrom("U"));
        Console.WriteLine("[Exists] records other than B, C, U: " + existsY);
        
        bool existsFreq = service.ExistsInTable(TABLE,
            Condition.New(MarkovTable.FREQUENCY.name).IsLessThan(999)
                .And(MarkovTable.FREQUENCY.name).IsMoreThan(40));
        Console.WriteLine("[Exists] frequency 40<f<999: " + existsFreq);
            
        string table = service.TableToString(TABLE, MarkovTable.COLUMNS);
        Console.WriteLine(table);
    }

    private static void deleteIfExistsCreateNew()
    {
        service.executeUpdate($"DROP TABLE IF EXISTS {TABLE}");
        //create table
        string createTableQuery = DatabaseService.ProduceCreateTableQuery(TABLE, MarkovTable.COLUMNS);
        service.executeUpdate(createTableQuery);
    }


    private static void testSelectInDatabase_1()
    {
        deleteIfExistsCreateNew();
        //introduce data
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("i", "dont", 7).Build());
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("on", null, 2).Build());
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("see", "sz", 3).Build());
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("sz", "else", 2).Build());
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("else", "if", 1).Build());
        
        //retrieve two cols
        string query = QueryBuilder.New().Retrieve(MarkovTable.CHAIN.name, MarkovTable.BASE.name).From(TABLE).Build();
        //retrieve all cols
        string allColQuery = QueryBuilder.New().Retrieve("*").From(TABLE).Build();
        Console.WriteLine("query: " + query);
        Console.WriteLine("allColQuery: " + allColQuery);
        List<List<object?>>? data = service.RetrieveColumns(query);
        List<List<object?>>? allData = service.RetrieveColumns(allColQuery);
        
        String res1 = service.QueryResultToString(data, MarkovTable.CHAIN, MarkovTable.BASE);
        String res2 = service.QueryResultToString(allData, MarkovTable.COLUMNS.ToArray());
        String table = service.TableToString(TABLE, MarkovTable.COLUMNS);
        Console.WriteLine(res1);
        Console.WriteLine(res2);
        //Console.Write(table);

        int rows = service.RetrieveNumberOfRows(TABLE);
        Console.WriteLine($"Rows in {TABLE}: {rows}");
    }

    private static void createTableQueries()
    {
        string createQuery1 = DatabaseService.ProduceCreateTableQuery(MarkovTable.TABLE_NAME, MarkovTable.COLUMNS);
        string createQuery2 = DatabaseService.ProduceCreateTableQuery(GuildAudioTable.TABLE_NAME, GuildAudioTable.COLUMNS);
        Console.WriteLine(createQuery1);
        Console.WriteLine(createQuery2);
    }
}

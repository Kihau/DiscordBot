using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Shitcord.Database;
using Shitcord.Database.Queries;
using Shitcord.Services;

namespace Shitcord.Tests;

public class DatabaseTests
{
    static DatabaseServiceNew service;
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
        //testDB();
        Console.WriteLine("[Completed] Time elapsed: " + testTimer.ElapsedMilliseconds);
        testTimer.Stop();
        //createTableQueries();
    }

    private static void testDB()
    {
        string query = "SHOW TABLES";
        service.QueryResultToString(service.GatherData(query));

    }

    private static void testMultipleOperations_3()
    {
        Console.WriteLine("testMultipleOperations:");
        int affected = service.executeUpdate($"DROP TABLE IF EXISTS {TABLE}");
        //WHY IS THE TABLE LOCKED   
        Console.WriteLine($"{affected} rows, table ain't locked");
        //create table
        string createTableQuery = DatabaseServiceNew.ProduceCreateTableQuery(TABLE, MarkovTable.COLUMNS);
        service.executeUpdate(createTableQuery);
        
        //INSERT queries
        service.executeUpdate(QueryBuilder.New().Insert()
            .Into(TABLE)
            .Values(null, "B", null).Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.CHAIN.name)
            .Into(TABLE)
            .Values("E").Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.STRING.name, MarkovTable.CHAIN.name)
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
    }

    private static void variousInserts_2()
    {
        Console.WriteLine("variousInserts:");
        
        service.executeUpdate($"DROP TABLE IF EXISTS {TABLE}");
        //create table
        string createTableQuery = DatabaseServiceNew.ProduceCreateTableQuery(TABLE, MarkovTable.COLUMNS);
        service.executeUpdate(createTableQuery);
        
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.CHAIN.name)
            .Into(TABLE)
            .Values("B").Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.STRING.name, MarkovTable.CHAIN.name)
            .Into(TABLE)
            .Values("B", "C").Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Columns(MarkovTable.STRING.name, MarkovTable.CHAIN.name, MarkovTable.FREQUENCY.name)
            .Into(TABLE)
            .Values("X", "Y", 32).Build());
        service.executeUpdate(QueryBuilder.New().Insert()
            .Into(TABLE)
            .Values("F", "U", 999).Build());
        //targeting Y
        bool existsY = service.ExistsInTable(TABLE, 
            Condition.New(MarkovTable.CHAIN.name).IsDiffFrom("B")
                .And(MarkovTable.CHAIN.name).IsDiffFrom("C")
                .And(MarkovTable.CHAIN.name).IsDiffFrom("U"));

        bool existsFreq = service.ExistsInTable(TABLE,
            Condition.New(MarkovTable.FREQUENCY.name).IsLessThan(999)
                .And(MarkovTable.FREQUENCY.name).IsMoreThan(40));

        Console.WriteLine("[Exists] records other than B, C, U: " + existsY);
        Console.WriteLine("[Exists] frequency 40<f<999: " + existsFreq);

        string table = service.TableToString(TABLE, MarkovTable.COLUMNS);
        Console.WriteLine(table);
    }


    private static void testSelectInDatabase_1()
    {
        service.executeUpdate($"DROP TABLE IF EXISTS {TABLE}");
        //create table
        string createTableQuery = DatabaseServiceNew.ProduceCreateTableQuery(TABLE, MarkovTable.COLUMNS);
        service.executeUpdate(createTableQuery);
        //introduce data
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("i", "dont", 7).Build());
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("on", null, 2).Build());
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("see", "sz", 3).Build());
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("sz", "else", 2).Build());
        service.executeUpdate(QueryBuilder.New().Insert().Into(TABLE).Values("else", "if", 1).Build());
        
        //retrieve two cols
        string query = QueryBuilder.New().Retrieve(MarkovTable.CHAIN.name, MarkovTable.STRING.name).From(TABLE).Build();
        //retrieve all cols
        string allColQuery = QueryBuilder.New().Retrieve("*").From(TABLE).Build();
        Console.WriteLine("query: " + query);
        Console.WriteLine("allColQuery: " + allColQuery);
        List<List<object>> data = service.GatherData(query);
        List<List<object>> allData = service.GatherData(allColQuery);
        
        String res1 = service.QueryResultToString(data, MarkovTable.CHAIN, MarkovTable.STRING);
        String res2 = service.QueryResultToString(allData, MarkovTable.COLUMNS.ToArray());
        String table = service.TableToString(TABLE, MarkovTable.COLUMNS);
        Console.Write(res1);
        Console.Write(res2);
        //Console.Write(table);
        
    }

    private static void createTableQueries()
    {
        string createQuery1 = DatabaseService.ProduceCreateTableQuery(MarkovTable.TABLE_NAME, MarkovTable.COLUMNS);
        string createQuery2 = DatabaseService.ProduceCreateTableQuery(GuildAudioTable.TABLE_NAME, GuildAudioTable.COLUMNS);
        Console.WriteLine(createQuery1);
        Console.WriteLine(createQuery2);
    }
}
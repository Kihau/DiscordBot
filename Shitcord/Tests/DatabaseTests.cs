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
    public static void runTests()
    {
        service = new ();
        testSelectInDatabase();
        //createTableQueries();
    }
    

    private static void testSelectInDatabase()
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
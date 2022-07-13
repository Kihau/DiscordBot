using Shitcord.Database;
using Shitcord.Services;

namespace Shitcord.Tests;

public class DatabaseTests
{
    
    public static void runTests()
    {
        createTableQueries();
    }

    private static void createTableQueries()
    {
        string createQuery1 = DatabaseService.ProduceCreateTableQuery(MarkovTable.TABLE_NAME, MarkovTable.COLUMNS);
        string createQuery2 = DatabaseService.ProduceCreateTableQuery(GuildAudioTable.TABLE_NAME, GuildAudioTable.COLUMNS);
        Console.WriteLine(createQuery1);
        Console.WriteLine(createQuery2);
    }
}
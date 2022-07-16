using DSharpPlus;
using DSharpPlus.Entities;
using System.Collections.Concurrent;
using Shitcord.Services;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Data;

public class GuildMarkovData
{
    public DiscordGuild Guild { get; }
    public DatabaseService Database { get; } 

    public bool IsEnabled { get; set; } = true;
    // NOTE: Adding channel blocks either auto response and data gathering
    public List<ulong>? ExcludedChannelIDs { get; set; } = new();

    public const int MAX_CHANCE = 1000;
    public bool ResponseEnabled { get; set; } = false;
    public int ResponseChance { get; set; } = 100;
    public TimeSpan ResponseTimeout { get; set; } = new(0, 30, 0);
    public DateTime LastResponse { get; set; } = DateTime.Now;

    public const int DEFAULT_MIN = 8;
    public const int DEFAULT_MAX = 28;
    public int MinChainLength { get; set; } = 8;
    public int MaxChainLength { get; set; } = 28;

    public GuildMarkovData() {}
    public GuildMarkovData(DiscordGuild guild, DatabaseService database) 
    {
        Guild = guild;
        Database = database;
        LoadDataFromDatabase();
    }

    public void LoadDataFromDatabase()
    {
        ExcludedChannelIDs = Database.RetrieveColumns(QueryBuilder
            .New().Retrieve(MarkovExcludedChannelsTable.EXCLUDED_ID)
            .From(MarkovExcludedChannelsTable.TABLE_NAME)
            .WhereEquals(MarkovExcludedChannelsTable.GUILD_ID, Guild.Id)
            .Build()
        )?.Select(column => (ulong)(long)column[0]).ToList();

        var data = Database.RetrieveColumns(QueryBuilder
            .New().Retrieve("*").From(GuildMarkovTable.TABLE_NAME)
            .WhereEquals(GuildMarkovTable.GUILD_ID, Guild.Id).Build()
        );

        if (data is not null) {
            // Insert - create new row
            return;
        }

        // Load from data the "data" list
    }
}

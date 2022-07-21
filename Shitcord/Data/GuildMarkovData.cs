using DSharpPlus.Entities;
using Shitcord.Services;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Data;

// I could easily add an option to store markov strings separete for each guild.
// That being said, currently this bo is only in ~3 guilds so there is no need for that
public class GuildMarkovData
{
    public DiscordGuild Guild { get; }
    public DatabaseService Database { get; } 

    public bool IsEnabled { get; set; } = false;
    //public bool IsGlobal { get; set; } = false;

    // NOTE: Adding channel blocks either auto response and data gathering
    public List<ulong> ExcludedChannelIDs { get; set; } = new();

    public const int MAX_CHANCE = 1000;
    public bool ResponseEnabled { get; set; } = false;
    public int ResponseChance { get; set; } = 100;
    public TimeSpan ResponseTimeout { get; set; } = new(0, 30, 0);
    public DateTime LastResponse { get; set; } = DateTime.Now;

    public const int DEFAULT_MIN = 8;
    public const int DEFAULT_MAX = 28;
    public int MinChainLength { get; set; } = DEFAULT_MIN;
    public int MaxChainLength { get; set; } = DEFAULT_MAX;

    public GuildMarkovData(DiscordGuild guild, DatabaseService database) 
    {
        Guild = guild;
        Database = database;
        LoadDataFromDatabase();
    }

    public void LoadDataFromDatabase()
    {
        var excluded = Database.RetrieveColumns(QueryBuilder
            .New().Retrieve(MarkovExcludedChannelsTable.EXCLUDED_ID)
            .From(MarkovExcludedChannelsTable.TABLE_NAME)
            .WhereEquals(MarkovExcludedChannelsTable.GUILD_ID, Guild.Id)
            .Build()
        )?[0].Select(
            column => (ulong)(long)(column ?? 
                throw new NullReferenceException("Column should never be null."))
        ).ToList();

        if (excluded != null)
            ExcludedChannelIDs = excluded;

        var data = Database.RetrieveColumns(QueryBuilder
            .New().Retrieve("*").From(GuildMarkovTable.TABLE_NAME)
            .WhereEquals(GuildMarkovTable.GUILD_ID, Guild.Id).Build()
        );

        if (data is null) {
            Database.executeUpdate(QueryBuilder
                .New().Insert().Into(GuildMarkovTable.TABLE_NAME)
                .Values(
                    Guild.Id, IsEnabled, ResponseEnabled, ResponseChance, 
                    ResponseTimeout.Ticks, MinChainLength, MaxChainLength
                ).Build()
            );
            return;
        }

        IsEnabled       = (long)(data[1][0] ?? 0) == 1;
        ResponseEnabled = (long)(data[2][0] ?? 0) == 1;
        ResponseChance  = (int)(long)(data[3][0] ?? 100);
        ResponseTimeout = TimeSpan.FromTicks((long)(data[4][0] ?? TimeSpan.TicksPerHour / 2)); 
        MinChainLength  = (int)(long)(data[5][0] ?? MinChainLength);
        MaxChainLength  = (int)(long)(data[6][0] ?? MaxChainLength);
    }

    public void UpdateEnabledFlag() {
        Database.executeUpdate(QueryBuilder
            .New().Update(GuildMarkovTable.TABLE_NAME)
            .WhereEquals(GuildMarkovTable.GUILD_ID, Guild.Id)
            .Set(GuildMarkovTable.ENABLED, IsEnabled)
            .Build()
        );
    }

    public void UpdateAutoResponse() {
        Database.executeUpdate(QueryBuilder
            .New().Update(GuildMarkovTable.TABLE_NAME)
            .WhereEquals(GuildMarkovTable.GUILD_ID, Guild.Id)
            .Set(GuildMarkovTable.RESPONSE_ENABLED, ResponseEnabled)
            .Set(GuildMarkovTable.RESPONSE_CHANCE, ResponseChance)
            .Set(GuildMarkovTable.RESPONSE_TIMEOUT, ResponseTimeout.Ticks)
            .Build()
        );
    }

    public void UpdateChainLength() {
        Database.executeUpdate(QueryBuilder
            .New().Update(GuildMarkovTable.TABLE_NAME)
            .WhereEquals(GuildMarkovTable.GUILD_ID, Guild.Id)
            .Set(GuildMarkovTable.MIN_CHAIN_LEN, MinChainLength)
            .Set(GuildMarkovTable.MAX_CHAIN_LEN, MaxChainLength)
            .Build()
        );
    }

    public void InsertNewExcludeChannel(ulong channel_id) 
    {
        ExcludedChannelIDs.Add(channel_id);
        Database.executeUpdate(QueryBuilder
            .New().Insert().Into(MarkovExcludedChannelsTable.TABLE_NAME)
            .Values(Guild.Id, channel_id).Build()
        );
    }

    public void DeleteExcludeChannel(ulong channel_id) 
    {
        Database.executeUpdate(QueryBuilder
            .New().Delete().From(MarkovExcludedChannelsTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovExcludedChannelsTable.GUILD_ID).Equals(Guild.Id)
                .And(MarkovExcludedChannelsTable.EXCLUDED_ID).Equals(channel_id)
            ).Build()
        );
        ExcludedChannelIDs.Remove(channel_id);
    }

    public void DeleteAllExcludeChannel() 
    {
        ExcludedChannelIDs.Clear();
        Database.DeleteAllRows(MarkovExcludedChannelsTable.TABLE_NAME);
    }
}

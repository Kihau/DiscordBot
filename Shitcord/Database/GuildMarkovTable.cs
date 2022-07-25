namespace Shitcord.Database;

public static class GuildMarkovTable
{
    public const string TABLE_NAME = "guild_markov_data";
    
    public static readonly Column GUILD_ID          = new("guild_id", "bigint", false, true);
    public static readonly Column ENABLED           = new("enabled", "boolean");
    public static readonly Column GLOBAL            = new("enabled", "boolean");
    public static readonly Column RESPONSE_ENABLED  = new("response_enabled", "boolean");
    public static readonly Column RESPONSE_CHANCE   = new("response_chance", "int");
    public static readonly Column RESPONSE_TIMEOUT  = new("response_timeout", "bigint");
    public static readonly Column MIN_CHAIN_LEN     = new("min_chain_len", "int");
    public static readonly Column MAX_CHAIN_LEN     = new("max_chain_len", "int");
    public static readonly List<Column> COLUMNS     = new() {
        GUILD_ID, ENABLED, GLOBAL, RESPONSE_ENABLED, RESPONSE_CHANCE, RESPONSE_TIMEOUT,
        MIN_CHAIN_LEN, MAX_CHAIN_LEN,
    };
}

public static class MarkovExcludedChannelsTable
{
    public static readonly string TABLE_NAME = "markov_excluded_channels";

    public static readonly Column GUILD_ID          = new("guild_id", "bigint");
    public static readonly Column EXCLUDED_ID       = new("excluded_id", "bigint");
    public static readonly List<Column> COLUMNS     = new() {
        GUILD_ID, EXCLUDED_ID
    };
}

namespace Shitcord.Database;

public class MarkovTable
{
    public const string TABLE_NAME = "markov_data";
    
    public static readonly Column STRING = new("str", "varchar(255)");
    public static readonly Column CHAIN = new("chain_str", "varchar(255)");
    public static readonly Column FREQUENCY = new("frequency", "int");
    public readonly List<Column> columns = new(){STRING, CHAIN, FREQUENCY};
    
    public static Column? PRIMARY_KEY = null;
}
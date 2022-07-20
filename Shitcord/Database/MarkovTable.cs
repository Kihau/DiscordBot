namespace Shitcord.Database;

public class MarkovTable
{
    public const string TABLE_NAME = "markov_data";
    
    public static readonly Column BASE          = new("base_str", "varchar(255)",false);
    public static readonly Column CHAIN         = new("chain_str", "varchar(255)", false);
    public static readonly Column FREQUENCY     = new("frequency", "int", false);
    public static readonly List<Column> COLUMNS = new() { BASE, CHAIN, FREQUENCY };
}

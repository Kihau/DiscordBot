using Shitcord.Database;

namespace Shitcord.Tests;

public class TestTable
{
    public const string TABLE_NAME = "markov";
    
    public static readonly Column BASE          = new("base_str", "varchar(255)");
    public static readonly Column CHAIN         = new("chain_str", "varchar(255)");
    public static readonly Column FREQUENCY     = new("frequency", "int");
    public static readonly List<Column> COLUMNS = new() { BASE, CHAIN, FREQUENCY };
}
namespace Shitcord.Database;

public class Column
{
    public string name;
    public string type;
    public bool nullable;

    public Column(string name, string type)
    {
        this.name = name;
        this.type = type;
    }
    public Column(string name, string type, bool nullable = true)
    {
        //TODO this(name, type);
        this.name = name;
        this.type = type;
    }
}
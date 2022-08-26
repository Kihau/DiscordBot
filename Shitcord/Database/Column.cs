namespace Shitcord.Database;

public class Column
{
    public readonly string name;
    public readonly string type;
    public readonly bool nullable = true;
    public readonly bool primaryKey = false;

    public Column(string name, string type)
    {
        this.name = name;
        this.type = type;
    }
    public Column(string name, string type, bool nullable = true) : this (name, type)
    {
        this.nullable = nullable;
    }
    public Column(
        string name, string type, bool nullable = true, bool primaryKey = false
    ) : this (name, type, nullable) {
        this.primaryKey = primaryKey;
    }
}

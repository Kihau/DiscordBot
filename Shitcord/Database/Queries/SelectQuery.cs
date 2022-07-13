using Shitcord.Extensions;

namespace Shitcord.Services.Database;

public class SelectQuery
{
    //SELECT c1 FROM t WHERE c2 = val;
    
    private readonly string c1;
    private string table;
    private string c2;
    private string oper;
    private object val;

    public SelectQuery(string colName)
    {
        c1 = colName;
    }
    public SelectQuery From(string tableName)
    {
        table = tableName;
        return this;
    }
    public SelectQuery Where(string columnName)
    {
        c2 = columnName;
        return this;
    }
    //hides the method from the object class
    public new SelectQuery Equals(object value)
    {
        oper = "=";
        val = value;
        return this;
    }
    public SelectQuery IsLessThan(object value)
    {
        oper = "<";
        val = value;
        return this;
    }
    public SelectQuery IsMoreThan(object value)
    {
        oper = ">";
        val = value;
        return this;
    }
    public string Build()
    {
        if (table==null || c2==null || oper==null || val==null)
        {
            throw new Exception("A required field is null");
        }

        return val is string
            ? $"SELECT {c1} FROM {table} WHERE {c2} {oper} \"{val}\"" 
            : $"SELECT {c1} FROM {table} WHERE {c2} {oper} {val}";
    }
}
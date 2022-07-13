using System.Text;

namespace Shitcord.Database.Queries;

public class UpdateQuery
{
    //UPDATE t SET c1 = x1 WHERE c2 = x2
    
    private readonly string table;
    private string c1;
    private object val1;
    private Condition condition;

    public UpdateQuery(string tableName)
    {
        table = tableName;
    }
    
    public UpdateQuery Set(string columnName, object value)
    {
        c1 = columnName;
        val1 = value;
        return this;
    }
    public UpdateQuery Where(Condition condition)
    {
        this.condition = condition;
        return this;
    }
    public string Build()
    {
        if (table==null || c1==null || val1==null)
        {
            throw new Exception("A required field is null");
        }

        StringBuilder queryBuilder = new StringBuilder($"UPDATE {table} SET {c1} = ");
        AttachValue(queryBuilder);
        if (condition == null)
        {
            return queryBuilder.ToString();
        }
        
        queryBuilder.Append($" WHERE {condition.Get()}");

        return queryBuilder.ToString();
    }

    private void AttachValue(StringBuilder queryBuilder)
    {
        if (val1 is string)
        {
            queryBuilder.Append($"\"{val1}\"");
        }
        else
        {
            queryBuilder.Append($"{val1}");
        }
    }
}

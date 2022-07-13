using System.Text;

namespace Shitcord.Services.Database;

public class UpdateQuery
{
    //UPDATE t SET c1 = x1 WHERE c2 = x2
    
    private readonly string table;
    private string c1;
    private object val1;
    private string c2;
    private string oper;
    private object val2;

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
    public UpdateQuery Where(string fieldName)
    {
        c2 = fieldName;
        return this;
    }
    //hides the method from the object class
    public new UpdateQuery Equals(object value)
    {
        oper = "=";
        val2 = value;
        return this;
    }
    
    public UpdateQuery IsLessThan(object value)
    {
        oper = "<";
        val2 = value;
        return this;
    }
    public UpdateQuery IsMoreThan(object value)
    {
        oper = ">";
        val2 = value;
        return this;
    }
    public string Build()
    {
        if (table==null || c1==null || val1==null)
        {
            throw new Exception("A required field is null");
        }

        StringBuilder queryBuilder = new StringBuilder($"UPDATE {table} SET {c1} = ");
        if (c2 == null && oper == null && val2 == null)
        {
            AttachValue(queryBuilder, val1);
            return queryBuilder.ToString();
        }

        if (c2 == null || oper == null || val2 == null)
        {
            throw new Exception("A required field is null");
        }
        
        AttachValue(queryBuilder, val1);
        queryBuilder.Append($"WHERE {c2} {oper} ");
        AttachValue(queryBuilder, val2);
        
        return queryBuilder.ToString();
    }

    private void AttachValue(StringBuilder queryBuilder, object value)
    {
        if (value is string)
        {
            queryBuilder.Append($"\"{value}\"");
        }
        else
        {
            queryBuilder.Append($"{value}");
        }
    }
}
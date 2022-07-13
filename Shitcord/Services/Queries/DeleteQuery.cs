namespace Shitcord.Services.Database;

public class DeleteQuery
{
    private string table;
    private string c1;
    private string oper;
    private object val;

    //DELETE FROM t WHERE c1 = val;
    
    public DeleteQuery From(string tableName)
    {
        table = tableName;
        return this;
    }
    public DeleteQuery Where(string columnName)
    {
        c1 = columnName;
        return this;
    }
    //hides the method from the object class
    public new DeleteQuery Equals(object value)
    {
        oper = "=";
        val = value;
        return this;
    }
    public DeleteQuery IsLessThan(object value)
    {
        oper = "<";
        val = value;
        return this;
    }
    public DeleteQuery IsMoreThan(object value)
    {
        oper = ">";
        val = value;
        return this;
    }
    
    public string Build()
    {
        if (table == null)
        {
            throw new Exception("A required field is null");
        }
        if (c1==null || oper==null || val==null)
        {
            //deletes all rows from table
            return $"DELETE FROM {table}";
        }
        return $"DELETE FROM {table} WHERE {c1} {oper} {val};";
    }

}
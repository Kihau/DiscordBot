namespace Shitcord.Database.Queries;

public class DeleteQuery
{
    private string table;
    private Condition condition;
    //DELETE FROM t WHERE c1 = val;
    
    public DeleteQuery From(string tableName)
    {
        table = tableName;
        return this;
    }
    public DeleteQuery Where(Condition condition)
    {
        this.condition = condition;
        return this;
    }

    public string Build()
    {
        if (table == null)
        {
            throw new Exception("A required field is null");
        }
        if (condition == null)
        {
            //deletes all rows from table
            return $"DELETE FROM {table}";
        }
        return $"DELETE FROM {table} WHERE {condition.Get()};";
    }
}

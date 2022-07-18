namespace Shitcord.Database.Queries;

public class DeleteQuery
{
    private string? _table;
    private Condition? _condition;
    
    public DeleteQuery From(string tableName)
    {
        _table = tableName;
        return this;
    }
    public DeleteQuery Where(Condition condition)
    {
        _condition = condition;
        return this;
    }
    public DeleteQuery WhereEquals(string columnName, object value)
    {
        _condition = Condition.New(columnName).Equals(value);
        return this;
    }
    public DeleteQuery WhereEquals(Column column, object value)
    {
        return WhereEquals(column.name, value);
    }
    public string Build()
    {
        if (_table == null)
            throw new QueryException("A required field is null");
        
        if (_condition == null)
            return $"DELETE FROM {_table}";
        return $"DELETE FROM {_table} WHERE {_condition.Get()};";
    }
}

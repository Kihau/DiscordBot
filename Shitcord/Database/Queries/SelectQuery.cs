using System.Text;

namespace Shitcord.Database.Queries;

public class SelectQuery
{
    private Condition? _condition;
    private string? _table;
    private string? _orderBy;
    private readonly string[] _cols;
    private bool _isAscending = true;
    private bool _distinct = false;
    private bool _isRandom = false;
    private int _limit = -1;

    public SelectQuery(params string[] columnNames)
    {
        if (columnNames.Length < 1)
            throw new QueryException("No column parameters were given");
        
        _cols = columnNames;
    }
    public SelectQuery (params Column[] columns)
    {
        string[] names = new string[columns.Length];
        Column[] colsArr = columns.ToArray();
        for (int i = 0; i < columns.Length; i++) {
            names[i] = colsArr[i].name;
        }
        _cols = names;
    }
    public SelectQuery Distinct()
    {
        _distinct = true;
        return this;
    }
    public SelectQuery From(string tableName)
    {
        _table = tableName;
        return this;
    }
    public SelectQuery Where(Condition condition)
    {
        _condition = condition;
        return this;
    }
    public SelectQuery WhereEquals(string columnName, object value)
    {
        _condition = Condition.New(columnName).Equals(value);
        return this;
    }
    public SelectQuery WhereEquals(Column column, object value)
    {
        return WhereEquals(column.name, value);
    }
    public SelectQuery OrderBy(string columnName, bool isAscending = true)
    {
        _orderBy = columnName;
        _isAscending = isAscending;
        return this;
    }
    public SelectQuery OrderBy(Column column, bool isAscending = true)
    {
        return OrderBy(column.name, isAscending);
    }
    //limit the number of rows returned by the query (upper constraint)
    public SelectQuery Random()
    {
        _isRandom = true;
        return this;
    }
    //limit the number of rows returned by the query (upper constraint)
    public SelectQuery Limit(int limit = 1)
    {
        if (limit < 0)
            throw new QueryException($"Specified limit {limit} is less than zero");
        
        _limit = limit;
        return this;
    }
    private void AppendColumns(StringBuilder sb)
    {
        for (int i = 0; ; i++) {
            if (i == _cols.Length - 1) {
                sb.Append(_cols[i]).Append(' ');
                break;
            }
            sb.Append(_cols[i]).Append(',');
        }
    }
    public string Build()
    {
        if (_table==null) 
            throw new QueryException("A required field is null");
        
        StringBuilder selectQuery = new StringBuilder("SELECT ");

        if (_distinct) selectQuery.Append("DISTINCT ");
        
        if(_cols.Length == 1 && _cols[0] == "*")
            selectQuery.Append('*').Append(' ');
        else AppendColumns(selectQuery);
        

        selectQuery.Append($"FROM {_table} ");
        if (_condition != null) {
            selectQuery.Append("WHERE ");
            selectQuery.Append($"{_condition.Get()}");
        }

        if (_orderBy != null) {
            selectQuery.Append($"ORDER BY {_orderBy} ");
            if (!_isAscending) selectQuery.Append("DESC");
        } else if (_isRandom) selectQuery.Append($"ORDER BY RANDOM() ");
        
        if (_limit != -1) selectQuery.Append($"LIMIT {_limit}");
        return selectQuery.ToString();
    }
}

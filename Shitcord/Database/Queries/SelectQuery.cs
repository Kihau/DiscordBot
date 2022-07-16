using System.Text;

namespace Shitcord.Database.Queries;

public class SelectQuery
{

    //SELECT c1 FROM t WHERE c2 = val;
    //alternatively:
    //SELECT (c1, c2, c3) FROM t WHERE c2 = val;
    //select every column:
    //SELECT * FROM t WHERE c2 = val;

    private bool distinct;
    private readonly string[] cols;
    private string table;
    private Condition condition;
    private string orderBy;
    private bool isAscending = true;

    public SelectQuery(params string[] columnNames)
    {
        if (columnNames.Length < 1)
            throw new QueryException("No column parameters were given");
        
        cols = columnNames;
    }
    public SelectQuery (params Column[] columns)
    {
        string[] names = new string[columns.Length];
        Column[] colsArr = columns.ToArray();
        for (int i = 0; i < columns.Length; i++) {
            names[i] = colsArr[i].name;
        }
        cols = names;
    }
    public SelectQuery Distinct()
    {
        distinct = true;
        return this;
    }
    public SelectQuery From(string tableName)
    {
        table = tableName;
        return this;
    }
    public SelectQuery Where(Condition condition)
    {
        this.condition = condition;
        return this;
    }
    public SelectQuery WhereEquals(string columnName, object value)
    {
        condition = Condition.New(columnName).Equals(value);
        return this;
    }
    public SelectQuery WhereEquals(Column column, object value)
    {
        return WhereEquals(column.name, value);
    }
    public SelectQuery OrderBy(string columnName, bool isAscending = true)
    {
        orderBy = columnName;
        this.isAscending = isAscending;
        return this;
    }
    public SelectQuery OrderBy(Column column, bool isAscending = true)
    {
        return OrderBy(column.name, isAscending);
    }

    private void AppendColumns(StringBuilder sb)
    {
        for (int i = 0; ; i++) {
            if (i == cols.Length - 1) {
                sb.Append(cols[i]).Append(' ');
                break;
            }
            sb.Append(cols[i]).Append(',');
        }
    }
    public string Build()
    {
        if (table==null) 
            throw new QueryException("A required field is null");
        

        StringBuilder selectQuery = new StringBuilder("SELECT ");

        if (distinct)
            selectQuery.Append("DISTINCT ");
        
        if(cols.Length==1 && cols[0]=="*")
            selectQuery.Append('*').Append(' ');
        else 
            AppendColumns(selectQuery);
        

        selectQuery.Append($"FROM {table} ");
        if (condition != null){
            selectQuery.Append("WHERE ");
            selectQuery.Append($"{condition.Get()}");
        }

        if (orderBy != null) {
            selectQuery.Append($"ORDER BY {orderBy} ");
            if (!isAscending) {
                selectQuery.Append("DESC");
            }
        }
        return selectQuery.ToString();
    }
}

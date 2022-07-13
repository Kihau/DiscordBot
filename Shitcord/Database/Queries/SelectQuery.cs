using System.Text;
using Shitcord.Database.Queries;

namespace Shitcord.Services.Database;

public class SelectQuery
{

    //SELECT c1 FROM t WHERE c2 = val;
    //alternatively:
    //SELECT (c1, c2, c3) FROM t WHERE c2 = val;
    //select every column:
    //SELECT * FROM t WHERE c2 = val;
    
    private readonly string[] cols;
    private string table;
    private Condition condition;

    public SelectQuery(params string[] columnNames)
    {
        if (columnNames.Length < 1)
        {
            throw new Exception("No column parameters were given");
        }
        cols = columnNames;
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
    private void AppendColumns(StringBuilder sb)
    {
        for (int i = 0; ; i++)
        {
            if (i == cols.Length - 1)
            {
                sb.Append(cols[i]);
                break;
            }
            sb.Append(cols[i]).Append(',');
        }
    }
    public string Build()
    {
        if (table==null)
        {
            throw new Exception("A required field is null");
        }

        StringBuilder selectQuery = new StringBuilder("SELECT ");
        
        if(cols.Length==1 && (cols[0]=="*" || cols[0]=="(*)"))
        {
            selectQuery.Append('*').Append(' ');
        }
        else
        {
            selectQuery.Append('(');
            AppendColumns(selectQuery);
            selectQuery.Append(')').Append(' ');
        }

        selectQuery.Append($"FROM {table} ");
        if (condition != null)
        {
            selectQuery.Append("WHERE ");
            selectQuery.Append($"{condition.Get()}");
        }
        return selectQuery.ToString();
    }
}
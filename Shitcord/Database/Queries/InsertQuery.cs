using System.Text;

namespace Shitcord.Database.Queries;

public class InsertQuery
{
    private string table;
    private string[] cols;
    private object[] vals;

    //INSERT INTO t (c1, c2, c3, ...) VALUES (x1, x2, x3, ...);
    
    public InsertQuery Into(string tableName)
    {
        table = tableName;
        return this;
    }
    
    //not necessary to call columns as long as the order of values is known
    public InsertQuery Columns(params string[] columnNames)
    {
        if (columnNames.Length == 0)
        {
            return this;
        }
        cols = columnNames;
        return this;
    }
    
    public InsertQuery Values(params object[] values)
    {
        if (values.Length == 0)
        {
            return this;
        }
        vals = values;
        return this;
    }
    
    public string Build()
    {
        if (table==null || vals==null)
        {
            throw new Exception("A required field is null");
        }
        StringBuilder queryBuilder = new StringBuilder($"INSERT INTO {table} ");
        if (cols != null)
        {
            if (cols.Length != vals.Length)
            {
                throw new Exception("Number of column parameters differed from the number of values");
            }
            queryBuilder.Append('(');
            AppendColumnNames(queryBuilder, cols);
            queryBuilder.Append(") ");
        }

        queryBuilder.Append("VALUES (");
        AppendValues(queryBuilder, vals);
        queryBuilder.Append(')');
        return queryBuilder.ToString();
    }

    private void AppendValues(StringBuilder sb, object[] values)
    {
        for (int i = 0; ; i++)
        {
            if (i == values.Length - 1)
            {
                if (values[i] is string)
                {
                    sb.Append('"');
                    sb.Append(values[i]);
                    sb.Append('"');
                    break;
                }
                sb.Append(values[i]);
                break;
            }
            if (values[i] is string)
            {
                sb.Append('"');
                sb.Append(values[i]);
                sb.Append("\",");
                continue;
            }
            sb.Append(values[i]).Append(',');
        }
    }
    private void AppendColumnNames(StringBuilder sb, string[] cols)
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
}

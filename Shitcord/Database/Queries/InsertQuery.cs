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
        if (columnNames.Length == 0) {
            return this;
        }
        cols = columnNames;
        return this;
    }
    public InsertQuery Columns(params Column[] columns)
    {
        string[] names = new string[columns.Length];
        Column[] colsArr = columns.ToArray();
        for (int i = 0; i < columns.Length; i++) {
            names[i] = colsArr[i].name;
        }
        return Columns(names);
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
            throw new QueryException("A required field is null");
        }
        StringBuilder queryBuilder = new StringBuilder($"INSERT INTO {table} ");
        if (cols != null)
        {
            if (cols.Length != vals.Length)
            {
                throw new QueryException("Number of column parameters differed from the number of values");
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

    private static void AppendValues(StringBuilder builder, object[] values)
    {
        for (int i = 0; ; i++) {
            object val = values[i];
            bool isNull = val == null;
            //handle last element
            if (i == values.Length - 1) {
                if (isNull) {
                    builder.Append("null");
                    break;
                }
                if (val is string lastStr) {
                    builder.Append('\'');
                    AppendString(lastStr, builder);
                    builder.Append('\'');
                    break;
                }
                builder.Append(val);
                break;
            }
            //handle every other element
            if (isNull) {
                builder.Append("null,");
                continue;
            }
            if (val is string str) {
                builder.Append('\'');
                AppendString(str, builder);
                builder.Append("',");
                continue;
            }
            builder.Append(values[i]).Append(',');
        }
    }
    private static void AppendString(string str, StringBuilder queryBuilder) 
    {
        //check if contains single quote/quotes, if it does - modify the string
        StringBuilder modified = ModifyStringForSQL(str);
        queryBuilder.Append(modified);
    }

    private static StringBuilder ModifyStringForSQL(string strToScan)
    {
        StringBuilder sb = new StringBuilder(strToScan);
        for (int i = 0; i < sb.Length; i++) {
            if (sb[i] != '\'')
                continue;
            sb.Insert(i, '\'');
            i++;
        }
        return sb;
    }
    private static void AppendColumnNames(StringBuilder sb, string[] cols)
    {
        for (int i = 0; ; i++) {
            if (i == cols.Length - 1) {
                sb.Append(cols[i]);
                break;
            }
            sb.Append(cols[i]).Append(',');
        }
    }
}

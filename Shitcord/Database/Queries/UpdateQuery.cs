using System.Text;

namespace Shitcord.Database.Queries;

public class UpdateQuery
{
    //UPDATE t SET c1 = x1 WHERE c2 = x2
    private readonly string table;
    private readonly List<(string, object?)> pairs = new();
    private readonly List<(string, int)> incrementPairs = new();
    private Condition? condition;

    public UpdateQuery(string tableName)
    {
        table = tableName;
    }
    
    public UpdateQuery Set(string columnName, object? value)
    {
        pairs.Add((columnName, value));
        return this;
    }
    public UpdateQuery Set(Column column, object? value)
    {
        return Set(column.name, value);
    }
    public UpdateQuery SetIncrementBy(string columnName, int val)
    {
        incrementPairs.Add((columnName, val));
        return this;
    }
    public UpdateQuery SetIncrementBy(Column column, int val)
    {
        return SetIncrementBy(column.name, val);
    }
    public UpdateQuery Where(Condition condition)
    {
        this.condition = condition;
        return this;
    }
    public UpdateQuery WhereEquals(string columnName, object value)
    {
        condition = Condition.New(columnName).Equals(value);
        return this;
    }
    public UpdateQuery WhereEquals(Column column, object value)
    {
        return WhereEquals(column.name, value);
    }
    public string Build()
    {
        int pairsLen = pairs.Count;
        int incrementsLen = incrementPairs.Count;
        if (table==null || (pairsLen<1 && incrementsLen<1)) {
            throw new QueryException("A required field is null");
        }

        StringBuilder queryBuilder = new StringBuilder($"UPDATE {table} SET ");
        for (int i = 0; i < pairsLen; i++){
            AttachPair(queryBuilder, pairs[i]);
            if (i != pairsLen - 1){
                queryBuilder.Append(", ");
            }
        }
        
        if (pairsLen > 0 && incrementsLen > 0)
            queryBuilder.Append(", ");
        
        for (int i = 0; i < incrementsLen; i++){
            AttachIncrementPair(queryBuilder, incrementPairs[i]);
            if (i != incrementsLen - 1){
                queryBuilder.Append(", ");
            }
        }
        //UPDATE does not require a condition
        if (condition == null) {
            return queryBuilder.ToString();
        }
        
        queryBuilder.Append($" WHERE {condition.Get()}");

        return queryBuilder.ToString();
    }

    private void AttachIncrementPair(StringBuilder queryBuilder, (string, int) p)
    {
        queryBuilder.Append(p.Item1).Append(" = ").Append(p.Item1);
        queryBuilder.Append(p.Item2 < 0 ? '-' : '+');
        queryBuilder.Append(Math.Abs(p.Item2));
    }

    //Item1 - columnName, Item2 - value
    private void AttachPair(StringBuilder queryBuilder, (string, object?) p)
    {
        queryBuilder.Append(p.Item1).Append(" = ");
        AppendValue(p.Item2, queryBuilder);
    }
    private void AppendValue(object? value, StringBuilder queryBuilder)
    {
        if (value is string str) {
            //check if contains single quote, if it does - modify the string
            str = ModifyStringForSQL(str);
            queryBuilder.Append($"'{str}'");
        }
        else {
            queryBuilder.Append(value);
        }
    }
    private string ModifyStringForSQL(string strToScan)
    {
        StringBuilder sb = new StringBuilder(strToScan);
        for (int i = 0; i < sb.Length; i++) {
            if (sb[i] != '\'')
                continue;
            sb.Insert(i, '\'');
            i++;
        }
        return sb.ToString();
    }
}

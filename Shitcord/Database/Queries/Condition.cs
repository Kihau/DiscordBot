using System.Text;

namespace Shitcord.Database.Queries;

public class Condition
{
    private bool operatorExpected = true;
    private readonly StringBuilder condition = new ();

    private Condition(string colName)
    {
        condition.Append(colName).Append(' ');
    }

    public static Condition New(string columnName)
    {
        return new Condition(columnName);
    }
    public static Condition New(Column column)
    {
        return New(column.name);
    }
    public Condition And(string columnName)
    {
        if (operatorExpected) {
            throw new QueryException("Syntax error");
        }
        operatorExpected = true;
        condition.Append("AND").Append(' ').Append(columnName).Append(' ');
        return this;
    }
    public Condition And(Column column)
    {
        return And(column.name);
    }
    public Condition Or(string columnName)
    {
        if (operatorExpected) {
            throw new QueryException("Syntax error");
        }
        operatorExpected = true;
        condition.Append("OR").Append(' ').Append(columnName).Append(' ');
        return this;
    }
    public Condition Or(Column column)
    {
        return Or(column.name);
    }
    //hides the method from the object class
    public new Condition Equals(object? value)
    {
        if (!operatorExpected) {
            throw new QueryException("Syntax error");
        }
        operatorExpected = false;
        if (value is null) {
            condition.Append("IS NULL ");
            return this;
        }
        condition.Append('=').Append(' ');
        AppendValue(value);
        condition.Append(' ');
        return this;
    }
    public Condition IsDiffFrom(object? value)
    {
        if (!operatorExpected) {
            throw new QueryException("Syntax error");
        }
        operatorExpected = false;
        if (value is null) {
            condition.Append("IS NOT NULL ");
            return this;
        }
        condition.Append("<>").Append(' ');
        AppendValue(value);
        condition.Append(' ');
        return this;
    }
    
    public Condition IsLike(string pattern)
    {
        if (!operatorExpected) {
            throw new QueryException("Syntax error");
        }
        operatorExpected = false;
        condition.Append("LIKE").Append(' ');
        AppendValue(pattern);
        condition.Append(' ');
        return this;
    }

    public Condition IsLessThan(object value)
    {
        if (!operatorExpected) {
            throw new QueryException("Syntax error");
        }
        operatorExpected = false;
        condition.Append('<').Append(' ');
        AppendValue(value);
        condition.Append(' ');
        return this;
    }
    public Condition IsMoreThan(object value)
    {
        if (!operatorExpected) {
            throw new QueryException("Syntax error");
        }
        operatorExpected = false;
        condition.Append('>').Append(' ');
        AppendValue(value);
        condition.Append(' ');
        return this;
    }
    private void AppendValue(object value)
    {
        if (value is string str) {
            //check if contains single quote, if it does - modify the string
            StringBuilder modified = ModifyStringForSQL(str);
            condition.Append('\'').Append(modified).Append('\'');
        }
        else {
            condition.Append(value);
        }
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

    public String Get()
    {
        if (operatorExpected) {
            throw new QueryException("Condition is incomplete");
        }
        return condition.ToString();
    }
}

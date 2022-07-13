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
    public Condition And(string columnName)
    {
        if (operatorExpected)
        {
            throw new Exception("Syntax error");
        }
        operatorExpected = true;
        condition.Append("AND").Append(' ').Append(columnName).Append(' ');
        return this;
    }
    public Condition Or(string columnName)
    {
        if (operatorExpected)
        {
            throw new Exception("Syntax error");
        }
        operatorExpected = true;
        condition.Append("OR").Append(' ').Append(columnName).Append(' ');
        return this;
    }
    //hides the method from the object class
    public new Condition Equals(object value)
    {
        if (!operatorExpected)
        {
            throw new Exception("Syntax error");
        }
        operatorExpected = false;
        condition.Append('=').Append(' ');
        AppendValue(value);
        condition.Append(' ');
        return this;
    }
    
    public Condition IsLike(string pattern)
    {
        if (!operatorExpected)
        {
            throw new Exception("Syntax error");
        }
        operatorExpected = false;
        condition.Append("LIKE").Append(' ');
        AppendValue(pattern);
        condition.Append(' ');
        return this;
    }

    public Condition IsLessThan(object value)
    {
        if (!operatorExpected)
        {
            throw new Exception("Syntax error");
        }
        operatorExpected = false;
        condition.Append('<').Append(' ');
        AppendValue(value);
        condition.Append(' ');
        return this;
    }
    public Condition IsMoreThan(object value)
    {
        if (!operatorExpected)
        {
            throw new Exception("Syntax error");
        }
        operatorExpected = false;
        condition.Append('>').Append(' ');
        AppendValue(value);
        condition.Append(' ');
        return this;
    }
    private void AppendValue(object value)
    {
        if (value is string)
        {
            condition.Append($"\"{value}\"");
        }
        else
        {
            condition.Append(value);
        }
    }
    public String Get()
    {
        if (operatorExpected)
        {
            throw new Exception("Condition is incomplete");
        }
        return condition.ToString();
    }
}

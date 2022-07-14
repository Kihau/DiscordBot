using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Tests;

class QueryTests
{
    private static int tests = 0;
    public static void runTests()
    {
        selectTests();
        insertTests();
        updateTests();
        conditionTests();
    }

    private static void selectTests()
    {
        const string expected1 = "SELECT name,lastname,another FROM markov WHERE chain_data_id = 23";
        string select1 = QueryBuilder.New()
            .Retrieve("name", "lastname", "another").From("markov")
            .Where(Condition.New("chain_data_id").Equals(23))
            .Build();
        
        const string expected2 = "SELECT * FROM markov WHERE id = 11";
        string select2 = QueryBuilder.New()
            .Retrieve("*").From("markov")
            .Where(Condition.New("id").Equals(11))
            .Build();
        
        const string expected3 = "SELECT field1,field2 FROM markov WHERE field3 LIKE \"N%\"";
        string select3 = QueryBuilder.New()
            .Retrieve("field1", "field2").From("markov")
            .Where(Condition.New("field3").IsLike("N%"))
            .Build();
        
        const string expectedOrderBy = "SELECT field1 FROM t WHERE c LIKE \"[d-f]%\" ORDER BY chain_str DESC";
        string selectOrderBy = QueryBuilder.New()
            .Retrieve("field1").From("t")
            .Where(Condition.New("c").IsLike("[d-f]%"))
            .OrderBy("chain_str", false)
            .Build();
        
        Console.WriteLine("Selects:");
        compareStartsWith(select1, expected1);
        compareStartsWith(select2, expected2);
        compareStartsWith(select3, expected3);
        compareStartsWith(selectOrderBy, expectedOrderBy);
    }

    static void conditionTests()
    {
        const string expected1 = "student_name = \"Jack\" AND age > 18";
        string condition1 = Condition.New("student_name").Equals("Jack").And("age").IsMoreThan(18).Get();

        const string expected2 = "lastname LIKE \"%ierce\" AND age > 18 AND name = \"Bruh\" OR major LIKE \"Math%\"";
        string condition2 = Condition.New("lastname").IsLike("%ierce")
            .And("age").IsMoreThan(18)
            .And("name").Equals("Bruh")
            .Or("major").IsLike("Math%")
            .Get();
        
        Console.WriteLine("Conditions:");
        compareStartsWith(condition1, expected1);
        compareStartsWith(condition2, expected2);
    }
    static void insertTests()
    {
        const string expected1 = "INSERT INTO MyTable (build,an,dwq) VALUES (2,\"bur\",True)";
        string insert1 = QueryBuilder.New().Insert().Into("MyTable").Values(2, "bur", true).Columns("build", "an", "dwq").Build();
        const string expected2 = "INSERT INTO markov_data VALUES (\"i\",\"dont\",7)";
        string insert2 = QueryBuilder.New().Insert().Into(MarkovTable.TABLE_NAME).Values("i", "dont", 7).Build();
        const string expected3 = "INSERT INTO markov_data VALUES (\"on\",null,2)";
        string insert3 = QueryBuilder.New().Insert().Into(MarkovTable.TABLE_NAME).Values("on", null, 2).Build();
        const string expected4 = "INSERT INTO markov_data VALUES (\"on\",\"zzz\",null)";
        string insert4 = QueryBuilder.New().Insert().Into(MarkovTable.TABLE_NAME).Values("on", "zzz", null).Build();

        Console.WriteLine("Inserts:");
        compareStartsWith(insert1, expected1);
        compareStartsWith(insert2, expected2);
        compareStartsWith(insert3, expected3);
        compareStartsWith(insert4, expected4);
    }

    static void updateTests()
    {
        const string expected = "UPDATE random_table SET name = \"bis\" WHERE id < 234";
        string insert1 = QueryBuilder.New().Update("random_table")
            .Set("name", "bis")
            .Where(Condition.New("id").IsLessThan(234))
            .Build();
        
        Console.WriteLine("Updates:");
        compareStartsWith(insert1, expected);
    }
    private static void compareStartsWith(string given, string expected)
    {
        bool compare = given.StartsWith(expected);
        if (compare)
            Console.WriteLine($"#{tests++} Passed");
        else
        {
            Console.WriteLine($"#{tests++} Failed");
            Console.WriteLine("Given: " + given);
            Console.WriteLine("Expected: " + expected);
        }
    }
}

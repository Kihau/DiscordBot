using System.Text;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Tests;

class QueryTests
{
    private static int tests = 0;
    private static int passed;

    public static void runTests()
    {
        selectTests();
        insertTests();
        updateTests();
        conditionTests();
        escapeCharactersTests();
        Console.WriteLine($"Ratio (Passed/ALL): {passed}/{tests}");
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
        
        const string expected3 = "SELECT field1,field2 FROM markov WHERE field3 LIKE 'N%'";
        string select3 = QueryBuilder.New()
            .Retrieve("field1", "field2").From("markov")
            .Where(Condition.New("field3").IsLike("N%"))
            .Build();
        
        const string expectedOrderBy = "SELECT field1 FROM t WHERE c LIKE '[d-f]%' ORDER BY chain_str DESC";
        string selectOrderBy = QueryBuilder.New()
            .Retrieve("field1").From("t")
            .Where(Condition.New("c").IsLike("[d-f]%"))
            .OrderBy("chain_str", false)
            .Build();
        
        const string expectedDistinct1 = "SELECT DISTINCT field FROM table";
        string selectDistinct1 = QueryBuilder.New()
            .Retrieve("field").Distinct()
            .From("table")
            .Build();
        
        const string expectedDistinct2 = "SELECT DISTINCT f1,f2,f3 FROM table";
        string selectDistinct2 = QueryBuilder.New()
            .Retrieve("f1","f2","f3" ).Distinct()
            .From("table")
            .Build();
        
        const string expectedDistinct3Column = "SELECT DISTINCT base_str,chain_str,frequency FROM table";
        string selectDistinct3Column = QueryBuilder.New()
            .Retrieve(MarkovTable.BASE, MarkovTable.CHAIN, MarkovTable.FREQUENCY).Distinct()
            .From("table")
            .Build();
        
        const string expectedOneParam = "SELECT chain_str FROM table";
        string givenOneParam = QueryBuilder.New()
            .Retrieve(MarkovTable.CHAIN)
            .From("table")
            .Build();
        
        const string expectedWhereEquals = "SELECT base_str FROM table WHERE frequency = 3";
        string whereEqualsCol1 = QueryBuilder.New()
            .Retrieve(MarkovTable.BASE)
            .From("table")
            .WhereEquals(MarkovTable.FREQUENCY, 3)
            .Build();
        string whereEqualsCol2 = QueryBuilder.New()
            .Retrieve(MarkovTable.BASE)
            .From("table")
            .WhereEquals(MarkovTable.FREQUENCY.name, 3)
            .Build();
        
        const string expectedRandom1 = "SELECT * FROM table ORDER BY RAND() LIMIT 1";
        string random1 = QueryBuilder.New()
            .Retrieve("*").From("table")
            .Random().Limit()
            .Build();
        
        const string expectedRandom2 = "SELECT * FROM table ORDER BY RAND() LIMIT 3";
        string random2 = QueryBuilder.New()
            .Retrieve("*").From("table")
            .Random().Limit(3)
            .Build();
        
        const string expectedLimit1 = "SELECT * FROM table LIMIT 55";
        string limit1 = QueryBuilder.New()
            .Retrieve("*").From("table")
            .Limit(55)
            .Build();
        
        const string expectedLimit2 = "SELECT base_str,chain_str,frequency FROM table WHERE chain_str = 'chunky' LIMIT 0";
        string limit2 = QueryBuilder.New()
            .Retrieve(MarkovTable.COLUMNS.ToArray()).From("table")
            .WhereEquals(MarkovTable.CHAIN, "chunky")
            .Limit(0)
            .Build();
        
        
        Console.WriteLine("Selects:");
        compareStartsWith(select1, expected1);
        compareStartsWith(select2, expected2);
        compareStartsWith(select3, expected3);
        compareStartsWith(selectOrderBy, expectedOrderBy);
        compareStartsWith(selectDistinct1, expectedDistinct1);
        compareStartsWith(selectDistinct2, expectedDistinct2);
        compareStartsWith(selectDistinct3Column, expectedDistinct3Column);
        compareStartsWith(givenOneParam, expectedOneParam);
        compareStartsWith(whereEqualsCol1, expectedWhereEquals);
        compareStartsWith(whereEqualsCol1, whereEqualsCol2);
        compareStartsWith(random1, expectedRandom1);
        compareStartsWith(random2, expectedRandom2);
        compareStartsWith(limit1, expectedLimit1);
        compareStartsWith(limit2, expectedLimit2);
    }

    static void conditionTests()
    {
        const string expected1 = "student_name = 'Jack' AND age > 18";
        string condition1 = Condition.New("student_name").Equals("Jack").And("age").IsMoreThan(18).Get();

        const string expected2 = "lastname LIKE '%ierce' AND age > 18 AND name = 'Bruh' OR major LIKE 'Math%'";
        string condition2 = Condition.New("lastname").IsLike("%ierce")
            .And("age").IsMoreThan(18)
            .And("name").Equals("Bruh")
            .Or("major").IsLike("Math%")
            .Get();
        
        const string expected3 = "customer_name <> 'Alice' AND customer_name LIKE '%lice'";
        string condition3 = Condition.New("customer_name").IsDiffFrom("Alice")
            .And("customer_name").IsLike("%lice")
            .Get();
        
        const string expected4 = "targetNumber <> 55 AND targetNumber > '51'";
        string condition4 = Condition.New("targetNumber").IsDiffFrom(55)
            .And("targetNumber").IsMoreThan("51")
            .Get();
        
        Console.WriteLine("Conditions:");
        compareStartsWith(condition1, expected1);
        compareStartsWith(condition2, expected2);
        compareStartsWith(condition3, expected3);
        compareStartsWith(condition4, expected4);
    }
    static void insertTests()
    {
        const string expected1 = "INSERT INTO MyTable (build,an,dwq) VALUES (2,'bur',True)";
        string insert1 = QueryBuilder.New().Insert().Into("MyTable").Values(2, "bur", true).Columns("build", "an", "dwq").Build();
        const string expected2 = "INSERT INTO markov_data VALUES ('i','dont',7)";
        string insert2 = QueryBuilder.New().Insert().Into(MarkovTable.TABLE_NAME).Values("i", "dont", 7).Build();
        const string expected3 = "INSERT INTO markov_data VALUES ('on',null,2)";
        string insert3 = QueryBuilder.New().Insert().Into(MarkovTable.TABLE_NAME).Values("on", null, 2).Build();
        const string expected4 = "INSERT INTO markov_data VALUES ('on','zzz',null)";
        string insert4 = QueryBuilder.New().Insert().Into(MarkovTable.TABLE_NAME).Values("on", "zzz", null).Build();
        
        const string expected5Cols = 
            "INSERT INTO markov_data (base_str,chain_str,frequency) VALUES ('one','two','3')";
        string insert5Cols = QueryBuilder.New()
            .Insert().Into(MarkovTable.TABLE_NAME)
            .Columns(MarkovTable.COLUMNS.ToArray())
            .Values("one", "two", "3").Build();

        Console.WriteLine("Inserts:");
        compareStartsWith(insert1, expected1);
        compareStartsWith(insert2, expected2);
        compareStartsWith(insert3, expected3);
        compareStartsWith(insert4, expected4);
        compareStartsWith(insert5Cols, expected5Cols);
    }

    static void updateTests()
    {
        const string expected1 = "UPDATE random_table SET name = 'bis' WHERE id < 234";
        string update1 = QueryBuilder.New().Update("random_table")
            .Set("name", "bis")
            .Where(Condition.New("id").IsLessThan(234))
            .Build();

        const string expected2 = "UPDATE any_table SET number = 23, info = 'warning' WHERE smth > 444";
        string update2 = QueryBuilder.New().Update("any_table")
            .Set("number", 23)
            .Set("info", "warning")
            .Where(Condition.New("smth").IsMoreThan(444))
            .Build();
        
        const string expectedSetByIncrement1 = "UPDATE any_table SET number = 23, info = 'warning', occurrence = occurrence+3 WHERE s > 0";
        string setByIncrement1 = QueryBuilder.New().Update("any_table")
            .Set("number", 23)
            .Set("info", "warning")
            .SetIncrementBy("occurrence", 3)
            .Where(Condition.New("s").IsMoreThan(0))
            .Build();
        const string expectedSetByIncrement2 = "UPDATE any_table SET a = 1, x = x-1 WHERE s > 0 AND z LIKE '%d'";
        string setByIncrement2 = QueryBuilder.New().Update("any_table")
            .Set("a", 1)
            .SetIncrementBy("x", -1)
            .Where(Condition.New("s").IsMoreThan(0).And("z").IsLike("%d"))
            .Build();
        const string expectedBunchOfIncrements = "UPDATE any_table SET name = 'John', x = x-1, y = y+1";
        string bunchOfIncrements = QueryBuilder.New().Update("any_table")
            .SetIncrementBy("x", -1)
            .Set("name", "John")
            .SetIncrementBy("y", +1)
            .Build();
        const string expectedSingleIncrement = "UPDATE aTable SET freq = freq+1 WHERE name = 'Charlie'";
        string singleIncrement = QueryBuilder.New().Update("aTable")
            .SetIncrementBy("freq", 1)
            .WhereEquals("name", "Charlie")
            .Build();
        const string expectedTwoIncrements = "UPDATE aTable SET f = f+2, g = g-3 WHERE f > 4";
        string twoIncrements = QueryBuilder.New().Update("aTable")
            .Where(Condition.New("f").IsMoreThan(4))
            .SetIncrementBy("f", 2)
            .SetIncrementBy("g", -3)
            .Build();

        Console.WriteLine("Updates:");
        compareStartsWith(update1, expected1);
        compareStartsWith(update2, expected2);
        compareStartsWith(setByIncrement1, expectedSetByIncrement1);
        compareStartsWith(setByIncrement2, expectedSetByIncrement2);
        compareStartsWith(bunchOfIncrements, expectedBunchOfIncrements);
        compareStartsWith(singleIncrement, expectedSingleIncrement);
        compareStartsWith(twoIncrements, expectedTwoIncrements);
    }
    
    private static void escapeCharactersTests()
    {
        const string expectedEscape1 = "SELECT any_col FROM any_table WHERE name = '\"escape_me\"'";
        string escape1 = QueryBuilder.New().Retrieve("any_col")
            .From("any_table")
            .WhereEquals("name", "\"escape_me\"")
            .Build();
        compareStartsWith(escape1, expectedEscape1);
    }
    public static void stringBuilderTest()
    {
        const string input1 = "pos'tgre''d han' ''dle i''t bet''ter";
        StringBuilder sb = new StringBuilder(input1);
        for (int i = 0; i < sb.Length; i++) {
            if (sb[i] != '\'')
                continue;
            
            sb.Insert(i, '\'');
            i++;
        }

        const string expectedResult = "pos''tgre''''d han'' ''''dle i''''t bet''''ter";
        if (expectedResult.Equals(sb.ToString())) {
            Console.WriteLine("Passed insert test");
        }
        else {
            Console.WriteLine("Failed");
            Console.WriteLine($"expected: {expectedResult}  result: {sb}");
        }
    }
    private static void compareStartsWith(string given, string expected)
    {
        bool compare = given.StartsWith(expected);
        if (compare) {
            passed++;
            Console.WriteLine($"#{tests++} Passed");
        }
        else
        {
            Console.WriteLine($"#{tests++} Failed");
            Console.WriteLine("Given: " + given);
            Console.WriteLine("Expected: " + expected);
        }
    }
}

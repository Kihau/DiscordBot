using static Shitcord.Extensions.SeekstampArgumentConverter;
namespace Shitcord.Tests;

public class ConvertTest
{
    private static int passedTests;
    private static int failedTests;
    
    public static void tests()
    {
        Console.WriteLine("===============================");
        testValid(62, "1m 2s");
        testValid(62, "1 m 2 s");
        testValid(62, "1m 2s");
        testValid(3600, "1h");
        testValid(79260, "1m 22h");
        testValid(5542, "92m 22s");
        testValid(5, "2s3s");
        testValid(10802, "2s3h");
        testValid(10802, "2 s3h");
        testValid(10802, "2 s3 h");
        testValid(10802, "     2      s3 h     ");
        testValid(259207, "2d3s4s1d");

        testInvalid("2 3 h");
        testInvalid("2 3h");
        testInvalid("h2");
        testInvalid("23h 3");
        testInvalid("23h 3sz");
        testInvalid("mh");
        testInvalid("3       3h");
        testInvalid("999999999d");
        testInvalid("1ms ");
        testInvalid("sick ");
        testInvalid("1m 3s s");
        
        Console.Write($"Passed/All: {passedTests}/{failedTests+passedTests}");
    }

    private static void testInvalid(string s)
    {
        try {
            int seconds = tryParseSuffixUnit(s, 0);
            failedTests++;
            Console.WriteLine($"Exception wasn't thrown for: {s}; Got: {seconds}");
        }catch {
            passedTests++;
        }
    }

    private static void testValid(int expected, string time)
    {
        int actual = tryParseSuffixUnit(time, 0);
        if (expected == actual) {
            passedTests++;
            return;
        }

        failedTests++;
        Console.WriteLine($"Failed for: {time}; Expected: {expected}; Actual: {actual}");
    }
}

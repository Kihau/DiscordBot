using static Shitcord.Extensions.SeekstampArgumentConverter;
namespace Shitcord.Tests;

public class ConvertTest
{
    private static int passedTests;
    private static int failedTests;

    public static void separatorTests()
    {
        Console.WriteLine("===============================");
        testValidSep(3723, "1:2:3");
        testValidSep(43200, "12:0:00");
        testValidSep(59, "00:00:59");
        testValidSep(0, "0:0:0");
        testValidSep(0, "0.0.0");
        testValidSep(0, "0.00.0");
        testValidSep(0, "00.0.00");
        testValidSep(0, "00:00:00");
        testValidSep(59, "0:0:59");
        testValidSep(60, "0:1:0");
        testValidSep(667, "0:11:7");
        testValidSep(667, "0.11.7");
        testValidSep(667, "0:11:07");
        testValidSep(84163, "23:22:43");
        testValidSep(84163, "23.22.43");
        testValidSep(256963, "2:23:22:43");
        testValidSep(256963, "2.23.22.43");
        testValidSep(32, "32");
        testValidSep(0, "0");
        testValidSep(92, "1:32");
        testInvalidSep("0:0:000");
        testInvalidSep("59:24:0");
        testInvalidSep("0 :0:000");
        testInvalidSep("0 :2 ");
        testInvalidSep("0:000:0");
        testInvalidSep("00:000:0");
        testInvalidSep("000:0:0");
        testInvalidSep("0:0:60");
        testInvalidSep("0:899:00");
        testInvalidSep("00:89:00");
        testInvalidSep("00=89200");
        testInvalidSep("00=89200");
        Console.Write($"[Separator Tests] Passed/All: {passedTests}/{failedTests+passedTests}");
    }
    

    public static void suffixTests()
    {
        Console.WriteLine("===============================");
        testValidSuffix(62, "1m 2s");
        testValidSuffix(62, "1 m 2 s");
        testValidSuffix(62, "1m 2s");
        testValidSuffix(3600, "1h");
        testValidSuffix(79260, "1m 22h");
        testValidSuffix(5542, "92m 22s");
        testValidSuffix(5, "2s3s");
        testValidSuffix(10802, "2s3h");
        testValidSuffix(10802, "2 s3h");
        testValidSuffix(10802, "2 s3 h");
        testValidSuffix(10802, "     2      s3 h     ");
        testValidSuffix(259207, "2d3s4s1d");

        testInvalidSuffix("2 3 h");
        testInvalidSuffix("2 3h");
        testInvalidSuffix("h2");
        testInvalidSuffix("23h 3");
        testInvalidSuffix("23h 3sz");
        testInvalidSuffix("mh");
        testInvalidSuffix("3       3h");
        testInvalidSuffix("999999999d");
        testInvalidSuffix("1ms ");
        testInvalidSuffix("sick ");
        testInvalidSuffix("1m 3s s");
        
        Console.Write($"[Unit Tests] Passed/All: {passedTests}/{failedTests+passedTests}");
    }

    private static void testInvalidSuffix(string s)
    {
        try {
            int seconds = tryParseSuffixUnit(s, 0);
            failedTests++;
            Console.WriteLine($"Exception wasn't thrown for: {s}; Got: {seconds}");
        }catch {
            passedTests++;
        }
    }
    private static void testInvalidSep(string s)
    {
        try {
            int seconds = tryParseSeparatorToken(s, 0);
            failedTests++;
            Console.WriteLine($"Exception wasn't thrown for: {s}; Got: {seconds}");
        }catch {
            passedTests++;
        }
    }
    private static void testValidSep(int expectedSeconds, string time)
    {
        int actual = -1;
        try {
            actual = tryParseSeparatorToken(time, 0);
        }catch {
            failedTests++;
            Console.WriteLine($"Exception thrown for: {time}");
            return;
        }
        
        if (expectedSeconds == actual) {
            passedTests++;
            return;
        }

        failedTests++;
        Console.WriteLine($"Failed for: {time}; Expected: {expectedSeconds}; Actual: {actual}");
    }

    private static void testValidSuffix(int expectedSeconds, string time)
    {
        int actual = tryParseSuffixUnit(time, 0);
        if (expectedSeconds == actual) {
            passedTests++;
            return;
        }

        failedTests++;
        Console.WriteLine($"Failed for: {time}; Expected: {expectedSeconds}; Actual: {actual}");
    }
}

using Shitcord.Extensions;

namespace Shitcord.Tests;

public class MatchingTest{
    private static int passedTests, failedTests;
    public const float TOLERANCE = 0.05f;
    public static void RunTests(){
        //TODO: base larger tests on accuracy rather than matching len
        test("lyrics", "lyics", 1f);
        test("lil yachty", "lil yachty", 1);
        test("lil yachty", "lil yachty russia", 0.6f);
        test("dekstop", "desktop", 0.85f);
        test("beeee gees", "beeee", 1f);
        test("gees", "skeler", 0); // 4/5
        test("coat", "cost", 0.75f);
        test("post", "post", 1);
        test("cot", "coat", 0.75f);
        test("coat", "cot", 1);
        test("bee gees", "skeler", 0);
        test("bee gees", "beeeez G", 0.42f);
        test("recur", "owner", 0f);
        test("q", "something", 0);
        test("Lil Yachty german", "lil yachty german", 1);
        test("lil yachty german", "lil yachty german", 1);
        test("lil yachty poland", "Lil Yachty - Poland", 0.93f);
        test("lil yachty poland", "?lil yachty poland freestyle by?MAJ", 0.451f);
        Console.WriteLine($"Passed/All  {passedTests}/{passedTests+failedTests}");
    }
    private static void test(string query, string target, float expected){
        float accuracy = StringMatching.Accuracy(query, target);
        if (Math.Abs(accuracy - expected) < TOLERANCE){
            passedTests++;
        }else{
            failedTests++;
            Console.WriteLine($"Expected: {expected}, Got:{accuracy}");
            Console.WriteLine($"For: {query} | {target}");
        }
    }
}
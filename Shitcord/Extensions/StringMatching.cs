
namespace Shitcord.Extensions;

public class StringMatching
{
    private const int MIN_LEN_THRESHOLD = 3;
    // returns 0.0 - 1.0 value representing what percentage of target string was matched
    public static float Accuracy(string name, string target)
    {
        if (name.Length == 0 || target.Length == 0) {
            return 0;
        }

        int accuracy = 0;
        string[] names = name.ToLower().Split(" ");
        string[] targets = target.ToLower().Split(" ");
        foreach (var q in names){
            foreach (var t in targets){
                int tmp = matchingLen(q.ToLower(), t.ToLower());
                if (tmp >= MIN_LEN_THRESHOLD){
                    accuracy += tmp;
                }

                if (tmp >= q.Length){
                    break;
                }
            }
        }

        int targetNonSpaceLen = target.Count(chr => chr != ' ');
        return Math.Min((float)accuracy / targetNonSpaceLen, 1);
    }

    public static int matchingLen(string str1, string str2)
    {
        int score = 0;
        for (int i = 0, j = 0; i < str1.Length && j < str2.Length; i++, j++) {
            char chr1 = str1[i];
            char chr2 = str2[j];
            if (chr1 == chr2) {
                score++;
            } else {
                if (j + 1 < str2.Length && chr1 == str2[j + 1]) {
                    // cot -> coat (missed letter)
                    score++;
                    j++;
                } else if (i + 1 < str1.Length && str1[i + 1] == chr2) {
                    // coat -> cot (additional letter)
                    score++;
                    i++;
                }
            }
            //coat -> cost
        }
        return score;
    }
}

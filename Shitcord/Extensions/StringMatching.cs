using System.Reflection.Metadata.Ecma335;

namespace Shitcord.Extensions;

public class StringMatching
{
    private const int MIN_LEN_THRESHOLD = 2;
    // larger value = better matching
    public static int Accuracy(string name, string target)
    {
        if (name.Length == 0 || target.Length == 0) {
            return 0;
        }

        int accuracy = 0;
        string[] names = name.ToLower().Split(" ");
        string[] targets = target.ToLower().Split(" ");
        foreach (string q in names) {
            foreach (string t in targets) {
                int tmp = matchingLen(q, t);
                if (tmp >= MIN_LEN_THRESHOLD) {
                    accuracy += tmp;
                }
            }
        }
        //int effectiveLength = names.Sum(q => q.Length);
        return Math.Min(accuracy, target.Length);
    }

    public static int matchingLen(string str1, string str2)
    {
        int score = 0;
        int minLen = Math.Min(str1.Length, str2.Length);
        for (int i = 0, j = 0; i < minLen && j < minLen; i++, j++) {
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

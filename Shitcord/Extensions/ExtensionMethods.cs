using System.Net;

namespace Shitcord.Extensions;

public static class ExtensionMethods
{
    public static long UlongToLong(ulong number) 
        => unchecked((long)number + Int64.MinValue);

    public static ulong LongToUlong(long number) 
        => unchecked((ulong)(number - Int64.MinValue));

    public static long? UlongToLong(ulong? number) 
        => number is null ? null : unchecked((long)number + Int64.MinValue);

    public static ulong? LongToUlong(long? number) 
        => number is null ? null : unchecked((ulong)(number - Int64.MinValue));

    public static bool WebConnectionOk(string url)
    {
        try {
            using var client = new HttpClient();
            var res = client.GetAsync(url);
            return res.Result.StatusCode == HttpStatusCode.OK;
        } catch {
            return false;
        }
    }
}

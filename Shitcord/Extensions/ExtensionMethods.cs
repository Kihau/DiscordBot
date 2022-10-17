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

    public static string HumanizeSpan(TimeSpan span) {
        var d = span.Days > 0 ? span.Days + "d " : "";
        var h = span.Hours > 0 ? span.Hours + "h " : "";
        var m = span.Minutes > 0 ? span.Minutes + "m " : "";
        return $"{d}{h}{m}{span.Seconds}s";
    }

    public static TimeSpan StripMilliseconds(this TimeSpan time)
        => new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
}

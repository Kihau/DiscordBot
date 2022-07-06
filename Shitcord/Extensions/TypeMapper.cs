namespace Shitcord.Extensions;

public static class TypeMapper
{
    public static long UlongToLong(ulong number) 
        => unchecked((long)number + Int64.MinValue);

    public static ulong LongToUlong(long number) 
        => unchecked((ulong)(number - Int64.MinValue));

    public static long? UlongToLong(ulong? number) 
        => number is null ? null : unchecked((long)number + Int64.MinValue);

    public static ulong? LongToUlong(long? number) 
        => number is null ? null : unchecked((ulong)(number - Int64.MinValue));
}

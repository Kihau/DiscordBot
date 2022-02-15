namespace Shitcord.Data;

public static class StaticData
{
    public static readonly List<ulong> AdminIds = new()
    {
        278778540554715137,
        790507097615237120,
        489788192145539072
    };

#if  DEBUG
    public static bool DebugEnabled { get; set; } = true;
#else
    public static bool DebugEnabled { get; set; } = false;
#endif
}
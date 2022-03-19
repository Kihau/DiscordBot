namespace Shitcord.Data;

// TODO:
public static class GlobalConfig
{
	public static bool LogMessages { get; set; } = false;	
	public static bool LogCommands { get; set; } = false;
	public static bool EnableAllExceptions { get; set; } = false;
	
#if DEBUG
	public static bool EnableTestingModule { get; set; } = false;
#endif
}
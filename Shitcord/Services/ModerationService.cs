namespace Shitcord.Services;

public class ModerationService
{
    public Dictionary<ulong, List<string>> GuildEditData { get; set; } = new();
    public Dictionary<ulong, List<string>> GuildDeleteData { get; set; } = new();

    public ModerationService() {}

    // TODO: store deleted and edited messages
}

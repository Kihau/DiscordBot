using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Shitcord.Services;

public class ModerationService
{
    DiscordClient Client;

    public const int CACHE_SIZE = 10;
    public Dictionary<ulong, List<(string, string)>> GuildEditData { get; set; } = new();
    public Dictionary<ulong, List<(string, string)>> GuildDeleteData { get; set; } = new();

    public ModerationService(DiscordClient client) 
    {
        Client = client;
        Client.MessageDeleted += MessageDeletedHandler; 
        Client.MessageUpdated += MessageEditedHandler;
    }

    private Task MessageDeletedHandler(DiscordClient client, MessageDeleteEventArgs e)
    {
        var data = GetOrAddDeleteData(e.Guild);
        data.Add((e.Message.Author.Username, e.Message.Content));

        if (data.Count > CACHE_SIZE)
            data.RemoveAt(0);

        return Task.CompletedTask;
    }

    public List<(string, string)> GetOrAddDeleteData(DiscordGuild guild)
    {
        if (GuildDeleteData.TryGetValue(guild.Id, out var data))
            return data;

        data = new List<(string, string)>();
        GuildEditData.Add(guild.Id, data);

        return data;
    }

    private Task MessageEditedHandler(DiscordClient client, MessageUpdateEventArgs e)
    {
        var data = GetOrAddEditData(e.Guild);
        data.Add((e.Message.Author.Username, e.Message.Content));

        if (data.Count > CACHE_SIZE)
            data.RemoveAt(0);

        return Task.CompletedTask;
    }

    public List<(string, string)> GetOrAddEditData(DiscordGuild guild)
    {
        // TODO: Check wether deleted message is also on edited messages list
        // (if yes - delete it from the list)
        if (GuildEditData.TryGetValue(guild.Id, out var data))
            return data;

        data = new List<(string, string)>();
        GuildEditData.Add(guild.Id, data);

        return data;
    }
}

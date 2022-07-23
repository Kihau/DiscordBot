using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Shitcord.Services;

public class ModerationService
{
    DiscordClient Client;

    public const int CACHE_SIZE = 10;

    public Dictionary<ulong, List<(DiscordMessage, string)>> GuildEditData { get; set; } = new();
    public Dictionary<ulong, List<DiscordMessage>> GuildDeleteData { get; set; } = new();

    public ModerationService(DiscordBot bot) 
    {
        Client = bot.Client;
        Client.MessageDeleted += MessageDeletedHandler; 
        Client.MessageUpdated += MessageEditedHandler;
    }

    private Task MessageDeletedHandler(DiscordClient client, MessageDeleteEventArgs e)
    {
        if (e.Message is null || e.Message.Author.IsBot)
            return Task.CompletedTask;
        
        var data = GetOrAddDeleteData(e.Guild);
        data.Add(e.Message);

        if (data.Count > CACHE_SIZE)
            data.RemoveAt(0);

        return Task.CompletedTask;
    }

    public List<DiscordMessage> GetOrAddDeleteData(DiscordGuild guild)
    {
        if (GuildDeleteData.TryGetValue(guild.Id, out var data)) {
            data.RemoveAll(msg => DateTime.Now - msg.CreationTimestamp > TimeSpan.FromHours(1));
            return data;
        }

        data = new List<DiscordMessage>();
        GuildDeleteData.Add(guild.Id, data);

        return data;
    }

    private Task MessageEditedHandler(DiscordClient client, MessageUpdateEventArgs e)
    {
        if (e.MessageBefore is null || e.Author.IsBot)
            return Task.CompletedTask;
        
        var data = GetOrAddEditData(e.Guild);
        
        data.Add((e.Message, e.MessageBefore.Content));

        if (data.Count > CACHE_SIZE)
            data.RemoveAt(0);

        return Task.CompletedTask;
    }

    public List<(DiscordMessage, string)> GetOrAddEditData(DiscordGuild guild)
    {
        if (GuildEditData.TryGetValue(guild.Id, out var data)) {
            data.RemoveAll(x => DateTime.Now - x.Item1.CreationTimestamp > TimeSpan.FromHours(1));
            return data;
        }

        data = new List<(DiscordMessage, string)>();
        GuildEditData.Add(guild.Id, data);

        return data;
    }
}

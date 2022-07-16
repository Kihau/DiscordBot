using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using Shitcord.Data;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Services;

public class AudioService
{
    private LavalinkService Lavalink { get; }
    private Dictionary<ulong, GuildAudioData> AudioData { get; }
    private DiscordClient Client { get; }
    private DatabaseService DatabaseContext { get; }
    private Random Rng { get; }
    
    public AudioService(Discordbot bot, LavalinkService lavalink, DatabaseService dbctx)
    {
        Lavalink = lavalink;
        AudioData = new Dictionary<ulong, GuildAudioData>();
        Client = bot.Client;
        Rng = new Random();
        DatabaseContext = dbctx;

        Client.VoiceStateUpdated += BotVoiceTimeout;
        Client.ComponentInteractionCreated += AudioUpdateButtons;

        Client.Ready += (_, _) => {
            LoadAllDataFromDatabase();
            return Task.CompletedTask;
        };
    }

    public void LoadAllDataFromDatabase()
    {
        foreach (var (id, guild) in Client.Guilds) {
            bool exists_in_table = DatabaseContext.ExistsInTable(
                GuildAudioTable.TABLE_NAME, 
                Condition.New(GuildAudioTable.GUILD_ID.name).Equals(id)
            );

            if (exists_in_table) GetOrAddData(guild);
        }
    }

    private Task AudioUpdateButtons(
        DiscordClient client, ComponentInteractionCreateEventArgs args
    ) {
        Task.Run(async () =>
        {
            this.AudioData.TryGetValue(args.Guild.Id, out var data);
            if (data == null) 
                return;

            bool defer = true;
            switch (args.Id)
            {
                // Song Info
                case "skip_btn":
                    await data.SkipAsync(1);
                    break;
                case "loop_btn":
                    data.ChangeLoopingState();
                    break;
                case "state_btn":
                    if (data.IsStopped)
                        await data.PlayAsync();
                    else if (data.IsPaused)
                        await data.ResumeAsync();
                    else await data.PauseAsync();
                    break;
                case "join_btn":
                    var member = await args.Guild.GetMemberAsync(args.User.Id);
                    if (member.VoiceState != null)
                        await data.CreateConnectionAsync(member.VoiceState.Channel);
                    break;
                case "stop_btn":
                    await data.StopAsync();
                    break;
                case "leave_btn":
                    await data.DestroyConnectionAsync();
                    break;
                
                // Queue Info
                case "firstpage_btn":
                    data.page = 0;
                    break;
                case "nextpage_btn":
                    data.page++;
                    break;
                case "shuffle_btn":
                    data.Shuffle();
                    break;
                case "clear_btn":
                    data.ClearQueue();
                    break;
                case "prevpage_btn":
                    data.page--;
                    break;
                case "lastpage_btn":
                    data.page = Int32.MaxValue;
                    break;
                
                default:
                    defer = false;
                    break;
            }
            
            if (defer)
                await args.Interaction.CreateResponseAsync(
                    InteractionResponseType.DeferredMessageUpdate
                );

            await data.UpdateSongMessage();
            await data.UpdateQueueMessage();
        });
        return Task.CompletedTask;
    }
    
    // TODO: Check if global events
    private Task BotVoiceTimeout(DiscordClient sender, VoiceStateUpdateEventArgs args)
    {
        this.AudioData.TryGetValue(args.Guild.Id, out var data);
        if (data is not {IsConnected: true}) 
            return Task.CompletedTask;

        if (data.Channel == null)
            return Task.CompletedTask;
        
        if (data.Channel.Users.Count == 1)
            data.StartTimeout();
        else if (data.TimeoutStarted) 
            data.CancelTimeout();

        return Task.CompletedTask;
    }

    public GuildAudioData GetOrAddData(DiscordGuild guild)
    {
        if (this.AudioData.TryGetValue(guild.Id, out var data))
            return data;

        data = new GuildAudioData(
            guild, this.Lavalink.Node, this.Client, this.DatabaseContext
        );

        this.AudioData.Add(guild.Id, data);
        return data;
    }

    public async Task<IEnumerable<LavalinkTrack>> GetTracksAsync(Uri uri)
    {
        var result = await this.Lavalink.Node.Rest.GetTracksAsync(uri);
        if (result.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            return result.Tracks;

        if (!result.Tracks.Any())
            return new List<LavalinkTrack>();
        
        return new List<LavalinkTrack> { result.Tracks.First() };
    }

    public async Task<IEnumerable<LavalinkTrack>> GetTracksAsync(String message)
    {
        var result = await this.Lavalink.Node.Rest.GetTracksAsync(message);
        if (result.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            return result.Tracks;
        
        if (!result.Tracks.Any())
            return new List<LavalinkTrack>();
        
        return new List<LavalinkTrack> { result.Tracks.First() }; 
    }

    public Task<LavalinkLoadResult> GetTracksAsync(FileInfo message)
        => this.Lavalink.Node.Rest.GetTracksAsync(message);

    public IEnumerable<LavalinkTrack> Shuffle(IEnumerable<LavalinkTrack> tracks)
        => tracks.OrderBy(_ => this.Rng.Next());
}

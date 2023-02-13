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
    
    public AudioService(DiscordBot bot, LavalinkService lavalink, DatabaseService dbctx) {
        Lavalink = lavalink;
        AudioData = new Dictionary<ulong, GuildAudioData>();
        Client = bot.Client;
        Rng = new Random();
        DatabaseContext = dbctx;

        Client.VoiceStateUpdated += BotVoiceTimeout;
        Client.VoiceStateUpdated += BotVoiceAutoJoin;
        Client.ComponentInteractionCreated += AudioUpdateButtons;

        Client.GuildDownloadCompleted += (_, _) => Task.Run(LoadAllDataFromDatabase);
    }

    private void LoadAllDataFromDatabase() {
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
        Task.Run(async () => {
            AudioData.TryGetValue(args.Guild.Id, out var data);
            if (data == null) 
                return;

            bool deferred = true;
            switch (args.Id)
            {
            // Song Info
            case "skip_btn": {
                await data.SkipAsync(1);
                data.SongRequiresUpdate = true;
                data.QueueRequiresUpdate = true;
            } break;
            case "prev_btn": {
                await data.PreviousAsync();
                data.SongRequiresUpdate = true;
                data.QueueRequiresUpdate = true;
            } break;
            case "loop_btn": {
                if (data.Looping == LoopingMode.Shuffle)
                    data.Looping = 0;
                else data.Looping++;
                data.SongRequiresUpdate = true;
            } break;
            case "state_btn": { 
                if (data.IsStopped)
                    await data.PlayAsync();
                else if (data.IsPaused)
                    await data.ResumeAsync();
                else await data.PauseAsync();
                data.SongRequiresUpdate = true;
                data.QueueRequiresUpdate = true;
            } break;
            case "remove_btn": {
                await data.SkipAsync(1, true);
                data.SongRequiresUpdate = true;
                data.QueueRequiresUpdate = true;
            } break;
            case "join_btn": {
                var member = await args.Guild.GetMemberAsync(args.User.Id);
                if (member.VoiceState != null)
                    await data.CreateConnectionAsync(member.VoiceState.Channel);
                data.SongRequiresUpdate = true;
            } break;
            case "stop_btn": {
                await data.StopAsync();
                data.SongRequiresUpdate = true;
            } break;
            case "leave_btn": {
                await data.DestroyConnectionAsync();
                data.SongRequiresUpdate = true;
            } break;

            // Queue Info
            case "firstpage_btn": {
                data.page = 0;
                data.QueueRequiresUpdate = true;
            } break;
            case "nextpage_btn": {
                data.page++;
                data.QueueRequiresUpdate = true;
            } break;
            case "shuffle_btn": {
                data.Shuffle();
                data.SongRequiresUpdate = true;
                data.QueueRequiresUpdate = true;
            } break;
            case "clear_btn": {
                data.ClearQueue();
                data.SongRequiresUpdate = true;
                data.QueueRequiresUpdate = true;
            } break;
            case "prevpage_btn": {
                data.page--;
                data.QueueRequiresUpdate = true;
            } break;
            case "lastpage_btn": {
                data.page = Int32.MaxValue;
                data.QueueRequiresUpdate = true;
            } break;
            default:
                deferred = false;
                break; 
            }
            
            if (deferred) {
                await args.Interaction.CreateResponseAsync(
                    InteractionResponseType.DeferredMessageUpdate
                );
            }
        });
        return Task.CompletedTask;
    }

    private Task BotVoiceAutoJoin(DiscordClient sender, VoiceStateUpdateEventArgs args) {
        Task.Run(async () => {
            AudioData.TryGetValue(args.Guild.Id, out var data);
            if (data == null || data.IsConnected || !data.AutoJoinChannel) 
                return;

            if (args.Channel == null)
                return;

            if (args.Before == null && args.Channel.Users.Count == 1) {
                await data.CreateConnectionAsync(args.Channel);

                if (data.ResumeOnAutoJoin)
                    await data.PlayAsync();
            }
        });

        return Task.CompletedTask;
    }
    
    private Task BotVoiceTimeout(DiscordClient sender, VoiceStateUpdateEventArgs args) {
        AudioData.TryGetValue(args.Guild.Id, out var data);
        if (data is not {IsConnected: true}) 
            return Task.CompletedTask;

        if (data.Channel == null)
            return Task.CompletedTask;
        
        if (data.Channel.Users.Count <= 1)
            data.StartTimeout();
        else if (data.TimeoutStarted) 
            data.CancelTimeout();

        return Task.CompletedTask;
    }

    public GuildAudioData GetOrAddData(DiscordGuild guild)
    {
        if (AudioData.TryGetValue(guild.Id, out var data))
            return data;

        data = new GuildAudioData(
            guild, Lavalink.Node, Client, DatabaseContext
        );

        this.AudioData.Add(guild.Id, data);
        return data;
    }

    public async Task<IEnumerable<LavalinkTrack>> GetTracksAsync(Uri uri)
    {
        var result = await Lavalink.Node.Rest.GetTracksAsync(uri);
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

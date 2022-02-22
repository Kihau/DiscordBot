using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using Shitcord.Data;

namespace Shitcord.Services;

    public class AudioService
    {
        private LavalinkService Lavalink { get; }
        private Dictionary<ulong, GuildAudioData> AudioData { get; }
        private DiscordClient Client { get; }
        private Random Rng { get; }
        
        public AudioService(Discordbot bot, LavalinkService lavalink)
        {
            this.Lavalink = lavalink;
            this.AudioData = new Dictionary<ulong, GuildAudioData>();
            this.Client = bot.Client;
            this.Rng = new Random();
            
            this.Client.VoiceStateUpdated += BotVoiceTimeout;
            this.Client.ComponentInteractionCreated += AudioUpdateButtons;
        }

        private Task AudioUpdateButtons(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            Task.Run(async () =>
            {
                this.AudioData.TryGetValue(e.Guild.Id, out var data);
                if (data == null) 
                    return;

                bool defer = true;
                switch (e.Id)
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
                        var member = await e.Guild.GetMemberAsync(e.User.Id);
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
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                await data.UpdateSongMessage();
                await data.UpdateQueueMessage();
            });
            return Task.CompletedTask;
        }
        
        private Task BotVoiceTimeout(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            this.AudioData.TryGetValue(e.Guild.Id, out var data);
            if (data is not {IsConnected: true}) 
                return Task.CompletedTask;

            if (data.Channel == null)
                return Task.CompletedTask;
            
            if (data.Channel.Users.Count == 1) 
                data.StartTimeout();
            else if (data.TimeoutStarted) data.CancelTimeout();

            return Task.CompletedTask;
        }

        public GuildAudioData GetOrAddData(DiscordGuild guild)
        {
            if (this.AudioData.TryGetValue(guild.Id, out var data))
                return data;

            data = new GuildAudioData(guild, this.Lavalink.Node, this.Client);
            this.AudioData.Add(guild.Id, data);
            return data;
        }

        public async Task<IEnumerable<LavalinkTrack>> GetTracksAsync(Uri uri)
        {
            var result = await this.Lavalink.Node.Rest.GetTracksAsync(uri);
            if (result.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
                return result.Tracks;
            
            return new [] { result.Tracks.First() };
        }

        public async Task<IEnumerable<LavalinkTrack>> GetTracksAsync(String message)
        {
            var result = await this.Lavalink.Node.Rest.GetTracksAsync(message);
            if (result.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
                return result.Tracks;
            
            return new [] { result.Tracks.First() }; 
        }

        public Task<LavalinkLoadResult> GetTracksAsync(FileInfo message)
            => this.Lavalink.Node.Rest.GetTracksAsync(message);

        public IEnumerable<LavalinkTrack> Shuffle(IEnumerable<LavalinkTrack> tracks)
            => tracks.OrderBy(_ => this.Rng.Next());
    }
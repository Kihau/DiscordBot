using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using Shitcord.Data;

namespace Shitcord.Services;

public class TimeService
    {
        private Dictionary<ulong, GuildWeatherData> TimeData { get; }
        private DiscordClient Client { get; }
        
        public TimeService(Discordbot bot)
        {
            this.TimeData = new Dictionary<ulong, GuildWeatherData>();
            this.Client = bot.Client;
        }

        public GuildWeatherData GetOrAddData(DiscordGuild guild)
        {
            if (this.TimeData.TryGetValue(guild.Id, out var data))
                return data;

            //data = new GuildWeatherData(guild);
            this.TimeData.Add(guild.Id, data);
            return data;
        }
    }
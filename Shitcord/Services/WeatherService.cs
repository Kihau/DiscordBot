using DSharpPlus;
using DSharpPlus.Entities;
using Shitcord.Data;

namespace Shitcord.Services;

public class WeatherService
{
    private Dictionary<ulong, GuildWeatherData> WeatherData { get; }
    private DiscordClient Client { get; }
    
    public WeatherService(Discordbot bot)
    {
        this.WeatherData = new Dictionary<ulong, GuildWeatherData>();
        this.Client = bot.Client;
    }

    public GuildWeatherData GetOrAddData(DiscordGuild guild)
    {
        if (this.WeatherData.TryGetValue(guild.Id, out var data))
            return data;

        if (data is null) {
            data = new GuildWeatherData();
            this.WeatherData.Add(guild.Id, data);
        }

        return data;
    }
}

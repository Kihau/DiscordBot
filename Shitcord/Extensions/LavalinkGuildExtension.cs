using System.Text.Json;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;

namespace Shitcord.Extensions;

public static class LavalinkGuildExtension
{
    public static Task SetPitchAsync(this LavalinkGuildConnection conn)
    {
        return  Task.CompletedTask;
    }

    public static string GetJson(this AudioFilters filter)
    {
        var options = new JsonSerializerOptions {WriteIndented = true};
        string output = JsonSerializer.Serialize(filter, options);

        return output;
    }
    
    public static AudioFilters? GetAudioFilters(this string json)
        => JsonSerializer.Deserialize<AudioFilters>(json);
}
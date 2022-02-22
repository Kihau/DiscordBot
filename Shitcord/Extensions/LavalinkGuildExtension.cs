using System.Threading.Tasks;
using DSharpPlus.Lavalink;

namespace Shitcord.Extensions;

public static class LavalinkGuildExtension
{
    public static Task SetPitchAsync(this LavalinkGuildConnection conn)
    {
        return  Task.CompletedTask;
    }
}
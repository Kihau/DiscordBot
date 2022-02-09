using System.Threading.Tasks;
using DSharpPlus.Lavalink;

namespace Shitcord.Modules;

public static class LavalinkGuildExtension
{
    public static Task SetPitchAsync(this LavalinkGuildConnection conn)
    {
        return  Task.CompletedTask;
    }
}
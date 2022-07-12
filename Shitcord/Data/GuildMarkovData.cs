using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.EventArgs;
using System.Collections.Concurrent;
using Shitcord.Services;

namespace Shitcord.Data;

public class GuildMarkovData
{
    public DiscordGuild Guild { get; set; }
    public bool IsEnabled { get; set; }
    public bool EnableDataGathering { get; set; }

    public const int MAX_CHANCE = 100;
    public int MessageChance { get; set; }

    public List<DiscordChannel> IncludedChannels { get; set; } = new();

    public bool ResponseEnables { get; set; }
    public TimeSpan ResponseTimeout { get; set; }
    public Timer? ResponseTimer { get; set; }

    public int MinChainLength { get; set; } = 8;
    public int MaxChainLength { get; set; } = 22;

    public GuildMarkovData(DiscordGuild guild, DiscordClient client) 
    {
        Guild = guild;
    }
}

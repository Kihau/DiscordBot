using DSharpPlus;
using DSharpPlus.Entities;
using System.Collections.Concurrent;
using Shitcord.Services;

namespace Shitcord.Data;

public class GuildMarkovData
{
    public bool IsEnabled { get; set; } = true;

    public const int MAX_CHANCE = 100;
    public int MessageChance { get; set; } = 10;

    public bool IncludeAddChannels { get; set; } = false;
    public List<ulong> IncludedChannelIDs { get; set; } = new();

    public bool ResponseEnables { get; set; } = false;
    public TimeSpan ResponseTimeout { get; set; } = new(0, 30, 0);
    public DateTime ResponseTimer { get; set; } = DateTime.Now;

    public int MinChainLength { get; set; } = 8;
    public int MaxChainLength { get; set; } = 28;
}

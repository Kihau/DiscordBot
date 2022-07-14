using DSharpPlus;
using DSharpPlus.Entities;
using System.Collections.Concurrent;
using Shitcord.Services;

namespace Shitcord.Data;

public class GuildMarkovData
{
    public bool IsEnabled { get; set; } = true;
    // NOTE: Adding channel blocks either auto response and data gathering
    public List<ulong> ExcludedChannelIDs { get; set; } = new();

    public const int MAX_CHANCE = 1000;
    public bool ResponseEnabled { get; set; } = false;
    public int ResponseChance { get; set; } = 100;
    public TimeSpan ResponseTimeout { get; set; } = new(0, 30, 0);
    public DateTime LastResponse { get; set; } = DateTime.Now;

    public const int DEFAULT_MIN = 8;
    public const int DEFAULT_MAX = 28;
    public int MinChainLength { get; set; } = 8;
    public int MaxChainLength { get; set; } = 28;
}

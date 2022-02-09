using DSharpPlus;
using DSharpPlus.Entities;

namespace Shitcord.Data;

/// <summary>
/// UNUSED CODE - NO FUN ALLOWED
/// DOESN'T WORD DUE TO DISCORD API RATELIMITS
/// </summary>

[Obsolete("discord ratelimits channel updates", true)]
public class GuildTimeData
{
    // store channel id in database 
    private DiscordChannel _channel;
    private DiscordGuild _guild;
    private Timer _updateTimer;
    
    public GuildTimeData(DiscordGuild guild)
    {
        _guild = guild;
    }
    
    public async Task SetDateChannel(DiscordChannel channel)
    {
        _channel = channel;
        _updateTimer = new Timer(UpdateDateChannel);
    }

    public async Task CreateDateChannel()
    {
        var d = DateTime.Now;
        _channel =  await _guild.CreateChannelAsync($"{d:HH}-{d:mm}", ChannelType.Text);
        _updateTimer = new Timer(UpdateDateChannel, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }
    
    private void UpdateDateChannel(object? state)
    {
        _channel.ModifyAsync(model =>
        {
            // Sync to ticks to system time
            var d = DateTime.Now;
            model.Name = $"{d:HH}-{d:mm}";
        });
    }
}
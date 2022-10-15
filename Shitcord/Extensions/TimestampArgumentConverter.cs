using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace Shitcord.Extensions;

public enum SeekSign {
    Plus, Minus, None
}

public struct Timestamp {
    SeekSign sign;
    TimeSpan seek_time;
}

/* Parsing the timestamp:
 * d, h, m, s - time tokens (day, hour, minute, secons)
 * +, -, (nothing) - operation tokens
 * 12, 42, 65 - value tokens
 * xx:xx - special timestamps
 *
 * Example strings:
 *     +12m - skip by 12 minutes
 *     - 2s - go back by 2 seconds
 *     1m 2 s - seek to 1 minute 2 second
 *     + 01:00 - skip by one minute
 *     - 1 - go back by one second
 *     2:00:01 - seek to 2 hour 1 second
 */ 
public class TimestampArgumentConverter : IArgumentConverter<Timestamp>
{
    // Referance: https://dsharpplus.github.io/articles/commands/argument_converters.html
    public Task<Optional<Timestamp>> ConvertAsync(string value, CommandContext ctx)
    {
        //
        // Parse stuff here
        //
        throw new NotImplementedException();
    }
}

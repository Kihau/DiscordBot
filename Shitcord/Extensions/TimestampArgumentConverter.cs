using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace Shitcord.Extensions;

public enum SeekSign {
    Plus, Minus, None
}

public struct SeekStamp {
    public SeekSign sign;
    public TimeSpan seek_time;
    public SeekStamp(SeekSign sign, TimeSpan seekTime)
    {
        this.sign = sign;
        seek_time = seekTime;
    }
}

/* Parsing the timestamp:
 * d, h, m, s - time tokens (day, hour, minute, secons[:brainlet:])
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
public class TimestampArgumentConverter : IArgumentConverter<SeekStamp>
{
    // Referance: https://dsharpplus.github.io/articles/commands/argument_converters.html
    public Task<Optional<SeekStamp>> ConvertAsync(string time, CommandContext ctx)
    {
        //throw new CommandException("An argument {repeated} was repeated");
        //if argument is repeated throw RepeatedArgumentException
        //1m 4h /- 2 s 3h
        time = time.Trim();
        SeekSign sign = time[0] switch {
            '-' => SeekSign.Minus,
            '+' => SeekSign.Plus,
            _   => SeekSign.None
        };

        int seconds;
        if (sign == SeekSign.None)
            seconds = parseSeconds(time, 0);
        else seconds = parseSeconds(time, 1);

        // TODO(?): Throw CommandException instead ???
        if (seconds == -1) 
            return Task.FromResult(Optional.FromNoValue<SeekStamp>());
        
        SeekStamp stamp = new SeekStamp(sign, TimeSpan.FromSeconds(seconds));
        return Task.FromResult(Optional.FromValue(stamp));
    }

    public static int parseSeconds(String time) => parseSeconds(time, 0);

    public static int parseSeconds(String time, int index)
    {
        int t1 = tryParseSeparatorToken(time, index);
        int t2 = tryParseSuffixUnit(time, index);
        if (t1 == -1 && t2 == -1) {
            throw new CommandException("No appropriate format could be found");
        }
        return t1 == -1 ? t2 : t1;
    }

    public static int tryParseSuffixUnit(string time, int index)
    {
        StringBuilder num = new StringBuilder();
        int seconds = 0;
        bool awaitUnit = false;
        bool awaitDigit = true;
        //-1 1 h (handled)
        //-d 1h 1 h (handled)
        //-1d m3 1h (
        //-1d 5 1h
        
        for (int i = index; i < time.Length; i++) {
            char ch = time[i];
            switch (ch) {
                case 's':
                case 'm':
                case 'h':
                case 'd': {
                    if (!awaitUnit) 
                        throw new CommandException($"Another unit was not expected at index {i}");
                    
                    if (awaitDigit)
                        throw new CommandException($"Digit was expected, current index: {i}");

                    awaitUnit = false;
                    awaitDigit = true;
                    seconds += toSeconds(num, ch);
                    num.Clear();
                } break;
                
                case ' ':
                    continue;
                default:
                    if (char.IsDigit(ch)) {
                        awaitDigit = false;
                        if (i != 0) {
                            char prvs = time[i - 1];
                            if (awaitUnit && prvs == ' ') throw new CommandException(
                                $"Unit was expected, current index: {i}"
                            );
                        }
                        
                        awaitUnit = true;
                        num.Append(ch);
                    }
                    else
                        throw new CommandException("Unrecognized character");
                    break;
            }
        }

        if (num.Length != 0) {
            throw new CommandException("Number without a unit was provided");
        }
        return seconds;
    }

    private static int toSeconds(StringBuilder num, char ch)
    {
        bool success = int.TryParse(num.ToString(), out var val);
        if (!success)
            throw new CommandException("Try parse failed");
        
        if(val < 0)
            throw new CommandException("Integer overflowed after parse");
        
        var value = ch switch
        {
            's' => val,
            'm' => val * 60,
            'h' => val * 3600,
            'd' => val * 3600 * 24,
            _ => -1
        };
        if(value < 0)
            throw new CommandException("Integer overflowed after multiplication");
        return value;
    }

    private static int tryParseSeparatorToken(string time, int index)
    {
        return -1;
        /*
        StringBuilder num = new StringBuilder();
        int seconds = 0;
        bool awaitSep = false;
        bool awaitDigit = false;
        for (int i = time.Length-1; i > 0; i++) {
            switch (time[i]) {
                case ':':
                case '.':
                case '/':
                    
                    break;
                
                case ' ':
                    
                    break;
                default:
                    if (char.IsDigit(time[i])) {
                        awaitSep = true;
                        num.Append(time[i]);
                        //append unit
                    }
                    throw new CommandException("Unrecognized character");
            }
        }
        return seconds;
        */
        //TODO to implement
    }
}

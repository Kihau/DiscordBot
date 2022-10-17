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
public class SeekstampArgumentConverter : IArgumentConverter<SeekStamp>
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
        bool exc = false;
        int seconds = -1;
        try {
            seconds = tryParseSuffixUnit(time, index);
        }catch{ 
            exc = true;
        }

        if (!exc)
            return seconds;
        
        try {
            seconds = tryParseSeparatorToken(time, index);
        }catch{ 
            throw new CommandException("No appropriate format could be found");
        }
        return seconds;
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

    public static int tryParseSeparatorToken(string time, int indexInclusive)
    {
        StringBuilder num = new StringBuilder();
        int separators = 0;
        int seconds = 0;
        bool awaitSep = false;
        for (int i = time.Length-1; indexInclusive <= i; i--) {
            char ch = time[i];
            switch (ch) {
                case ':':
                case '.':
                    if(!awaitSep)
                        throw new CommandException("Separator wasn't expected");

                    separators++;
                    awaitSep = false;
                    seconds += secondsFrom(num, separators);
                    num.Clear();
                    break;
                case ' ':
                    throw new CommandException("Whitespace is forbidden");
                default:
                    if (char.IsDigit(ch)) {
                        if (num.Length == 2) {
                            throw new CommandException("Three digits in a row");
                        }
                        awaitSep = true;
                        num.Append(ch);
                    }else
                        throw new CommandException("Unrecognized character");
                    break;
            }
        }

        if (num.Length > 0)
            seconds += secondsFrom(num, separators+1);

        return seconds;
    }

    private static int secondsFrom(StringBuilder num, int separators)
    {
        bool alwaysSuccess = int.TryParse(reverse(num), out var val);
        switch (separators) {
            case 1:
                if (val > 59){
                    throw new CommandException("Second value exceeds 59");
                }
                return val;
            case 2:
                if (val > 59){
                    throw new CommandException("Minute value exceeds 59");
                }
                return val * 60;
            case 3:
                if (val > 23){
                    throw new CommandException("Hours specified exceed 23");
                }
                return val * 3600;
            case 4:
                if (val > 6){
                    throw new CommandException("Days specified exceed 6");
                }
                return val * 86400;
            default:
                throw new CommandException("Too many separators");
        }
    }

    private static string reverse(StringBuilder num)
    {
        switch (num.Length) {
            case 1:
                return num.ToString();
            case 2:
                //python moment
                (num[0], num[1]) = (num[1], num[0]);
                return num.ToString();
            default:
                throw new CommandException("Three digits reverse");
        }
    }
}

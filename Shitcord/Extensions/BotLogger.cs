using System.Text;
using Microsoft.Extensions.Logging;

namespace Shitcord.Extensions;

// NODE: This is garbage, but the DSharpPlus requires this crap to set up custom logging
public class BotLoggerFactory : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName)
        => new BotLogger("shitcord", "botlogs");

    public void Dispose() { }
}

// TODO: Add some options - max log file size, how many days to keep, etc
public class BotLogger : ILogger
{
    public LogLevel MinimumLevel { get; }
    public string TimestampFormat { get; }
    public string? LogFileName { get; }
    public string? LogDirectoryName { get; }

    private string _path = "";

    public BotLogger(
        string? file_name = null,
        string? directory_name = null,
        LogLevel min_level = LogLevel.Information,
        string timestamp_format = "yyyy-MM-dd HH:mm:ss zzz"
    ) {
        MinimumLevel = min_level;
        TimestampFormat = timestamp_format;
        LogFileName = file_name;
        LogDirectoryName = directory_name;

        if (LogDirectoryName != null && !String.IsNullOrWhiteSpace(LogDirectoryName))
            _path += LogDirectoryName + "/";

        if (LogFileName != null)
            _path += LogFileName + ".log";
    }

    public void Log<TState>(
        LogLevel log_level, EventId event_id, TState state, Exception exception, 
        Func<TState, Exception, string> formatter
    ) {
        if (!this.IsEnabled(log_level))
            return;

        var log_output = new StringBuilder();

        var ename = event_id.Name;
        ename = ename?.Length > 12 ? ename?.Substring(0, 12) : ename;
        var time = DateTimeOffset.Now.ToString(this.TimestampFormat);

        log_output.Append($"[{time}] [{event_id.Id,-4}/{ename,-12}] ");
        Console.Write($"[{time}] [{event_id.Id,-4}/{ename,-12}] ");

        switch (log_level) {
            case LogLevel.Trace:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;

            case LogLevel.Debug:
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                break;

            case LogLevel.Information:
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                break;

            case LogLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;

            case LogLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;

            case LogLevel.Critical:
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Black;
                break;
        }

        var level_string = (log_level switch {
            LogLevel.Trace => "[Trace] ",
            LogLevel.Debug => "[Debug] ",
            LogLevel.Information => "[Info ] ",
            LogLevel.Warning => "[Warn ] ",
            LogLevel.Error => "[Error] ",
            LogLevel.Critical => "[Crit ]",
            LogLevel.None => "[None ] ",
            _ => "[?????] "
        });

        Console.Write(level_string);
        log_output.Append(level_string);

        Console.ResetColor();

        // The foreground color is off.
        if (log_level == LogLevel.Critical) {
            Console.Write(" ");
            log_output.Append(" ");
        }

        var message = formatter(state, exception);

        log_output.AppendLine(message);
        Console.WriteLine(message);

        if (exception != null) {
            log_output.AppendLine(exception.ToString());
            Console.WriteLine(exception);
        }

        if (LogFileName == null)
            return;

        if (LogDirectoryName != null && !Directory.Exists(LogDirectoryName)) {
            Directory.CreateDirectory(LogDirectoryName);
        }

        File.AppendAllText(_path, log_output.ToString());

        var file_info = new FileInfo(_path);
        if (DateTime.Now - file_info.CreationTime > TimeSpan.FromDays(1))
            File.Move(_path, $"{_path}-{DateTime.Now.ToString("yyyy-MM-dd")}");
    }

    public bool IsEnabled(LogLevel log_level)
        => log_level >= MinimumLevel;

    public IDisposable BeginScope<TState>(TState state) 
        => throw new NotImplementedException();
}

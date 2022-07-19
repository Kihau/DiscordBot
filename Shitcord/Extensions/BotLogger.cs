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
    {
        Directory.CreateDirectory("botlogs");
        return new BotLogger("logs/shitcord.log");
    }

    public void Dispose() { }
}

public class BotLogger : ILogger
{
    public LogLevel MinimumLevel { get; }
    public string TimestampFormat { get; }
    public string? OutputLogPath { get; }

    public BotLogger(
        string? output_path = null,
        LogLevel min_level = LogLevel.Information,
        string timestamp_format = "yyyy-MM-dd HH:mm:ss zzz"
    ) {
        MinimumLevel = min_level;
        TimestampFormat = timestamp_format;
        OutputLogPath = output_path;
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
        if (log_level == LogLevel.Critical)
            Console.Write(" ");

        var message = formatter(state, exception);

        log_output.AppendLine(message);
        Console.WriteLine(message);

        if (exception != null) {
            log_output.AppendLine(exception.ToString());
            Console.WriteLine(exception);
        }

        if (OutputLogPath == null)
            return;

        if (File.Exists(OutputLogPath)) {
            var file_info = new FileInfo(OutputLogPath);
            if (DateTime.Now - file_info.CreationTime > TimeSpan.FromDays(1))
                File.Move(OutputLogPath, $"{OutputLogPath}-{DateTime.Now.ToString("yyyy-MM-dd")}");
        }

        File.AppendAllText(OutputLogPath, log_output.ToString());
    }

    public bool IsEnabled(LogLevel log_level)
        => log_level >= MinimumLevel;

    public IDisposable BeginScope<TState>(TState state) 
        => throw new NotImplementedException();
}

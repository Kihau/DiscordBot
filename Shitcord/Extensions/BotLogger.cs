using System.Text;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Shitcord.Extensions;

// NOTE: This is garbage, but the DSharpPlus requires this crap to set up custom logging
public class BotLoggerFactory : ILoggerFactory
{
    private BotConfig Config { get; }

    public BotLoggerFactory(BotConfig config) 
        => Config = config;

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName)
        => new BotLogger(Config.Logging);

    public void Dispose() { }
}

public class BotLogger : ILogger
{
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss zzz";
    public string LogFileName { get; set; } = "shitcord";

    private string _path = "";
    private LoggingConfig Config { get; }

    public BotLogger(LoggingConfig config) 
    {
        Config = config;

        if (String.IsNullOrWhiteSpace(Config.Directory))
            throw new ArgumentException("Directory name cannot be whitespace");

        _path += Config.Directory+ "/";
        _path += LogFileName + ".log";
    }


    public void Log<TState>(
        LogLevel log_level, EventId event_id, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter
    ) {
        if (!IsEnabled(log_level))
            return;

        var log_output = new StringBuilder();

        var ename = event_id.Name;
        ename = ename?.Length > 12 ? ename?.Substring(0, 12) : ename;
        var time = DateTimeOffset.Now.ToString(TimestampFormat);

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

        var level_string = log_level switch {
            LogLevel.Trace => "[Trace] ",
            LogLevel.Debug => "[Debug] ",
            LogLevel.Information => "[Info ] ",
            LogLevel.Warning => "[Warn ] ",
            LogLevel.Error => "[Error] ",
            LogLevel.Critical => "[Crit ]",
            LogLevel.None => "[None ] ",
            _ => "[?????] "
        };

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

        if (!Config.SaveToFile) return;

        if (!Directory.Exists(Config.Directory))
            Directory.CreateDirectory(Config.Directory);

        ArchiveOldLogs();

        // Why do I have to do this?!?!
        // What THE FUCK is wrong with this language
        DateTime creation_time = DateTime.Now;
        if (File.Exists(_path))
            creation_time = File.GetCreationTime(_path); 

        // Also I might want to open the stream and keep writing to it
        // instead of opening the file every time.
        File.AppendAllText(_path, log_output.ToString());
        File.SetCreationTime(_path, creation_time);
    }

    private void ArchiveOldLogs()
    {
        if (!File.Exists(_path))
            return;
        
        if (DateTime.Now - File.GetCreationTime(_path) > TimeSpan.FromDays(1)) {

            var log_files = new DirectoryInfo(Config.Directory).GetFiles()
                .Where(files => files.Name.StartsWith(LogFileName + ".log")).ToList();

            // When number of log files exceeds MaxHistory, delete old logs 
            if (log_files.Count >= Config.MaxHistory) {
                int amount = log_files.Count - Config.MaxHistory - 1;
                var files_to_rm = log_files.OrderBy(file => file.CreationTime).Take(amount);
                
                foreach (var file in files_to_rm)
                    file.Delete();
            }

            CompressAndMove();
        }
    }

    public void CompressAndMove()
    {
        var file = new FileInfo(_path);
        using (FileStream original_fs = file.OpenRead()) {
            using FileStream compressed_fs = 
                File.Create($"{_path}-{DateTime.Now.ToString("yyyy-MM-dd")}.gz");

            using GZipStream compression_stream = 
                new GZipStream(compressed_fs, CompressionMode.Compress);

            original_fs.CopyTo(compression_stream);
        }

        file.Delete();
    }

    public bool IsEnabled(LogLevel log_level)
        => log_level >= Config.MinLogLevel && Config.IsEnabled;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        throw new NotImplementedException();
    }
}

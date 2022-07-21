using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Shitcord;

[Serializable]
public class BotConfig
{
    [JsonPropertyName("discord")] 
    public DiscordConfig Discord { get; set; } = new();

    [JsonPropertyName("logging")] 
    public LoggingConfig Logging { get; set; } = new();

    [JsonPropertyName("ssh")] 
    public SshConfig Ssh { get; set; } = new();

    [JsonPropertyName("lavalink")] 
    public LavalinkConfig Lava { get; set; } = new();

    public BotConfig() { }

    public BotConfig(string? path = null)
    {
        path ??= "Resources/config.json";
        if (!File.Exists(path)) {
            this.Save(path);

            throw new Exception(
                "Example config file was generated. Edit it in order to start the bot"
            );
        }
        
        this.Load(path);
    }

    private void Load(string path)
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BotConfig>(json);

        if (config == null)
            throw new Exception(
                "Invalid configuration file. Remove it and run" +
                " the program again to generate example config"
            );
        
        this.Discord = config.Discord;
        this.Ssh = config.Ssh;
        this.Lava = config.Lava;
    }

    public void Save(string path)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string output = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, output);
    }
}

[Serializable]
public class DiscordConfig
{
    [JsonPropertyName("token")] 
    public string Token { get; set; } = "discord token";

    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = ">>";

    [JsonPropertyName("cache")]
    public int CacheSize { get; set; } = 100;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "something";

    [JsonPropertyName("startdelay")]
    public int StartDelay { get; set; } = 0;
}

[Serializable]
public class LoggingConfig
{
    [JsonPropertyName("enabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("min-loglevel")]
    public LogLevel MinLogLevel { get; set; } = LogLevel.Information;

    [JsonPropertyName("save-to-file")]
    public bool SaveToFile { get; set; } = true;

    [JsonPropertyName("directory")] 
    public string Directory { get; set; } = "botlogs";

    [JsonPropertyName("max-history")]
    public int MaxHistory { get; set; } = 30;
}

[Serializable]
public class SshConfig
{
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = "localhost";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 1111;

    [JsonPropertyName("username")] 
    public string Username { get; set; } = "derp";

    [JsonPropertyName("key")]
    public string Keyfile { get; set; } = "/path/to/a/keyfile";

    [JsonPropertyName("enabled")] 
    public bool IsEnabled { get; set; } = false;
}

[Serializable]
public class LavalinkConfig
{
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = "localhost";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 2333;

    [JsonPropertyName("password")]
    public string Password { get; set; } = "password here";

    [JsonPropertyName("enabled")]
    public bool IsEnabled { get; set; } = false;

    [JsonPropertyName("autostart")]
    public bool AutoStart { get; set; } = false;

    [JsonPropertyName("javapath")]
    public string JavaPath { get; set; } 
        = "path/to/java.exe (or just java if set as enviroment veriable)";
}

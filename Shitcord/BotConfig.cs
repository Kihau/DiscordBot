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

    [JsonPropertyName("lavalink")] 
    public LavalinkConfig Lava { get; set; } = new();

    [JsonPropertyName("openai")] 
    public OpenAIConfig OpenAI { get; set; } = new();

    [JsonPropertyName("genius")] 
    public GeniusConfig Genius { get; set; } = new();

    public BotConfig() { }

    public BotConfig(string? path = null)
    {
        path ??= "Resources/config.json";
        if (!File.Exists(path)) {
            SaveConfig(path);

            throw new Exception(
                $"Example config file was generated {path}. Edit it in order to start the bot"
            );
        }
        
        LoadConfig(path);
    }

    private void LoadConfig(string path)
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BotConfig>(json);

        if (config == null) {
            throw new Exception(
                "Invalid configuration file. Remove it and run " +
                "the program again to generate new example config"
            );
        }
        
        Discord = config.Discord;
        Lava = config.Lava;
        Logging = config.Logging;
        OpenAI = config.OpenAI;
        Genius = config.Genius;
    }

    public void SaveConfig(string path)
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
    public string JavaPath { get; set; } = "path/to/java.exe (or just java if set as an enviroment veriable)";

    [JsonPropertyName("connectiontimeout")]
    public int ConnectionTimeout { get; set; } = 3000;
}

[Serializable]
public class OpenAIConfig 
{
    [JsonPropertyName("enabled")] 
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("token")] 
    public string Token { get; set; } = "openai api key";

    [JsonPropertyName("users")]
    public List<ulong> AllowedUsers { get; set; } = new();
}

[Serializable]
public class GeniusConfig 
{
    [JsonPropertyName("token")] 
    public string Token { get; set; } = "your genus token";

    [JsonPropertyName("min_length")] 
    public int MinLength { get; set; } = 3;
}

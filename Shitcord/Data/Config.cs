using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shitcord.Data;

[Serializable]
public class Config
{
    [JsonPropertyName("discord")] 
    public DiscordConfig Discord { get; set; }

    [JsonPropertyName("ssh")] 
    public SshConfig Ssh { get; set; }

    [JsonPropertyName("lavalink")] 
    public LavalinkConfig Lava { get; set; }

    private void Init(string path)
    {
        this.Discord = new DiscordConfig();
        this.Ssh = new SshConfig();
        this.Lava = new LavalinkConfig();

        this.Save(path);
    }

    public Config() { }

    public Config(string? path = null)
    {
        path ??= "Resources/config.json";
        if (!File.Exists(path))
        {
            this.Init(path);
            throw new Exception("Example config file was generated. Edit it in order to start the bot");
        }
        
        this.Load(path);
    }

    private void Load(string path)
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<Config>(json);

        if (config == null)
            throw new Exception(
                "Invalid configuration file. Remove it and run the program again to generate example config");
        
        this.Discord = config.Discord;
        this.Ssh = config.Ssh;
        this.Lava = config.Lava;
    }

    public void Save(string path)
    {
        var options = new JsonSerializerOptions {WriteIndented = true};
        string output = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, output);
    }
}

// TODO: Add option to enable/disable logging
// TODO: Create logging class (maybe) add option similar to those in lavalink config file 
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

    [JsonPropertyName("enabled")] public bool IsEnabled { get; set; } = false;
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
    public string JavaPath { get; set; } = "path/to/java.exe (or just java if set as enviroment veriable)";
}
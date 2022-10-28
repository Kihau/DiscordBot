using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shitcord.Data;

public class WhilelistEntry {
    [JsonPropertyName("userid")] 
    public ulong userid;
    [JsonPropertyName("username")] 
    public string? username;

    public WhilelistEntry() { }
}

// Mostly used for storing random/temporary stuff
public static class GlobalData {
    public static void StaticInitalize() {
        if (!File.Exists(whitelist_path))
            File.Create(whitelist_path);

        string json = File.ReadAllText(whitelist_path);
        try {
            mc_whitelist = JsonSerializer.Deserialize<List<WhilelistEntry>>(json) ?? new();
        } catch { /* Ignored for now */ }
    }

    [JsonPropertyName("whitelist")] 
    public static List<WhilelistEntry> mc_whitelist = new();

    public const string whitelist_path = "Resources/botwhitelist.json";
    public const string mcserver_path = "/home/kihau/Servers/Minecraft";
}

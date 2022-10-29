using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shitcord.Data;

public class WhitelistEntry {
    [JsonPropertyName("userid")] 
    public ulong userid { get; set; }
    [JsonPropertyName("username")] 
    public string? username { get; set; }

    public WhitelistEntry() { }
}

// Mostly used for storing random/temporary stuff
public static class GlobalData {
    public static void StaticInitalize() {
        try {
            if (!File.Exists(whitelist_path))
                File.Create(whitelist_path);

            string json = File.ReadAllText(whitelist_path);
            mc_whitelist = JsonSerializer.Deserialize<List<WhitelistEntry>>(json) ?? new();
        } catch { /* Ignored for now */ }
    }

    public static List<WhitelistEntry> mc_whitelist = new();

    public const string whitelist_path = "Resources/botwhitelist.json";
    public const string mcserver_path = "/home/kihau/Servers/Minecraft";
}

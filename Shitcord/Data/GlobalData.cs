using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shitcord.Data;

// public class WhitelistEntry {
//     [JsonPropertyName("userid")] 
//     public ulong userid { get; set; }
//     [JsonPropertyName("username")] 
//     public string? username { get; set; }
//
//     public WhitelistEntry() { }
// }

// This data is the whitelist.json from Minecraft server dir
public class McWhitelistEntry {
    [JsonPropertyName("uuid")] 
    public string? uuid { get; set; }
    [JsonPropertyName("name")] 
    public string? name { get; set; }

    public McWhitelistEntry() { }
}

public class McWhitelist {
    [JsonPropertyName("userids")]
    List<string> userids { get; set; } = new();

    [JsonPropertyName("whitelist")]
    List<McWhitelistEntry> mc_whitelist { get; set; } = new();

    public McWhitelist() { }
}

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
            // mc_whitelist = JsonSerializer.Deserialize<McWhitelist>(json) ?? new();
            mc_whitelist = JsonSerializer.Deserialize<List<WhitelistEntry>>(json) ?? new();
        } catch { /* Ignored for now */ }
    }

    public static List<WhitelistEntry> mc_whitelist = new();
    // TODO: Do this more robust(but still hacky) way?
    // public static McWhitelist mc_whitelist = new();

    // Temp stuff
    public const string whitelist_path = "Resources/mcwhitelist.json";
    public const string mcserver_path = "/home/kihau/Servers/Minecraft";
}

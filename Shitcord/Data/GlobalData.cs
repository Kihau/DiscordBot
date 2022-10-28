using System.Text.Json;

namespace Shitcord.Data;

public class WhilelistEntry {
    public ulong userid;
    public string? username;
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

    public static List<WhilelistEntry> mc_whitelist = new();
    public const string whitelist_path = "Resources/botwhitelist.json";
    public const string mcserver_path = "/home/kihau/Servers/Minecraft";
}

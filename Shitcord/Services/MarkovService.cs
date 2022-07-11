using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Runtime.Serialization.Formatters.Binary;

namespace Shitcord.Services;

public class MarkovService
{
    private DiscordClient Client { get; }
    private DatabaseService DatabaseContext { get; }
    static Dictionary<string, Dictionary<string, int>> markovStrings = new();
    private char[] _excludeCharacters = { '.', ',', ':', ';', '?', '!' }; 

    public bool Enabled { get; set; } = true;
    public bool LearnEnabled { get; set; } = true;
    
    // TODO: A very lazy solution - store it in database later on
    private string _markovBinaryPath = "markov.bin";

    public MarkovService(Discordbot bot, DatabaseService database) 
    {
        Client = bot.Client;
        DatabaseContext = database;

        //Client.Ready += (sender, e) => {
        //    sender.MessageCreated += MarkovMessageHandler;
        //    return Task.CompletedTask;
        //};

        Client.MessageCreated += MarkovMessageHandler;
    }

    public string GenerateMarkovString(int min_len, int max_len)
    {
        string generated_string = "";
        int current_len = 0;

        if (markovStrings.Count == 0)
            return "";

        var rng = new Random();
        var index = rng.Next(markovStrings.Count - 1);
        var startStrings = markovStrings.Keys.ToArray();
        
        var rand_key = startStrings[index];
        do {
            generated_string += rand_key + " ";
            if (!markovStrings.TryGetValue(rand_key, out var nextDict)) {
                if (current_len++ < min_len) {
                    index = rng.Next(markovStrings.Count - 1);
                    rand_key = startStrings[index];
                    continue;
                }
                break;
            }

            if (nextDict.Count == 0) {
                continue;
            }
            
            var alignedDict = nextDict.ToArray();
            var probabilites = new List<double>();

            int fitness_sum = alignedDict.Select(x => x.Value).Sum();
            double prev_probability = 0.0;
            double fitness = 0.0;
            
            for (int i = 0; i < alignedDict.Length; i++) {
                prev_probability += fitness / fitness_sum;
                probabilites.Add(prev_probability);
            }

            int found = 0;
            var gen_num = rng.NextDouble();
            
            while (found < probabilites.Count) {
                if (gen_num < probabilites[found])
                    break;
                found++;
            }

            rand_key = alignedDict[--found].Key;
        } while (current_len++ < max_len);

        return $"{Char.ToUpper(generated_string[0])}{generated_string.Remove(0, 1)}";
    }

    public void FeedStringsToMarkov(List<string> data) 
    {
        for (int i = 0; i < data.Count - 1; i++) {
            if (markovStrings.TryGetValue(data[i], out var nextDict)) {
                if (nextDict.ContainsKey(data[i + 1]))
                    nextDict[data[i + 1]]++;
                else nextDict.Add(data[i + 1], 1);
            } else markovStrings.Add(data[i--], new());
        }
        markovStrings.TryAdd(data[data.Count - 1], new());
    }

    private Task MarkovMessageHandler(DiscordClient client, MessageCreateEventArgs e)
    {
        if (!LearnEnabled)
            return Task.CompletedTask;

        string input = e.Message.Content.ToLower();
        List<string> data = input.Split(' ').ToList();
        for (int i = 0; i < data.Count; i++) {
            if (_excludeCharacters.Contains(data[i].Last()))
                data[i] = data[i].Remove(data[i].Length - 1);
            else if (_excludeCharacters.Contains(data[i].First()))
                data[i] = data[i].Remove(0);
        }

        FeedStringsToMarkov(data);
        return Task.CompletedTask;
    }

    public void LoadMarkovBinaryData()
    {
        if (!File.Exists(_markovBinaryPath))
            return;

        using (var fs = new FileStream(_markovBinaryPath, FileMode.Open)) {
            var fmt = new BinaryFormatter();
            markovStrings = fmt.Deserialize(fs) 
                as Dictionary<string, Dictionary<string, int>> ?? new();
        }
    }

    public void SaveMarkovBinaryData()
    {
        using (var fs = new FileStream(_markovBinaryPath, FileMode.OpenOrCreate)) {
            var fmt = new BinaryFormatter();
            fmt.Serialize(fs, markovStrings);
        }
    }
}

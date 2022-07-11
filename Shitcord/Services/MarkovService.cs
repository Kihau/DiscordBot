using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Runtime.Serialization.Formatters.Binary;
using Shitcord.Extensions;

namespace Shitcord.Services;

public class MarkovService
{
    private DiscordClient Client { get; }
    private DatabaseService DatabaseContext { get; }
    private Random Rng { get; }
    static Dictionary<string, Dictionary<string, int>> markovStrings = new();
    private char[] _excludeCharacters = { '.', ',', ':', ';', '?', '!' }; 

    public bool IsEnabled { get; set; } = true;
    public bool GatherData { get; set; } = true;

    // TODO: Those will be stored for each guild (together with IsEnabled, GatherData, 
    // AutoResponseTimeout, EnableAutoResponse, ExcludedChannel(?), IncludedChannels(?))
    private const int min_len = 12;
    private const int max_len = 20;
    
    // TODO: A very lazy solution - store it in database later on
    private string _markovBinaryPath = "markov.bin";

    public MarkovService(Discordbot bot, DatabaseService database) 
    {
        Client = bot.Client;
        DatabaseContext = database;
        Rng = new Random();

        //Client.Ready += (sender, e) => {
        //    sender.MessageCreated += MarkovMessageHandler;
        //    return Task.CompletedTask;
        //};

        Client.MessageCreated += MarkovMessageHandler;
    }

    // NOTE: The prob_array must be sorted!
    private int CalculateRandomIndex(KeyValuePair<string, int>[] prob_array) {
        int fitness_sum = prob_array.Select(x => x.Value).Sum();
        double calc_probability = 0.0;
        
        var gen_probability = Rng.NextDouble();
        int index = 0;
        for (index = 0; index < prob_array.Length; index++) {
            calc_probability += (double)prob_array[index].Value / fitness_sum;

            if (gen_probability < calc_probability)
                break;
        }

        // Probably don't need this, but better safe than sorry.
        if (index == prob_array.Length)
            index--;

        return index;
    }

    // TODO: Detect if markov is repeating same strings
    public string GenerateMarkovString()
    {
        if (markovStrings.Count == 0)
            throw new CommandException("Markov is speechless. It needs to learn more");

        string generated_string = "";
        int current_len = 0;

        var startStrings = markovStrings.Keys.ToArray();
        var index = Rng.Next(startStrings.Length);
        
        var rand_key = startStrings[index];
        do {
            generated_string += rand_key + " ";

            if (markovStrings.TryGetValue(rand_key, out var nextDict)) {
                if (nextDict.Count != 0) {
                    var alignedDict = nextDict.OrderBy(x => x.Value).ToArray();
                    int found = CalculateRandomIndex(alignedDict);
                    rand_key = alignedDict[found].Key;
                } else if (current_len < min_len) {
                    index = Rng.Next(startStrings.Length);
                    rand_key = startStrings[index];
                } else break;
                
            } else if (current_len < min_len) {
                index = Rng.Next(startStrings.Length);
                rand_key = startStrings[index];
            } else break;
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
        if (!GatherData)
            return Task.CompletedTask;

        if (e.Author.IsBot)
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

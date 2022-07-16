using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Runtime.Serialization.Formatters.Binary;
using Shitcord.Extensions;
using Shitcord.Data;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Services;

public class MarkovService
{
    private DiscordClient Client { get; }
    private Dictionary<ulong, GuildMarkovData> MarkovData { get; } = new();
    private DatabaseService DatabaseContext { get; }
    private Random Rng { get; }
    // TODO(?): Discard old data
    static Dictionary<string, Dictionary<string, int>> markovStrings = new();
    private char[] _excludeCharacters = { '.', ',', ':', ';', '?', '!' }; 

    // TODO: Those will be stored for each guild (together with IsEnabled, GatherData, 
    // AutoResponseTimeout, EnableAutoResponse, ExcludedChannel(?), IncludedChannels(?))
    //private const int min_len = 12;
    //private const int max_len = 20;
    
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

    // NOTE: The prob_array must be sorted - otherwise the algorithm won't work!
    private int CalculateRandomIndex(KeyValuePair<string, int>[] prob_array) 
    {
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

    // TODO: Detect if markov is repeating same strings - 3 chains at least
    public string GenerateMarkovString(int min_len, int max_len)
    {
        if (markovStrings.Count == 0)
            throw new CommandException("Markov is speechless. It needs to learn more");

        string generated_string = "";
        int current_len = 0;

        var startStrings = markovStrings.Keys.ToArray();
        var index = Rng.Next(startStrings.Length);
        
        var rand_key = startStrings[index];
        do {
            if (generated_string.Length + rand_key.Length + 1 >= 2000)
                break;

            generated_string += rand_key + " ";

            if (markovStrings.TryGetValue(rand_key, out var nextDict)) {
                if (nextDict.Count != 0) {
                    // TODO: Check: Is this sorting correct? Can I optimalize it?
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

        //return $"{generated_string[0]}{generated_string.Remove(0, 1)}";
        return generated_string;
    }


    public bool ContainsBaseString(string base_string) 
    {
        return DatabaseContext.ExistsInTable(
            MarkovTable.TABLE_NAME, Condition
                .New(MarkovTable.BASE.name)
                .Equals(base_string)
        );
    }

    public bool ContainsChainString(string base_string, string chain_string) 
    {
        return DatabaseContext.ExistsInTable(
            MarkovTable.TABLE_NAME, Condition
                .New(MarkovTable.BASE.name)
                .Equals(base_string)
                .And(MarkovTable.CHAIN.name)
                .Equals(chain_string)
        );
    }

    public void InsertChainString(string base_string, string chain_string) 
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Insert()
            .Into(MarkovTable.TABLE_NAME)
            .Values(base_string, chain_string, 1)
            .Build()
        );
    }

    public void UpdateChainFrequency(string base_string, string chain_string)
    {
        var data = DatabaseContext.GatherData(QueryBuilder
            .New()
            .Retrieve(MarkovTable.FREQUENCY.name)
            .From(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE.name)
                .Equals(base_string)
                .And(MarkovTable.CHAIN.name)
                .Equals(chain_string)
            ).Build()
        );

        if (data is null) throw new Exception("Unreachable code");

        int freq = (int)data[0][0];

        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Update(MarkovTable.FREQUENCY.name)
            .Where(Condition
                .New(MarkovTable.BASE.name)
                .Equals(base_string)
                .And(MarkovTable.CHAIN.name)
                .Equals(chain_string)
            ).Set(MarkovTable.FREQUENCY.name, freq + 1)
            .Build()
        );
    }

    //var retrieved = DatabaseContext.GatherData(QueryBuilder
    //    .New()
    //    .Retrieve(MarkovTable.FREQUENCY.name)
    //    .Where(Condition
    //        .New(MarkovTable.BASE.name)
    //        .Equals(data[i])
    //        .And(MarkovTable.CHAIN.name)
    //        .Equals(data[i +1])
    //    ).Build()
    //);
    public void FeedStringsToMarkovNew(List<string> data) 
    {
        for (int i = 0; i < data.Count - 1; i++) {
            if (ContainsBaseString(data[i])) {
                if (ContainsChainString(data[i], data[i + 1]))
                    InsertChainString(data[i], data[i + 1]);
                else UpdateChainFrequency(data[i], data[i + 1]);
            } // And here is the problem - when inserting new base string, there are no chain 
              // string associated with it and we cannot create only the base string (since one 
              // row has 3 columns associated with it)
        }
    }

    public void FeedStringsToMarkov(List<string> data) 
    {
        for (int i = 0; i < data.Count - 1; i++) {
            // TODO(?): Do not store chain string if the next value is the same as the previous one 
            if (markovStrings.TryGetValue(data[i], out var nextDict)) {
                if (nextDict.ContainsKey(data[i + 1]))
                    nextDict[data[i + 1]]++;
                else nextDict.Add(data[i + 1], 1);
            } else markovStrings.Add(data[i--], new());
        }
        markovStrings.TryAdd(data[data.Count - 1], new());
    }

    public GuildMarkovData GetOrAddData(DiscordGuild guild)
    {
        if (MarkovData.TryGetValue(guild.Id, out var data))
            return data;

        data = new GuildMarkovData();
        MarkovData.Add(guild.Id, data);

        return data;
    }

    // TODO(?): Ignore strings that start with the bot prefix
    private async Task MarkovMessageHandler(DiscordClient client, MessageCreateEventArgs e)
    {
        if (e.Author.IsBot)
            return;

        var data = GetOrAddData(e.Guild);

        if (!data.IsEnabled)
            return;

        string input = e.Message.Content;
        // When the bot is tagged, respond with a markov message
        if (input.StartsWith(Client.CurrentUser.Mention)) { 
            var response = GenerateMarkovString(data.MinChainLength, data.MaxChainLength);
            await e.Message.RespondAsync(response);
            return;
        }

        // Do not gather data from channels excluded by the user
        if (data.ExcludedChannelIDs.Contains(e.Channel.Id))
            return;

        // NOTE: Max word length is set to 64 chars
        List<string> parsed_input = input.Split(
            new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries
        ).Where(x => x.Length <= 64).ToList();

        // TODO (?): Some logic to remove unnesessary characters
        /*
        for (int i = 0; i < data.Count; i++) {
            if (_excludeCharacters.Contains(data[i].Last()))
                data[i] = data[i].Substring(0, data[i].Length - 2);

            if (_excludeCharacters.Contains(data[i].First()))
                data[i] = data[i].Substring(1);
        }
        */

        FeedStringsToMarkov(parsed_input);

        // Logic to auto respond to user messages with markov strings
        if (!data.ResponseEnabled)
            return;

        var time = DateTime.Now - data.LastResponse;
        if (time < data.ResponseTimeout)
            return;

        var rolled_chance = Rng.Next(GuildMarkovData.MAX_CHANCE);
        if (data.ResponseChance >= rolled_chance) {
            data.LastResponse = DateTime.Now;
            var markov_text = GenerateMarkovString(data.MinChainLength, data.MaxChainLength);

            var direct_respond = Rng.Next(2) == 1;
            if (direct_respond) 
                await e.Message.RespondAsync(markov_text);
            else await e.Channel.SendMessageAsync(markov_text);
        }
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

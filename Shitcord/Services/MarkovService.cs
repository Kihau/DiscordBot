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

    public string[] GetAllBaseStrings() 
    {
        var all_values = DatabaseContext.GatherData(
            QueryBuilder.New().Retrieve(MarkovTable.BASE.name).From(MarkovTable.TABLE_NAME).Build()
        );

        if (all_values is null)
            return new string[] {};
        
        var base_strings = all_values.Select(x => (string)x[0]).ToArray();
        return base_strings;
    }

    public (string, int)[] GetAllChainFrequency(string base_string) 
    {
        var all_values = DatabaseContext.GatherData(QueryBuilder
            .New()
            .Retrieve(MarkovTable.CHAIN.name, MarkovTable.FREQUENCY.name)
            .From(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE.name)
                .Equals(base_string)
            ).Build()
        );

        if (all_values is null)
            return new (string, int)[] {};

        List <(string, int)> chain_freq_list = new();
        for (int i = 0; i < all_values[0].Count; i++)
            chain_freq_list.Add(((string, int))(all_values[0][i], (long)all_values[1][i]));

        return chain_freq_list.ToArray();
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
        // Remove old base_string
        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Delete()
            .From(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE.name)
                .Equals(base_string)
                .And(MarkovTable.FREQUENCY.name)
                .Equals(0)
            ).Build()
        );

        // Old base_string is now replaced with one that is
        // associated to chain_string and frequency
        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Insert()
            .Into(MarkovTable.TABLE_NAME)
            .Values(base_string, chain_string, 1)
            .Build()
        );
    }

    public void InsertNewBaseString(string base_string) 
    {
        // Add default base string to remove it later
        // (this is not that good)
        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Insert()
            .Into(MarkovTable.TABLE_NAME)
            .Values(base_string, "", 0)
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

        int freq = (int)(long)data[0][0];

        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Update(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE.name)
                .Equals(base_string)
                .And(MarkovTable.CHAIN.name)
                .Equals(chain_string)
            ).Set(MarkovTable.FREQUENCY.name, freq + 1)
            .Build()
        );
    }

    // NOTE: The prob_array must be sorted - otherwise the algorithm won't work!
    private int CalculateRandomIndex((string, int)[] prob_array) 
    {
        int fitness_sum = prob_array.Select(x => x.Item2).Sum();
        double calc_probability = 0.0;
        
        var gen_probability = Rng.NextDouble();
        int index = 0;
        for (index = 0; index < prob_array.Length; index++) {
            calc_probability += (double)prob_array[index].Item2 / fitness_sum;
            if (gen_probability < calc_probability)
                break;
        }

        // Probably don't need this, but better safe than sorry.
        if (index == prob_array.Length)
            index--;

        return index;
    }

    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // TODO: SQL DISTINCT IN QUERIES
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // also rename base strings to key strings (maybe)
    // also query to get items already sorted
    
    // TODO: Detect if markov is repeating same strings - 3 chains at least
    public string GenerateMarkovString(int min_len, int max_len)
    {
        var base_strings = GetAllBaseStrings();

        if (base_strings.Length == 0)
            throw new CommandException("Markov is speechless. It needs to learn more");

        string generated_string = "";
        int current_len = 0;

        var next = Rng.Next(base_strings.Length);
        var rand_base = base_strings[next];

        do {
            if (generated_string.Length + rand_base.Length + 1 >= 2000)
                break;

            generated_string += rand_base + " ";

            if (ContainsBaseString(rand_base)) {
                var chain_freq = GetAllChainFrequency(rand_base);
                if (chain_freq.Length != 0) {
                    Array.Sort(chain_freq, (a, b) => a.Item2.CompareTo(b.Item2));
                    int index = CalculateRandomIndex(chain_freq);
                    rand_base = chain_freq[index].Item1;
                } else if (current_len < min_len) {
                    next = Rng.Next(base_strings.Length);
                    rand_base = base_strings[next];
                } else break;
                
            } else if (current_len < min_len) {
                next = Rng.Next(base_strings.Length);
                rand_base = base_strings[next];
            } else break;
            
        } while (current_len++ < max_len);

        //return $"{generated_string[0]}{generated_string.Remove(0, 1)}";
        return generated_string;
    }

    public void FeedStringsToMarkov(List<string> data) 
    {
        // TODO: Last potential base string is discarded - fix it
        for (int i = 0; i < data.Count - 1; i++) {
            // TODO(?): Do not store chain string if the next value is the same as the previous one 
            if (ContainsBaseString(data[i])) {
                if (ContainsChainString(data[i], data[i + 1]))
                    UpdateChainFrequency(data[i], data[i + 1]);
                else InsertChainString(data[i], data[i + 1]);
            } else InsertNewBaseString(data[i--]); 
        }

        if (!ContainsBaseString(data[data.Count - 1]))
            InsertNewBaseString(data[data.Count - 1]); 
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

        // NOTE: Max word length is set to 255 chars
        List<string> parsed_input = input.Split(
            new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries
        ).Where(x => x.Length <= 255).ToList();

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

    [Obsolete]
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

    [Obsolete]
    public void SaveMarkovBinaryData()
    {
        using (var fs = new FileStream(_markovBinaryPath, FileMode.OpenOrCreate)) {
            var fmt = new BinaryFormatter();
            fmt.Serialize(fs, markovStrings);
        }
    }
}

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
    private char[] _excludeCharacters = { '.', ',', ':', ';', '?', '!' }; 

    public MarkovService(DiscordBot bot, DatabaseService database) 
    {
        Client = bot.Client;
        DatabaseContext = database;
        Rng = new Random();

        Client.MessageCreated += MarkovMessageHandler;
    }

    // NOTE: We can simplify this to a simple DISTINCT query if that is needed
    public string[] GetAllBaseStrings() 
    {
        var all_columns = DatabaseContext.RetrieveColumns(
            QueryBuilder.New().Retrieve(MarkovTable.BASE).Distinct()
            .From(MarkovTable.TABLE_NAME).Build()
        );

        if (all_columns is null)
            throw new UnreachableException();

        var base_strings = all_columns[0].Select(row => (string)(row ?? "")).ToArray();
        return base_strings;
    }

    public (string, int)[] GetAllChainFrequency(string base_string) 
    {
        var all_columns = DatabaseContext.RetrieveColumns(QueryBuilder
            .New()
            .Retrieve(MarkovTable.CHAIN, MarkovTable.FREQUENCY)
            .From(MarkovTable.TABLE_NAME)
            .WhereEquals(MarkovTable.BASE, base_string)
            .OrderBy(MarkovTable.FREQUENCY)
            .Build()
        );

        if (all_columns is null)
            throw new UnreachableException();

        List <(string, int)> chain_freq_list = new();
        for (int i = 0; i < all_columns[0].Count; i++) {
            chain_freq_list.Add(
                ((string, int))(all_columns[0][i] ?? "", (long)(all_columns[1][i] ?? 0))
            );
        }

        return chain_freq_list.ToArray();
    }

    public bool ContainsBaseString(string base_string) 
    {
        return DatabaseContext.ExistsInTable(
            MarkovTable.TABLE_NAME, Condition
                .New(MarkovTable.BASE)
                .Equals(base_string)
        );
    }

    public bool ContainsChainString(string base_string, string chain_string) 
    {
        return DatabaseContext.ExistsInTable(
            MarkovTable.TABLE_NAME, Condition
                .New(MarkovTable.BASE)
                .Equals(base_string)
                .And(MarkovTable.CHAIN)
                .Equals(chain_string)
        );
    }

    public void InsertChainString(string base_string, string chain_string) 
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Update(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE)
                .Equals(base_string)
                .And(MarkovTable.FREQUENCY)
                .Equals(0)
            ).Set(MarkovTable.BASE, base_string)
            .Set(MarkovTable.CHAIN, chain_string)
            .Set(MarkovTable.FREQUENCY, 1)
            .Build()
        );
    }

    public void InsertAllStrings(string base_string, string chain_string, int frequency) 
    {
        var query = QueryBuilder
            .New()
            .Insert()
            .Into(MarkovTable.TABLE_NAME)
            .Values(base_string, chain_string, frequency)
            .Build();

        Console.WriteLine(query);
        DatabaseContext.executeUpdate(query);
    }

    public void InsertNewBaseString(string base_string) 
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Insert()
            .Into(MarkovTable.TABLE_NAME)
            .Values(base_string, "", 0)
            .Build()
        );
    }

    // TODO: Increment query (increment by 1 in this case)
    public void UpdateChainFrequency(string base_string, string chain_string)
    {
        var columns = DatabaseContext.RetrieveColumns(QueryBuilder
            .New()
            .Retrieve(MarkovTable.FREQUENCY)
            .From(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE)
                .Equals(base_string)
                .And(MarkovTable.CHAIN)
                .Equals(chain_string)
            ).Build()
        );

        if (columns is null) throw new UnreachableException();

        int freq = (int)(long)(columns[0][0] ?? 0);

        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Update(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE)
                .Equals(base_string)
                .And(MarkovTable.CHAIN)
                .Equals(chain_string)
            ).Set(MarkovTable.FREQUENCY, freq + 1)
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

    // TODO(?): Rename base strings to key strings (maybe).
    
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

        if (data.Count > 0 && !ContainsBaseString(data[data.Count - 1]))
            InsertNewBaseString(data[data.Count - 1]); 
    }

    public GuildMarkovData GetOrAddData(DiscordGuild guild)
    {
        if (MarkovData.TryGetValue(guild.Id, out var data))
            return data;

        data = new GuildMarkovData(guild, DatabaseContext);
        MarkovData.Add(guild.Id, data);

        return data;
    }

    // TODO(?): Ignore strings that start with the bot prefix
    private Task MarkovMessageHandler(DiscordClient client, MessageCreateEventArgs e)
    {
        Task.Run(async () => {
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
        });

        return Task.CompletedTask;
    }
}

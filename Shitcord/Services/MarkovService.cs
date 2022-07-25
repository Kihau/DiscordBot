using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Shitcord.Extensions;
using Shitcord.Data;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Services;

// TODO: Implement guilds + global
public class MarkovService
{
    private DiscordClient Client { get; }
    private BotConfig Config { get; }
    private Dictionary<ulong, GuildMarkovData> MarkovData { get; } = new();
    private DatabaseService DatabaseContext { get; }
    private Random Rng { get; }
    private char[] _excludeCharacters = { '.', ',', ':', ';', '?', '!' }; 

    public MarkovService(DiscordBot bot, DatabaseService database)
    {
        Config = bot.Config;
        Client = bot.Client;
        DatabaseContext = database;
        Rng = new Random();

        Client.MessageCreated += MarkovMessageHandler;
    }

    public void ClearCoruptedStrings() 
    {
        DatabaseContext.executeUpdate(@$"
            DELETE FROM {MarkovTable.TABLE_NAME}
            WHERE rowid NOT IN (
                SELECT MIN(rowid)
                FROM {MarkovTable.TABLE_NAME}
                GROUP BY
                    {MarkovTable.BASE.name}, 
                    {MarkovTable.CHAIN.name}, 
                    {MarkovTable.FREQUENCY.name}
            )
        ");
    }

    public void RemoveAnyString(string to_be_remove) 
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New().Delete().From(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE).Equals(to_be_remove)
                .Or(MarkovTable.CHAIN).Equals(to_be_remove)
            ).Build()
        );
    }

    public string GetRandomBaseString()
    {
        var all_columns = DatabaseContext.RetrieveColumns(
            QueryBuilder.New().Retrieve(MarkovTable.BASE)
                .Distinct().Random().Limit()
                .From(MarkovTable.TABLE_NAME).Build()
        );

        if (all_columns is null)
            throw new UnreachableException("No elements in markov table");

        return (string)all_columns[0][0]!;
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
    
    public bool ContainsAnyChainString(string base_string) 
    {
        return DatabaseContext.ExistsInTable(
            MarkovTable.TABLE_NAME, Condition
                .New(MarkovTable.BASE)
                .Equals(base_string)
                .And(MarkovTable.FREQUENCY)
                .IsMoreThan(0)
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

    public void UpdateChainFrequency(string base_string, string chain_string)
    {
        DatabaseContext.executeUpdate(QueryBuilder
            .New()
            .Update(MarkovTable.TABLE_NAME)
            .Where(Condition
                .New(MarkovTable.BASE)
                .Equals(base_string)
                .And(MarkovTable.CHAIN)
                .Equals(chain_string)
            ).SetIncrementBy(MarkovTable.FREQUENCY, 1)
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

    public string GenerateMarkovString(int min_len, int max_len)
    {
        string generated_string = "";
        int current_len = 0;

        var rand_base = GetRandomBaseString();

        do {
            if (generated_string.Length + rand_base.Length + 1 >= 2000)
                break;

            generated_string += rand_base + " ";

            if (ContainsAnyChainString(rand_base)) {
                var chain_freq_array = GetAllChainFrequency(rand_base);
                if (chain_freq_array.Length != 0) {
                    int index = CalculateRandomIndex(chain_freq_array);
                    rand_base = chain_freq_array[index].Item1;
                } else if (current_len < min_len) {
                    rand_base = GetRandomBaseString();
                } else break;
                
            } else if (current_len < min_len) {
                rand_base = GetRandomBaseString();
            } else break;
            
        } while (current_len++ < max_len);

        return generated_string;
    }

    public void FeedStringsToMarkov(List<string> data) 
    {
        for (int i = 0; i < data.Count - 1; i++) {
            if (ContainsBaseString(data[i])) {
                if (ContainsChainString(data[i], data[i + 1]))
                    UpdateChainFrequency(data[i], data[i + 1]);
                else InsertChainString(data[i], data[i + 1]);
            } else InsertNewBaseString(data[i--]); 
        }

        if (data.Count > 0 && !ContainsBaseString(data.Last()))
            InsertNewBaseString(data.Last()); 
    }

    public GuildMarkovData GetOrAddData(DiscordGuild guild)
    {
        if (MarkovData.TryGetValue(guild.Id, out var data))
            return data;

        data = new GuildMarkovData(guild, DatabaseContext);
        MarkovData.Add(guild.Id, data);

        return data;
    }
    
    private Task MarkovMessageHandler(DiscordClient client, MessageCreateEventArgs e)
    {
        Task.Run(async () => {
            if (e.Author.IsBot)
                return;

            var data = GetOrAddData(e.Guild);

            if (!data.IsEnabled)
                return;

            string input = e.Message.Content.Trim();
            // Ignore strings that start with the bot prefix
            if (input.StartsWith(Config.Discord.Prefix)) 
                return;
            
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

            // Some logic to remove unnecessary characters
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

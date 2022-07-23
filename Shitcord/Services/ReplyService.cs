using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Shitcord.Extensions;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Services;

public enum MatchMode : int {
    Any = 0, First = 1, Exact = 2
}

public class AutoReplyData
{
    public string match;
    public string reply;
    public MatchMode mode;
    public bool match_case;

    public AutoReplyData(string m, string r, MatchMode md, bool mc)
    {
        match = m;
        reply = r;
        mode = md; 
        match_case = mc;
    }
}

public class AutoReplyService
{
    public DiscordClient Client { get; init; }
    public DatabaseService Database { get; }
    private Dictionary<ulong, List<AutoReplyData>> ReplyDataSet { get; }

    public AutoReplyService(DiscordBot bot, DatabaseService database)
    {
        Client = bot.Client;
        Database = database;
        ReplyDataSet = new Dictionary<ulong, List<AutoReplyData>>();

        Client.MessageCreated += ReplyMessageHandler;
        Client.GuildDownloadCompleted += (_, _) => {
            Task.Run(LoadDataFromDatabase);
            return Task.CompletedTask;
        };
    }

    private void LoadDataFromDatabase() 
    {
        var data = Database.RetrieveColumns(QueryBuilder
            .New().Retrieve(
                AutoReplyTable.GUILD_ID, 
                AutoReplyTable.MATCH, 
                AutoReplyTable.REPLY,
                AutoReplyTable.MODE,
                AutoReplyTable.CASE
            ).From(AutoReplyTable.TABLE_NAME)
            .Build()
        );

        if (data is null) 
            return;

        for (int i = 0; i < data[0].Count; i++) {
            var guild_id = (ulong)(long)data[0][i]!;
            var match = (string)data[1][i]!;
            var reply = (string)data[2][i]!;
            var mode = (MatchMode)(long)data[3][i]!;
            var match_case = (long)data[4][i]! == 1;

            if (!ReplyDataSet.ContainsKey(guild_id))
                ReplyDataSet.Add(guild_id, new List<AutoReplyData>());

            var auto_reply_data = new AutoReplyData(match, reply, mode, match_case);
            var item = ReplyDataSet[guild_id];
            item.Add(auto_reply_data);
        }
    }

    private async Task ReplyMessageHandler(DiscordClient client, MessageCreateEventArgs args)
    {
        if (!ReplyDataSet.ContainsKey(args.Guild.Id))
            return;

        var dataset = ReplyDataSet[args.Guild.Id];
        foreach (var data in dataset) {
            StringComparison cmp;
            if (data.match_case) cmp = StringComparison.OrdinalIgnoreCase;
            else cmp = StringComparison.Ordinal;

            var msg = args.Message.Content;
            bool found_match = data.mode switch {
                MatchMode.Any => msg.Contains(data.match, cmp), 
                MatchMode.Exact => data.match_case ?
                    msg.ToLower() == data.match.ToLower() : msg == data.match,
                MatchMode.First => msg.StartsWith(data.match, cmp),
                _ => false,
            };

            if (found_match) {
                await args.Message.RespondAsync(data.reply);
                return;
            }
        }
    }

    public void AddReplyData(DiscordGuild guild, AutoReplyData data)
    {
        if (!ReplyDataSet.ContainsKey(guild.Id))
            ReplyDataSet.Add(guild.Id, new List<AutoReplyData>());

        var item = ReplyDataSet[guild.Id];
        if (!item.Any(x => x.match == data.match)) {
            item.Add(data);

            Database.executeUpdate(QueryBuilder
                .New().Insert().Into(AutoReplyTable.TABLE_NAME)
                .Values(guild.Id, data.match, data.reply, (int)data.mode, data.match_case)
                .Build()
            );
        } else throw new CommandException("Match already exists in the dataset");
    }

    public void RemoveAllReplyData(DiscordGuild guild)
    {
        if (!ReplyDataSet.ContainsKey(guild.Id))
            throw new CommandException("Reply list is already empty");

        var dataset = ReplyDataSet[guild.Id];
        dataset.Clear();

        Database.executeUpdate(QueryBuilder
            .New().Delete().From(AutoReplyTable.TABLE_NAME)
            .WhereEquals(AutoReplyTable.GUILD_ID, guild.Id)
            .Build()
        );
    }

    public void RemoveReplyData(DiscordGuild guild, string match)
    {
        if (!ReplyDataSet.ContainsKey(guild.Id))
            throw new CommandException("Reply list is empty");

        var dataset = ReplyDataSet[guild.Id];
        AutoReplyData? found = null;

        foreach (var data in dataset)
            if (data.match == match)
                found = data;

        if (found == null)
            throw new CommandException("Match not found.");
        else dataset.Remove(found);

        Database.executeUpdate(QueryBuilder
            .New().Delete().From(AutoReplyTable.TABLE_NAME)
            .Where(Condition
                .New(AutoReplyTable.GUILD_ID).Equals(guild.Id)
                .And(AutoReplyTable.MATCH).Equals(found.match)
            ).Build()
        );
    }

    public void RemoveReplyDataAt(DiscordGuild guild, int index)
    {
        if (!ReplyDataSet.ContainsKey(guild.Id))
            throw new CommandException("Reply list is empty");

        var dataset = ReplyDataSet[guild.Id];
        if (index > 0 && index < dataset.Count) {
            Database.executeUpdate(QueryBuilder
                .New().Delete().From(AutoReplyTable.TABLE_NAME)
                .Where(Condition
                    .New(AutoReplyTable.GUILD_ID).Equals(guild.Id)
                    .And(AutoReplyTable.MATCH).Equals(dataset[index].match)
                ).Build()
            );
            dataset.RemoveAt(index);
        }
        else throw new CommandException("Given index is incorrect");
    }

    public IReadOnlyList<AutoReplyData>? GetReplyData(DiscordGuild guild)
        => ReplyDataSet.GetValueOrDefault(guild.Id);
}

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Shitcord.Extensions;
using Shitcord.Database;
using Shitcord.Database.Queries;

namespace Shitcord.Services;

public struct AutoReplyData
{
    public string match;
    public string response;

    public AutoReplyData(string m, string r)
    {
        match = m;
        response = r;
    }
}

public class AutoReplyService
{
    public DiscordClient Client { get; init; }
    public DatabaseService Database { get; }
    private Dictionary<ulong, List<AutoReplyData>> ReplyDataSet { get; init; }

    public AutoReplyService(DiscordBot bot, DatabaseService database)
    {
        Client = bot.Client;
        Database = database;
        ReplyDataSet = new Dictionary<ulong, List<AutoReplyData>>();

        Client.MessageCreated += ReplyMessageHandler;
        Client.GuildDownloadCompleted += (_, _) => {
            Task.Run(() => LoadDataFromDatabase());
            return Task.CompletedTask;
        };
    }

    private void LoadDataFromDatabase() 
    {
        var data = Database.RetrieveColumns(QueryBuilder
            .New().Retrieve(
                AutoReplyTable.GUILD_ID, 
                AutoReplyTable.MATCH, 
                AutoReplyTable.RESPONSE
            ).From(AutoReplyTable.TABLE_NAME)
            .Build()
        );

        if (data is null) return;

        for (int i = 0; i < data.Count; i++) {
            var guild_id = (ulong)(long)data[i][0]!;
            var match = (string)data[i][1]!;
            var response = (string)data[i][2]!;

            if (!ReplyDataSet.ContainsKey(guild_id))
                ReplyDataSet.Add(guild_id, new List<AutoReplyData>());

            var auto_reply_data = new AutoReplyData(match, response);
            var item = ReplyDataSet[guild_id];
            if (!item.Contains(auto_reply_data))
                item.Add(auto_reply_data);
        }
    }

    public async Task ReplyMessageHandler(DiscordClient client, MessageCreateEventArgs args)
    {
        if (!ReplyDataSet.ContainsKey(args.Guild.Id))
            return;

        var dataset = ReplyDataSet[args.Guild.Id];
        foreach (var data in dataset) {
            var msg = args.Message.Content.ToLower();
            if (msg.Contains(data.match, StringComparison.OrdinalIgnoreCase)) {
                await args.Message.RespondAsync(data.response);
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
                .Values(guild.Id, data.match, data.response)
                .Build()
            );
        } else throw new CommandException("Match already exists in the dataset");
    }


    public void RemoveAllReplyData(DiscordGuild guild)
    {
        if (!ReplyDataSet.ContainsKey(guild.Id))
            throw new CommandException("Response list is already empty");

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
            throw new CommandException("Response list is empty");

        var dataset = ReplyDataSet[guild.Id];
        AutoReplyData? found = null;

        foreach (var data in dataset)
            if (data.match == match)
                found = data;

        if (found == null)
            throw new CommandException("Match not found.");
        else dataset.Remove(found.Value);

        Database.executeUpdate(QueryBuilder
            .New().Delete().From(AutoReplyTable.TABLE_NAME)
            .Where(Condition
                .New(AutoReplyTable.GUILD_ID).Equals(guild.Id)
                .And(AutoReplyTable.MATCH).Equals(found.Value)
            ).Build()
        );
    }

    public void RemoveReplyDataAt(DiscordGuild guild, int index)
    {
        if (!ReplyDataSet.ContainsKey(guild.Id))
            throw new CommandException("Response list is empty");

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

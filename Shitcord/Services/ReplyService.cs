using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Shitcord.Extensions;

namespace Shitcord.Services;

public struct ReplyData
{
    public string match;
    public string response;

    public ReplyData(string m, string r)
    {
        match = m;
        response = r;
    }
}

public class ReplyService
{
    public DiscordClient Client { get; init; }
    private Dictionary<ulong, List<ReplyData>> ReplyDataSet { get; init; }

    public ReplyService(Discordbot bot)
    {
        this.Client = bot.Client;
        this.ReplyDataSet = new Dictionary<ulong, List<ReplyData>>();

        this.Client.MessageCreated += ReplyMessageHandler;
    }

    // TODO: Add interaction handler ???

    public async Task ReplyMessageHandler(DiscordClient client, MessageCreateEventArgs args)
    {
        if (!this.ReplyDataSet.ContainsKey(args.Guild.Id))
            return;

        var dataset = this.ReplyDataSet[args.Guild.Id];
        foreach (var data in dataset)
        {
            var msg = args.Message.Content.ToLower();
            if (msg.Contains(data.match, StringComparison.OrdinalIgnoreCase))
            {
                await args.Message.RespondAsync(data.response);
                return;
            }
        }
    }

    public void AddReplyData(DiscordGuild guild, ReplyData data)
    {
        if (!this.ReplyDataSet.ContainsKey(guild.Id))
        {
            this.ReplyDataSet.Add(guild.Id, new List<ReplyData>());
        }

        var item = this.ReplyDataSet[guild.Id];
        if (!item.Contains(data))
            item.Add(data);
    }

    public void RemoveReplyData(DiscordGuild guild, string match)
    {
        if (!this.ReplyDataSet.ContainsKey(guild.Id))
            throw new CommandException("Response list is empty");

        var dataset = this.ReplyDataSet[guild.Id];
        ReplyData? found = null;

        foreach (var data in dataset)
        {
            if (data.match == match)
            {
                found = data;
                break;
            }
        }

        if (found == null)
            throw new CommandException("Response not found.");
        else dataset.Remove(found.Value);
    }

    public void RemoveReplyDataAt(DiscordGuild guild, int index)
    {
        if (!this.ReplyDataSet.ContainsKey(guild.Id))
            throw new CommandException("Response list is empty");

        var dataset = this.ReplyDataSet[guild.Id];
        if (index > 0 && index < dataset.Count)
            dataset.RemoveAt(index);
        else throw new CommandException("Given index is incorrect");
    }

    public IReadOnlyList<ReplyData>? GetReplyData(DiscordGuild guild)
        => this.ReplyDataSet.GetValueOrDefault(guild.Id);
}

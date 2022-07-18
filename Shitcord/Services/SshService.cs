using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Renci.SshNet;
using Shitcord.Data;

namespace Shitcord.Services;

public class SshService
{
    public DiscordClient Client { get; init; }
    public SshClient Ssh { get; private set; }
    public SshConfig Config { get; init; }

    public bool IsEnabled => this.Config.IsEnabled;
    
#pragma warning disable CS8618
    public SshService(DiscordBot bot)
#pragma warning restore CS8618
    {
        this.Client = bot.Client;
        this.Config = bot.Config.Ssh;

        if (this.IsEnabled)
            this.Client.Ready += Client_Ready;
    }

    private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
    {
        var sshConf = new ConnectionInfo(
            Config.Hostname, Config.Port, Config.Username,
            new PrivateKeyAuthenticationMethod(
                Config.Username, new PrivateKeyFile(Config.Keyfile)
            )
        );

        this.Ssh = new SshClient(sshConf);

        // TODO: Add ssh handlers and other things in here
        this.Ssh.Connect();
        sender.MessageCreated += SshCommandHandler;
        
        return Task.CompletedTask;
    }
    
    private Task SshCommandHandler(DiscordClient client, MessageCreateEventArgs e)
    {
        var cnext = client.GetCommandsNext();
        var msg = e.Message;

        var linuxCmd = msg.GetStringPrefixLength("$$");
        if (linuxCmd != -1)
        {
            var cmdString = msg.Content.Substring(linuxCmd);
            // Only execute commands from kihau & frisk
            if (e.Author.Id == 278778540554715137 ||
                (e.Author.Id == 790507097615237120 && !cmdString.Trim().StartsWith("sudo")))
                Task.Run(() => this.ExecuteSshCmd(e.Channel, cmdString));
            return Task.CompletedTask;
        }
    
        return Task.CompletedTask;
    }

    private string _pwd = "";
    private void ExecuteSshCmd(DiscordChannel channel, string command)
    {
        _pwd = this.Ssh.RunCommand("cat .current").Result.Trim();
        
        var con_comm = $"cd {_pwd} && {command} && echo $PWD > .currentu && pwd && echo $PWD";
        var result = this.Ssh.RunCommand(con_comm).Result;
        _pwd = this.Ssh.RunCommand("cat .currentu").Result.Trim();
        
        var message = $"```kihau@{_pwd}\n---\n{result}```";
        channel.SendMessageAsync(message);
    }
}

using System.Diagnostics;
using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Shitcord.Data;
using Shitcord.Extensions;
using ExtensionMethods = Shitcord.Extensions.ExtensionMethods;

namespace Shitcord.Modules;

[Description("Fun and silly commands")]
public class FunModule : BaseCommandModule 
{
    [Command("hlep")]
    public async Task HlepCommand(CommandContext ctx, [RemainingText] string? ignored = null) 
        => await ctx.RespondAsync("https://tenor.com/view/falling-bread-bread-gif-19081960");

    [Command("httpcat"), Aliases("http")]
    [Description("Get http error reply")]
    public async Task HttpErrorCommand(CommandContext ctx, int code) {
        if (ExtensionMethods.WebConnectionOk($"https://http.cat/{code}"))
            await ctx.RespondAsync($"https://http.cat/{code}");
        else throw new CommandException("404 - HttpCat not found");
    }


    [Command("vanish")]
    [Description("You and your messages go *POOF*")]
    public async Task VanishCommand(CommandContext ctx, int counter = 10) {
        var to_delete = new List<DiscordMessage>();
        var messages = await ctx.Channel.GetMessagesAsync();
        for (int i = 0; i < messages.Count && counter > 0; i++) {
            var msg = messages[i];
            if (msg.Author.Id == ctx.Message.Author.Id) {
                to_delete.Add(msg);
                counter -= 1;
            }
        }
        await ctx.Channel.DeleteMessagesAsync(to_delete);
    }

    [Command("mcseed")]
    public async Task MCSeedCommand(CommandContext ctx) 
        => await ctx.RespondAsync(new Random().NextInt64().ToString());

    [Command("solarlesson"), Aliases("solar", "lesson")] 
    public async Task SolarLessonCommand(CommandContext ctx) {
        int index = new Random().Next(FunModule.solarlessons.Length);
        await ctx.RespondAsync(FunModule.solarlessons[index]);
    }

    // [Command("runscript"), Aliases("script", "python")]
    // async Task RunSciptCommand(
    //     CommandContext ctx, string scipt_name, [RemainingText] string? input_args = null
    // ) {
    // }

    [Command("mcwhitelist"), Aliases("whitelist")]
    public async Task McWhitelistCommand(CommandContext ctx, string username) {
        // await ctx.Message.DeleteAsync();
        const ulong temp_id = 1035654547659554919; // mcwhitelist
        if (ctx.Channel.Id != temp_id)
            throw new CommandException("Whitelisting here is not allowed.");

        if (GlobalData.mc_whitelist.Any(x => x.username == username))
            throw new CommandException("Username like this is already whitelisted.");

        var server = new Process();
        server.StartInfo = new ProcessStartInfo {
            FileName = $"bash",
            WorkingDirectory = GlobalData.mcserver_path,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };

        string output = "";
        var first = GlobalData.mc_whitelist.Where(x => x.userid == ctx.User.Id).FirstOrDefault();
        if (first != null) {
            server.StartInfo.Arguments =
                $"./startup.sh whitelist change {first.username} {username}";

            first.username = username;
            output = "Account changed";
            
        } else {
            GlobalData.mc_whitelist.Add(new WhitelistEntry {
                userid = ctx.User.Id,
                username = username,
            });

            server.StartInfo.Arguments = $"./startup.sh whitelist add {username}";
            output = "Added to whitelist";
        }

        server.Start();
        await server.WaitForExitAsync();

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(GlobalData.mc_whitelist, options);
        File.WriteAllText(GlobalData.whitelist_path, json);

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("Command executed");
        embed.WithDescription(output);
        embed.WithColor(DiscordColor.Blue);

        // var message = await ctx.RespondAsync(output);
        var message = await ctx.RespondAsync(embed.Build());

        // await Task.Delay(3000);
        // await message.DeleteAsync();
    }

    [Command("mcexec"), RequireAuthorized]
    public async Task McExecCommand(CommandContext ctx, [RemainingText] string command) {
        var server = new Process();
        server.StartInfo = new ProcessStartInfo {
            FileName = $"bash",
            WorkingDirectory = GlobalData.mcserver_path,
            Arguments = $"./startup.sh mcexec \"{command}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };

        server.Start();
        await server.WaitForExitAsync();

        var output = await server.StandardOutput.ReadLineAsync() 
            ?? throw new CommandException("Failed to read from the process.");
        await ctx.RespondAsync(output);
    }

    [Command("mcstart"), Description("Starts the minecraft server")]
    public async Task McStartCommand(CommandContext ctx) {
        if (!GlobalData.mc_whitelist.Any(x => x.userid == ctx.User.Id))
            throw new CommandException("You are not whitelisted. Unlucky...");
        
        var server = new Process();
        server.StartInfo = new ProcessStartInfo {
            FileName = $"bash",
            WorkingDirectory = GlobalData.mcserver_path,
            Arguments = "-c \"./startup.sh start\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };

        server.Start();
        await server.WaitForExitAsync();

        var output = await server.StandardOutput.ReadLineAsync() 
            ?? throw new CommandException("Failed to read from the process.");
        await ctx.RespondAsync(output);
    }

    [Command("shakeuser"), Aliases("shake")]
    public async Task ShakeUserCommand(CommandContext ctx, DiscordMember member) 
    {
        var vchannels = ctx.Guild.Channels
            .Where(x => x.Value.Type == ChannelType.Voice)
            .Select(x => x.Value)
            .ToArray();

        if (vchannels.Length == 0)
            throw new CommandException("There is no voice channels?");

        var rng = new Random();
        for (int i = 0; i < 10; i++) {
            var rng_index = rng.Next(vchannels.Length);
            await member.ModifyAsync(x => x.VoiceChannel = vchannels[rng_index]);
        }
    }

    // Lessons from "solar opposites" series
    static string[] solarlessons = {
        "Don't count your chickens before they hatch.",
        "Beer before liquor, never sicker.",
        "Ass, grass, or gas. Nobody rides for free.",
        "The best way to cope with stress is to confront it. Only then can you truly be happy or at least satisfied a little bit.",
        "Sometimes emotional stories don't fit into a rigid structure and you just have to embrace the chaos and not worry about it… as a family!",
        "A penny saved is a penny earned.",
        "Never press the big red button.",
        "Always bet on red.",
        "A little food, a little exercise, a lot of water.",
        "Check the estimated travel time the night before.",
        "If I can change, and you can change, we all can change.",
        "Nobody beats the wiz.",
        "No matter where you go, there you are.",
        "Homeownership is a scam.",
        "It's touCAN, not touCAN't.",
        "A moment on the lips, a lifetime on the hips.",
        "The greatest thing you'll ever learn is just to love and be loved in return.",
        "Whoever smelt it inhaled aerosolized fecal matter.",
        "Microwave ovens aren't all micro. Some are actually quite large.",
        "When your dentist says they're doing a full cavity search, ask them exactly what they mean by that.",
        "Find a job you enjoy doing with dogs and you'll never work a day in your life.",
        "People who think they have good voices always do. Keep singing!",
        "You can't have everything unless you're a bagel.",
        "The number of people older than you will never increase unless you're reverse aging.",
        "When time is running out and you don't know which wire to cut, go with yellow.",
        "When time is running out and you don't know which wire to cut, go with red.",
        "When time is running out and you don't know which wire to cut, go with blue.",
        "When time is running out and you don't know which wire to cut, go with green.",
        "If you lose your remote control, look behind the couch.",
        "Don't swim for thirty minutes after eating.",
        "Do eat for thirty minutes after swimming.",
        "Always salt your pasta water generously before it comes to a boil.",
        "No phones at the dinner table.",
        "Always bring milk with you to a protest in case you get pepper sprayed.",
        "Shop local!",
        "Food expiration dates are just a suggestion.",
        "You must take your place in the circle of life.",
        "Wash your face twice a day.",
        "Use a tinted moisturizer instead of foundation to improve your skin.",
        "You need to learn who you are outside of relationships or you will keep running away at your wedding.",
        "An eye for an eye makes the whole world blind.",
        "Remember to engage your core.",
        "Try to store a two-week supply of water in your home for emergencies.",
        "I before e except after c, sometimes.",
        "If you're in line to vote before the polls close, stay in line!",
        "If you're ever on fire, stop, drop, and roll! Then stop again when it's out.",
        "If you're ever in a duel, aim up at the sky unless you're dueling Aaron Burr.",
        "Call your mother!",
        "Vanity will always lead to sadness.",
        "Enjoy what you already have.",
        "You can never go backwards.",
        "Baking soda is NOT the same as baking powder.",
        "Invest in that salad spinner!",
        "Read the whole recipe first.",
        "The hardest part of any new adventure is the first step.",
        "Believe in yourself and the world will follow.",
        "Love arrives when you aren't looking for it.",
        "Always set your clock five minutes early.",
        "Always put it in writing!",
        "Some questions ARE dumb.",
        "Quality, not quantity.",
        "Everything worthwhile takes time.",
        "For the best curls, dry your hair with a t-shirt, not a towel.",
        "Always cut your steak against the grain.",
        "Don't take advice from cartoon aliens.",
        "If it's yellow let it mellow, if it's brown flush it down. If it's crying: oh no.",
        "Every meeting can be a phone call and every phone call can be an email.",
        "A watched pot never boils.",
        "Don't judge a book by it's cover.",
        "If you love something, let it go. If it comes back to you, it's yours forever. If it doesn't, then it was never meant to be.",
        "A bird in the hand is worth two in the bush.",
        "Buy bitcoin 10 years ago.",
        "Thaw the turkey before you put it in the deep fryer.",
        "Two people can keep a secret if one of them is dead.",
        "Don't write anything in an email you wouldn't want read in family court.",
        "Better to just shave it all off. Nobody's buying that comb over.",
        "Sometimes it's best to just sit down and shut your mouth.",
        "TRUST NO ONE.",
        "It's okay to fart if no one will hear you and it won't smell too badly.",
        "Quit smoking now for bonus days on your life.",
        "The only thing worse than accidentally letting all the hogs out of the barn is lying to grandma about it.",
        "Money isn't everything but it is still very important.",
        "Don't expect to go to prom if you smell like a sewer.",
        "Nobody likes a ball hog (in sports but also in the bedroom and the ballroom).",
        "Everyone is expendable.",
        "Don't ruin the family's vacation over a petty argument. The petty argument can wait until everyone's home and you're not paying a fortune every night for a hotel room.",
        "Never call your sister that word!",
        "Don't lie about being sick because you wanted to go to the battle of the bands instead of having dinner with your sister's old roommate.",
        "Snitches get stitches.",
        "Don't sell your soul for $5.",
        "A man should treat his wife as well as he treats the hotrod he's rebuilding in the garage.",
        "You must know when to hold 'em, but also you must know when to fold 'em (talking about blankets).",
        "Never drink from a public water fountain without checking all your angles.",
        "If someone has you in a rear naked choke, fight their hands and get your back to the ground.",
        "A man who has friends is a rich man indeed. Just not with money.",
        "Pain is just weakness leaving the body.",
        "Follow your fear. Scare your fear in a garage at night. Send your fear anonymous threatening notes.",
        "Drink water upside down to get rid of hiccups. BOO!",
        "When you get a chance to sit it out or dance — dance.",
        "Yesterday is history, tomorrow is a mystery, but today is a gift, that's why they call it the present.",
        "Don't cry over spilled milk, you baby.",
        "Clear eyes, full hearts, can't lose.",
        "Teamwork makes the dream work.",
        "There's no \"I\" in team, but there is a me, meat, tame, mate, eat, ate, mat...",
        "Hang in there.",
        "Don't give up, you've got a reason to live. You can't forget, you only get what you give.",
        "Make pay while the sun shines.",
        "High Yield Savings accounts are a joke.",
        "Pay into your 401k early and often, especially if your company will match it.",
        "A hotdog may be a sandwich but a hoagie isn't.",
        "It's a real shame they're called tote bags and not totes bags.",
        "Just because you can doesn't mean you should make your bed everyday.",
        "Never leave the house without an amulet that's been blessed by a witch.",
        "TV writing is not for the faint of arms, neither is regular writing.",
        "Dance like everyone's watching.",
        "Never put table salt on your nightstand.",
        "Talking is for closers.",
        "Make love not onion rings.",
        "Water signs are bitches, Earth signs get stitches.",
        "They tried to bury us, they didn't know we were gophers.",
        "To beet or not to beet, that is the question.",
        "Ceramics is a really competitive industry, but breaking in isn't as hard as you think.",
        "Affirmations are for the birds.",
        "An eye for an eye, a tooth for a tooth and a bugle for a bugle.",
        "All play and no work makes you the life of the partay!",
        "A leopard cannot change its spots without a sci-fi ray or at least some markers.",
        "Wash your hands for at least a hundred seconds.",
        "Don't cheat on your wife, you're better than that.",
        "Cats get to choose who holds them.",
        "Moderation, all the time.",
        "Make sure someone is looking at you before you throw something to them. Yelling \"heads up\" just isn't enough.",
        "Condoms expire.",
        "Just make two trips carrying the groceries.",
        "If you're making a noise while trying to push out a dump, then calm down and lean back.",
        "Don't make one genre of music your whole thing.",
        "Don't fart at the dinner table.",
        "Soft bristles, gentle brushing. The firm bristles are going to mess up your enamel.",
        "Write a little every day.",
        "Assume that bad drivers are only acting like that because they're holding a fish bowl while trying to get home from the pet store.",
        "Cut your family some slack.",
        "Restart your computer every once in a while.",
        "You can check your subscriptions by going to your account in settings.",
        "Be kind.",
        "Life is just over after you die, so make the best of it and help other people while you're around.",
        "Don't take investing advice from social media.",
        "Don't look right at the laser pointer.",
        "If you can't say something nice, don't say anything at all.",
        "It ain't easy being cheesy.",
        "When \"god\" closes a door, he opens a window.",
        "Don't bite off more than you can chew, you little scamp.",
        "Shrimp for breakfast is diarrhea city.",
        "If you masturbate too much you actually will go blind. Trust me.",
        "It's not the size of the ship, it's about the motion of the ocean (unless you have a really small embarrassing ship).",
        "Call your grandma. BUT NOT GRANDPA.",
        "Don't text your ex. Write them a long, devastating letter on expensive paper and have a bird deliver it.",
        "If someone tells you they love you just as you are, they are your soulmate. They're also your soulmate if they're a hot British lawyer.",
        "Just because you are the loudest, doesn't make you wrong.",
        "If you're on a school trip in Rome and everyone treats you like a celebrity, you probably have an Italian pop star doppelgänger.",
        "Buy a sink strainer.",
        "An apple a day keeps the doctor away. A doctor a day makes you the mysterious doctor kidnapper.",
        "If it looks like a shlorduck, swims like a shlorduck, and quacks like a shlorduck, then it probably is ashlorduck.",
        "Don't get mad, get even.",
        "Always double cleanse.",
        "Never over exfoliate.",
        "Listen to your heart. If it's really loud you might be dehydrated.",
        "Don't bite the hand that feeds you.",
        "Don't put all your eggs in one basket.",
        "Look both ways before crossing the street.",
        "Your parents were once hot. Food for thought.",
        "The answer is blowing in the wind.",
        "Floss, then brush.",
        "Brush, then floss.",
        "Don't mess with Texas.",
        "He who cannot be a good follower, cannot be a good leader.",
        "Those who know, do. Those that understand, teach.",
        "Educating the mind without educating the heart is no education at all.",
        "Don't count your chickens before they hatch.",
        "If it's spicy going in, it's spicy going out (the butthole.)",
        "If it ain't broke, dain't fix it.",
        "The customer is always right.",
        "Reduce, reuse, recycle, repent.",
        "When giving a high five, look at the other person's elbow. Same thing works for kissing.",
        "Always have a song ready just in case you get passed the AUX.",
        "Clout is king.",
        "Your parents got a house for two acorns and a handshake. It's okay that you're still renting.",
        "Tragedy plus time equals comedy.",
        "Crack is in fact wack. Sorry.",
        "Keep texting even though they angrily said \"bye\".",
        "Yelling \"on me\" doesn't help your squad.",
        "Don't ever let anyone convince you that you don't like what you like.",
        "If it's your turn to freestyle, don't steal a verse you heard on the radio. Rap from the heart, yo"
    };
}

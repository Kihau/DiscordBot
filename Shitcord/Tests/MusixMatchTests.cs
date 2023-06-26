using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Shitcord.WebScrapper;

namespace Shitcord.Tests;

public class MusixMatchTests{
    private static HttpClient client = new();
    private const string TOKEN = "68c485929bde0b4c9642b7f34b2f8e63";
    private const string API = "http://api.musixmatch.com/ws/1.1/";

    public static async void run(){
        BotConfig config = new BotConfig("Resources/config-debug.json");
        Thread thread = new Thread(nothing);
        thread.Start();
        
        Console.WriteLine("OUTPUT");
        client.Timeout = TimeSpan.FromSeconds(12);
        string skeler = "Falling Apart skeler"; //no lyrics
        string beeGees = "bee gees stayin' alive"; //too many incorrect results
        string deadCanDance = "dead can dance anabis"; //no tracks
        string pokahontaz = "pokahontaz 404 kaliber";
        string songName = beeGees;

        var searchRequest = new HttpRequestMessage {
            RequestUri = new Uri($"{API}track.search?apikey={TOKEN}&q={songName}"),
            Method = HttpMethod.Get,
        };
        searchRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = await client.SendAsync(searchRequest);
        if (response.StatusCode != HttpStatusCode.OK){
            //exception
            Console.WriteLine("RESPONSE: " + response.StatusCode);
            return;
        }
        string content = await response.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromSeconds(4));
        var json = JsonNode.Parse(content);
        JsonArray? tracks = json?["message"]?["body"]?["track_list"]?.AsArray();
        if (tracks == null || tracks.Count == 0){
            Console.WriteLine("No tracks found");
            return;
        }

        var songs = new List<MusixSongInfo>(tracks.Count);
        foreach (JsonNode? trackNode in tracks){
            var track = trackNode?["track"];
            if(track == null) 
                continue;
            
            MusixSongInfo song = new MusixSongInfo();
            song.SetTitle(track["track_name"]);
            song.SetInstrumental(track["instrumental"]);
            song.SetTrackURL(track["track_share_url"]);
            song.SetArist(track["artist_name"]);
            song.SetRating(track["track_rating"]);
            song.SetNumFav(track["num_favourite"]);
            songs.Add(song);
            Console.WriteLine(song);
        }

        if (songs.Count == 0){
            Console.WriteLine("No valid songs found");
            return;
        }
        Console.WriteLine("Retrieving lyrics");
        //implement matching?
        string lyrics = await RetrieveLyricsFromURL(songs[0].trackURL);
        Console.WriteLine(lyrics == string.Empty ? "No lyrics available" : lyrics);
        
    }
    
    private static async Task<string> RetrieveLyricsFromURL(string url){
        if (url == string.Empty)
            return "";
        var trackLookup = new HttpRequestMessage {
            RequestUri = new Uri(url),
            Method = HttpMethod.Get,
        };
        
        HttpResponseMessage response = await client.SendAsync(trackLookup);
        if (response.StatusCode != HttpStatusCode.OK){
            //exception
            Console.WriteLine("RESPONSE: " + response.StatusCode);
            return "";
        }
        string content = await response.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromSeconds(5));
        return scrapeLyrics(content);
    }

    private static string scrapeLyrics(string content){
        HtmlDoc html = new HtmlDoc(content);
        Tag? firstTag = html.Find("span", ("class", "lyrics__content__ok"));
        if (firstTag == null){
            return "";
        }
        
        Tag? mainTag = html.FindFrom("span", firstTag.StartOffset +1,  ("class", "lyrics__content__ok"));
        if (mainTag == null){
            return "";
        }
        string extract1 = html.ExtractText(firstTag);
        string extract2 = html.ExtractText(mainTag);
        return extract1  + extract2;
    }

    private static void nothing(){
        Thread.Sleep(60000000);
    }
}

record MusixSongInfo{
    public string title = "", artist = "", trackURL = "";
    public bool instrumental;
    public int rating, numFavorite;
    public void SetTitle(JsonNode? node){
        if (node == null){
            return;
        }
        title = node.ToString();
    }
    public void SetRating(JsonNode? node){
        if (node == null){
            return;
        }

        try{
            rating = int.Parse(node.ToString());
        }catch {
            rating = -1;
        }
    }
    public void SetNumFav(JsonNode? node){
        if (node == null){
            return;
        }

        try{
            numFavorite = int.Parse(node.ToString());
        }catch {
            numFavorite = -1;
        }
    }
    public void SetTrackURL(JsonNode? node){
        if (node == null){
            return;
        }
        trackURL = node.ToString();
        int paramsIndex = trackURL.IndexOf('?');
        if (paramsIndex == -1){
            return;
        }
        trackURL = trackURL[..paramsIndex];
    }
    
    public void SetInstrumental(JsonNode? node){
        if (node == null)
            return;
        string intBoolean = node.ToString();
        instrumental = intBoolean[0] == '1';
    }
    public void SetArist(JsonNode? node){
        if (node == null)
            return;
        artist = node.ToString();
    }
}
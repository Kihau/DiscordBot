
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Shitcord.Extensions;

namespace Shitcord.Tests;

public class HttpTests{
    private static HttpClient SharedClient = new ();
    public static async void main(){
        BotConfig config = new BotConfig("Resources/config-debug.json");
        Thread thread = new Thread(nothing);
        thread.Start();
        
        Console.WriteLine("OUTPUT");
        SharedClient.Timeout = TimeSpan.FromSeconds(12);
        string deadCanDance = "dead can dance anabis";
        string tripleOne = "triple one driving off";
        string songName = tripleOne;
        var searchRequest = new HttpRequestMessage {
            RequestUri = new Uri($"https://api.genius.com/search?q={songName}"),
            Method = HttpMethod.Get,
        };
        searchRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Genius.Token);
        HttpResponseMessage response = await SharedClient.SendAsync(searchRequest);
        if (response.StatusCode != HttpStatusCode.OK){
            //exception
            Console.WriteLine("RESPONSE: " + response.StatusCode);
            return;
        }

        string content = await response.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromSeconds(3));
        
        Console.WriteLine("PARSING RESPONSE");
        var json = JsonNode.Parse(content);
        var hits = json?["response"]?["hits"]?.AsArray();
        if (hits is null) return;
        
        var songs = new List<SongInfo>();
        foreach (var hit in hits) {
            if (hit is null){
                continue;
            }
            var result = hit["result"];
            var song = result.Deserialize<SongInfo?>();
            if (song != null){
                song.fixEncoding();
                songs.Add(song);
            }
        }
        
        foreach (var song in songs){
            Console.WriteLine(song);
        }
        SongInfo? mostAccurate = SelectMostAccurate(songName, songs);
        if (mostAccurate?.lyrics_url == null) 
            return;
        Console.WriteLine(mostAccurate);
        
        //scrape lyrics request
        if (!mostAccurate.lyrics_url.StartsWith("https://genius.com")){
            return;
        }
        
        var webpageRequest = new HttpRequestMessage {
            RequestUri = new Uri(mostAccurate.lyrics_url),
            Method = HttpMethod.Get,
        };
        webpageRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        webpageRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 Gecko/20100101");
        webpageRequest.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.7));
        HttpResponseMessage pageResponse = await SharedClient.SendAsync(webpageRequest);
        if (pageResponse.StatusCode != HttpStatusCode.OK){
            //exception
            return;
        }
        byte[] bytes = await pageResponse.Content.ReadAsByteArrayAsync();
        var pageContent = Encoding.UTF8.GetString(bytes);
        string lyrics = ScrapeLyrics(pageContent);
        Console.WriteLine("SCRAPED");
        Console.WriteLine(lyrics);
    }

    private static void nothing(){
        Thread.Sleep(60000000);
    }

    private static string ScrapeLyrics(string page){
        if (page == null)
            return "";
        const string CONTAINER = "Lyrics__Container-sc";
        const string INSTRUMENTAL = "This song is an instrumental";
        int fakeContainer = page.IndexOf(CONTAINER, StringComparison.Ordinal);
        if(fakeContainer == -1){
            return page.Contains(INSTRUMENTAL) ? "This song is an instrumental" : "";
        }
        int lyricContainer = page.IndexOf(CONTAINER, fakeContainer + 1, StringComparison.Ordinal);
        int textStart = page.IndexOf('>', lyricContainer);
        StringBuilder lyrics = new StringBuilder();
        int angleBrackets = 0, divCounter = 1;
        bool spaced = false;
        Console.WriteLine("PARSING");
        for (int i = textStart + 1; i < page.Length; i++){
            switch (page[i]){
                case '<':
                    //open + close
                    if (page.Substring(i+1, 2) == "br"){
                        i += 4;
                        if (page[i + 3] == '/'){
                            i++;
                        }
                        if(!spaced)
                            lyrics.Append('\n');
                        spaced = true;
                        continue;
                    }
                    if (page.Substring(i+1, 4) == "/div"){
                        if (divCounter == 0){
                            Console.WriteLine("DIV EXIT");
                            goto exitLoop;
                        }
                        i += 4;
                        divCounter--;
                    }else if (page.Substring(i + 1, 3) == "div"){
                        i += 3;
                        divCounter++;
                    }
                    angleBrackets++;
                    break;
                case '&':
                    if (page.Substring(i + 1, 5) == "#x27;"){
                        i += 5;
                        lyrics.Append('\'');
                    }
                    break;
                case '>':
                    if (angleBrackets == 0){
                        goto exitLoop;
                    }
                    angleBrackets--;
                    break;
                default:
                    if (angleBrackets == 0){
                        spaced = false;
                        lyrics.Append(page[i]);
                    }
                    break;
            }
        }
        Console.WriteLine("EXITED");
        exitLoop:
        stripDigits(lyrics, 3);
        if(endsWith(lyrics, "Embed")){
            lyrics.Length -= 5;
        }
        stripDigits(lyrics, 4);
        if(endsWith(lyrics, "You might also like")){
            lyrics.Length -= 19;
        }
        return lyrics.ToString();
    }
    
    private static void stripDigits(StringBuilder str, int quantity){
        if(quantity <= 0) 
            return;
        int currLen = str.Length;
        for (int i = currLen-1; i >= currLen - quantity && i > -1; i--){
            if(char.IsDigit(str[i])){
                str.Length = i;
            }
        }
    }

    private static bool endsWith(StringBuilder str, string seq){
        int mainLen = str.Length;
        int start = str.Length - seq.Length;
        if(start < 0){
            return false;
        }
        for (int i = start, j = 0; i < mainLen; i++, j++){
            if(str[i] != seq[j]){
                return false;
            }
        }
        return true;
    }
    private static SongInfo? SelectMostAccurate(string name, List<SongInfo> songs){
        int len = songs.Count;
        if(len == 0){
            return null;
        }
        double[] accuracies = new double[len];
        double max = 0;
        int index = -1;
        for (int i = 0; i < len; i++){
            string? fullTitle = songs[i].full_title;
            if (fullTitle == null)
                continue;
            
            accuracies[i] = StringMatching.Accuracy(name, fullTitle);
            accuracies[i] /= fullTitle.Length;
            if(accuracies[i] > max){
                max = accuracies[i];
                index = i;
            }
        }
        if(index == -1){
            index = 0;
        }
        Console.WriteLine(string.Join(" ", accuracies));
        return songs[index];
    }
}

record SongInfo
{
    [JsonPropertyName("is")] 
    public int id { get; set; }

    [JsonPropertyName("full_title")] 
    public string? full_title { get; set; }

    [JsonPropertyName("url")] 
    public string? lyrics_url { get; set; }

    [JsonPropertyName("release_date_for_display")] 
    public string? release { get; set; }

    public void fixEncoding(){
        //NBSP fix?
        lyrics_url = lyrics_url?.Replace('\u00a0', ' ');
        full_title = full_title?.Replace('\u00a0', ' ');
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Shitcord.Extensions;

namespace Shitcord.Tests;

public class HttpTests{
    private static HttpClient SharedClient = new ();
    private const string ACCESS_TOKEN = "";
    public static async void main(){
        Thread thread = new Thread(nothing);
        thread.Start();
        
        Console.WriteLine("OUTPUT");
        SharedClient.Timeout = TimeSpan.FromSeconds(5);
        string songName = "Lil Yachty";
        var searchRequest = new HttpRequestMessage {
            RequestUri = new Uri($"https://api.genius.com/search?q={songName}"),
            Method = HttpMethod.Get,
        };
        searchRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ACCESS_TOKEN);
        
        HttpResponseMessage response = await SharedClient.SendAsync(searchRequest);
        Console.WriteLine("RETURNED RESPONSE");
        if (response.StatusCode != HttpStatusCode.OK){
            //exception
            return;
        }
        string content = await response.Content.ReadAsStringAsync().WaitAsync(TimeSpan.FromSeconds(2));
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
                songs.Add(song);
            }
        }

        foreach (var song in songs){
            Console.WriteLine(song);
        }

    }

    private static void nothing(){
        Thread.Sleep(10000);
    }
    
    private static SongInfo? SelectMostAccurate(string name, SongInfo[] songs){
        int len = songs.Length;
        if(len == 0){
            return null;
        }
        int[] accuracies = new int[len];
        int max = 0, index = -1;
        for (int i = 0; i < len; i++){
            string? fullTitle = songs[i].full_title;
            if (fullTitle == null)
                continue;
            
            accuracies[i] = StringMatching.Accuracy(name, fullTitle);
            if(accuracies[i] > max){
                max = accuracies[i];
                index = i;
            }
        }
        if(index == -1){
            index = 0;
        }
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
}
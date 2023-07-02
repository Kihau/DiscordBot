using System.Text.Json.Serialization;

namespace Shitcord.Data;

record SongInfo
{
    [JsonPropertyName("id")] 
    public int id { get; set; }

    [JsonPropertyName("full_title")] 
    public string? full_title { get; set; }
    
    [JsonPropertyName("artist_names")] 
    public string? artist_name { get; set; }

    [JsonPropertyName("url")] 
    public string? lyrics_url { get; set; }
    
    [JsonPropertyName("header_image_thumbnail_url")] 
    public string? thumbnail_url { get; set; }

    [JsonPropertyName("release_date_for_display")] 
    public string? release { get; set; }

    public void fixEncoding(){
        //NBSP fix?
        lyrics_url = lyrics_url?.Replace('\u00a0', ' ');
        full_title = full_title?.Replace('\u00a0', ' ');
    }
}
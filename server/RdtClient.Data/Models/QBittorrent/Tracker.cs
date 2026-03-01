using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class Tracker
{
    [JsonPropertyName("url")]
    public required String Url { get; set; }

    [JsonPropertyName("status")]
    public required String Status { get; set; }

    [JsonPropertyName("num_peers")]
    public required Int64 NumPeers { get; set; }
    
    [JsonPropertyName("msg")]
    public required String Msg { get; set; }
}
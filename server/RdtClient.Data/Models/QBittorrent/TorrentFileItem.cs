using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class TorrentFileItem
{
    [JsonPropertyName("index")]
    public Int32 Index { get; set; }

    [JsonPropertyName("name")]
    public String? Name { get; set; }

    [JsonPropertyName("size")]
    public Int64 Size { get; set; }

    [JsonPropertyName("progress")]
    public Single Progress { get; set; }

    [JsonPropertyName("priority")]
    public Int32 Priority { get; set; }

    [JsonPropertyName("is_seed")]
    public Boolean IsSeed { get; set; }
}

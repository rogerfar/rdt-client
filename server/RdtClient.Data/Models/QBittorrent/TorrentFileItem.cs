using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class TorrentFileItem
{
    [JsonPropertyName("name")]
    public String? Name { get; set; }
}
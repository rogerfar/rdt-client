using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class TorrentCategory
{
    [JsonPropertyName("name")]
    public String? Name { get; set; }

    [JsonPropertyName("savePath")]
    public String? SavePath { get; set; }
}
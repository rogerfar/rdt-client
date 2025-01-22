using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class TransferInfo
{
    [JsonPropertyName("connection_status")]
    public String ConnectionStatus { get; set; } = default!;

    [JsonPropertyName("dht_nodes")]
    public Int64 DhtNodes { get; set; }

    [JsonPropertyName("dl_info_data")]
    public Int64 DlInfoData { get; set; }

    [JsonPropertyName("dl_info_speed")]
    public Int64 DlInfoSpeed { get; set; }

    [JsonPropertyName("dl_rate_limit")]
    public Int64 DlRateLimit { get; set; }

    [JsonPropertyName("up_info_data")]
    public Int64 UpInfoData { get; set; }

    [JsonPropertyName("up_info_speed")]
    public Int64 UpInfoSpeed { get; set; }

    [JsonPropertyName("up_rate_limit")]
    public Int64 UpRateLimit { get; set; }
}
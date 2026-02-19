using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.Sabnzbd;

public class SabnzbdMisc
{
    [JsonPropertyName("complete_dir")]
    public String CompleteDir { get; set; } = "";

    [JsonPropertyName("download_dir")]
    public String DownloadDir { get; set; } = "";

    [JsonPropertyName("port")]
    public String Port { get; set; } = "6500";

    [JsonPropertyName("version")]
    public String Version { get; set; } = "4.4.0";
}
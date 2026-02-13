using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.Sabnzbd;

public class SabnzbdResponse
{
    [JsonPropertyName("queue")]
    public SabnzbdQueue? Queue { get; set; }

    [JsonPropertyName("history")]
    public SabnzbdHistory? History { get; set; }

    [JsonPropertyName("version")]
    public String? Version { get; set; }

    [JsonPropertyName("status")]
    public Boolean? Status { get; set; }

    [JsonPropertyName("error")]
    public String? Error { get; set; }

    [JsonPropertyName("nzo_ids")]
    public List<String>? NzoIds { get; set; }

    [JsonPropertyName("config")]
    public SabnzbdConfig? Config { get; set; }

    [JsonPropertyName("categories")]
    public List<String>? Categories { get; set; }
}
using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.Sabnzbd;

public class SabnzbdConfig
{
    [JsonPropertyName("misc")]
    public SabnzbdMisc Misc { get; set; } = new();

    [JsonPropertyName("categories")]
    public List<SabnzbdCategory> Categories { get; set; } = new();

    [JsonPropertyName("servers")]
    public List<Object> Servers { get; set; } = new();
}
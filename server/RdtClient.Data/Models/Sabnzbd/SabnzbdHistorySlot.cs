using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.Sabnzbd;

public class SabnzbdHistorySlot
{
    [JsonPropertyName("nzo_id")]
    public String NzoId { get; set; } = "";

    [JsonPropertyName("name")]
    public String Name { get; set; } = "";

    [JsonPropertyName("size")]
    public String Size { get; set; } = "0 B";

    [JsonPropertyName("status")]
    public String Status { get; set; } = "Completed";

    [JsonPropertyName("category")]
    public String Category { get; set; } = "Default";

    [JsonPropertyName("storage")]
    public String Path { get; set; } = "";
}
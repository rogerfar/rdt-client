using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.Sabnzbd;

public class SabnzbdQueueSlot
{
    [JsonPropertyName("index")]
    public Int32 Index { get; set; }

    [JsonPropertyName("nzo_id")]
    public String NzoId { get; set; } = "";

    [JsonPropertyName("filename")]
    public String Filename { get; set; } = "";

    [JsonPropertyName("size")]
    public String Size { get; set; } = "0 B";

    [JsonPropertyName("sizeleft")]
    public String SizeLeft { get; set; } = "0 B";

    [JsonPropertyName("percentage")]
    public String Percentage { get; set; } = "0";

    [JsonPropertyName("status")]
    public String Status { get; set; } = "Downloading";

    [JsonPropertyName("cat")]
    public String Category { get; set; } = "Default";

    [JsonPropertyName("timeleft")]
    public String TimeLeft { get; set; } = "0:00:00";

    [JsonPropertyName("priority")]
    public String Priority { get; set; } = "Normal";
}
using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.Sabnzbd;

public class SabnzbdQueue
{
    [JsonPropertyName("version")]
    public String Version { get; set; } = "4.4.0";

    [JsonPropertyName("status")]
    public String Status { get; set; } = "Idle";

    [JsonPropertyName("speed")]
    public String Speed { get; set; } = "0 ";

    [JsonPropertyName("size")]
    public String Size { get; set; } = "0 B";

    [JsonPropertyName("sizeleft")]
    public String SizeLeft { get; set; } = "0 B";

    [JsonPropertyName("noofslots")]
    public Int32 NoOfSlots { get; set; }

    [JsonPropertyName("slots")]
    public List<SabnzbdQueueSlot> Slots { get; set; } = new();
}
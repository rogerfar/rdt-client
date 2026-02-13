using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.Sabnzbd;

public class SabnzbdHistory
{
    [JsonPropertyName("total_slots")]
    public Int32 TotalSlots { get; set; }

    [JsonPropertyName("noofslots")]
    public Int32 NoOfSlots { get; set; }

    [JsonPropertyName("slots")]
    public List<SabnzbdHistorySlot> Slots { get; set; } = new();
}
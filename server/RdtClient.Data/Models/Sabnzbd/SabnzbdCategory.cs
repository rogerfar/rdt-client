using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.Sabnzbd;

public class SabnzbdCategory
{
    [JsonPropertyName("name")]
    public String Name { get; set; } = "default";

    [JsonPropertyName("order")]
    public Int32 Order { get; set; } = 0;

    [JsonPropertyName("dir")]
    public String Dir { get; set; } = "";

    [JsonPropertyName("newzbin")]
    public String Newzbin { get; set; } = "";

    [JsonPropertyName("priority")]
    public Int32 Priority { get; set; } = -100;

    [JsonPropertyName("script")]
    public String Script { get; set; } = "None";
}
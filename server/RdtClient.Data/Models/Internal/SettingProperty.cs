namespace RdtClient.Data.Models.Internal;

public class SettingProperty: SettingKeyValuePair
{
    public String? DisplayName { get; set; }
    public String? Description { get; set; }
    public String Type { get; set; } = default!;
    public Dictionary<Int32, String>? EnumValues { get; set; }
}
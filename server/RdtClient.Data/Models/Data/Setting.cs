using System.ComponentModel.DataAnnotations;

namespace RdtClient.Data.Models.Data;

public class Setting
{
    [Key]
    public String SettingId { get; set; } = null!;

    public String? Value { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace RdtClient.Data.Models.Data;

public class Setting
{
    [Key]
    public String SettingId { get; set; }

    public String Value { get; set; }
}
namespace RdtClient.Data.Models.Internal;

public class SettingKeyValuePair
{
    /// <summary>
    /// The unique human-readable key identifying the setting
    /// </summary>
    /// <example>General:LogLevel</example>
    /// <example>"Provider:Default:ExcludeRegex</example>
    public String Key { get; set; } = default!;
    /// <summary>
    /// The value of the setting
    /// </summary>
    public Object? Value { get; set; }
}

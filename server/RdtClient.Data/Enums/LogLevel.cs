using System.ComponentModel;

namespace RdtClient.Data.Enums;

public enum LogLevel
{
    [Description("Debug")]
    Debug,

    [Description("Information")]
    Information,

    [Description("Warning")]
    Warning,

    [Description("Error")]
    Error
}
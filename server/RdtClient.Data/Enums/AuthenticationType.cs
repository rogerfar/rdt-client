using System.ComponentModel;

namespace RdtClient.Data.Enums;

public enum AuthenticationType
{
    [Description("Username + Password")]
    UserNamePassword,

    [Description("No Authentication")]
    None
}
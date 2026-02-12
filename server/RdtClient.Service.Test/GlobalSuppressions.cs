using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("Usage",
                       "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
                       Justification = "It is serializable.",
                       Scope = "NamespaceAndDescendants",
                       Target = "N:RdtClient.Service.Test")]
[assembly:
    SuppressMessage("Performance",
                       "SYSLIB1045:Convert to 'GeneratedRegexAttribute'.",
                       Justification = "We don't care for unit tests.",
                       Scope = "NamespaceAndDescendants",
                       Target = "N:RdtClient.Service.Test")]

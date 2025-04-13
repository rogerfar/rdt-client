// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Bug in Serilog", Scope = "NamespaceAndDescendants", Target = "N:RdtClient.Service")]
[assembly: SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "Code style", Scope = "NamespaceAndDescendants", Target = "N:RdtClient.Service")]
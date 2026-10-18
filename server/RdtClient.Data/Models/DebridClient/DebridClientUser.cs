namespace RdtClient.Data.Models.DebridClient;

public class DebridClientUser
{
    public String? Username { get; set; }
    public DateTimeOffset? Expiration { get; set; }

    /// <summary>
    ///     Explicitly premium even without a dated <see cref="Expiration" /> (e.g. an unlimited/lifetime plan).
    ///     Most providers only ever set <see cref="Expiration" />, which is premium enough on its own.
    /// </summary>
    public Boolean Premium { get; set; }
}

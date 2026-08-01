namespace RdtClient.Service.Middleware;

public static class RateLimitPolicies
{
    /// <summary>
    ///     Applied to the endpoints that verify a password. Acts as a speed brake in front of Identity's account lockout,
    ///     which is the actual brute-force defence because it works regardless of the source address.
    /// </summary>
    public const String Authentication = "authentication";
}

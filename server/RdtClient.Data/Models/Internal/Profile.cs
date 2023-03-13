namespace RdtClient.Data.Models.Internal;

public class Profile
{
    public String? Provider { get; set; }
    public String? UserName { get; set; }
    public DateTimeOffset? Expiration { get; set; }
    public String? CurrentVersion { get; set; }
    public String? LatestVersion { get; set; }
}
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RdtClient.Service.BackgroundServices;

public class UpdateChecker(ILogger<UpdateChecker> logger) : BackgroundService
{
    public static String? CurrentVersion { get; private set; }
    public static String? LatestVersion { get; private set; }
    
    public static Boolean? IsInsecure { get; private set; }

    private static readonly List<String> KnownGhsaIds = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Startup.Ready)
        {
            await Task.Delay(1000, stoppingToken);
        }

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        if (String.IsNullOrWhiteSpace(version))
        {
            CurrentVersion = "";

            return;
        }

        CurrentVersion = $"v{version[..version.LastIndexOf('.')]}";

        logger.LogInformation("UpdateChecker started, currently on version {CurrentVersion}.", CurrentVersion);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var gitHubReleases = await GitHubRequest<List<GitHubReleasesResponse>>("/repos/rogerfar/rdt-client/tags?per_page=1", stoppingToken);

                var latestRelease = gitHubReleases?.FirstOrDefault(m => m.Name != null)?.Name;

                if (latestRelease == null)
                {
                    logger.LogWarning($"Unable to find latest version on GitHub");
                    return;
                }

                if (latestRelease != CurrentVersion)
                {
                    logger.LogInformation("New version found on GitHub: {latestRelease}", latestRelease);
                }

                LatestVersion = latestRelease;

                var gitHubSecurityAdvisories = await GitHubRequest<List<GitHubSecurityAdvisoriesResponse>>("/repos/rogerfar/rdt-client/security-advisories", stoppingToken);

                var unseenGhsaIds = gitHubSecurityAdvisories?.Where(advisory => !KnownGhsaIds.Contains(advisory.GhsaId));
                
                if (unseenGhsaIds == null)
                {
                    logger.LogWarning($"Unable to find security advisories on GitHub");
                    return;
                }

                IsInsecure = unseenGhsaIds.Any();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Unexpected error occurred while checking for updates. This error is safe to ignore.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        logger.LogInformation("UpdateChecker stopped.");
    }

    private static async Task<T?> GitHubRequest<T>(String endpoint, CancellationToken cancellationToken)
    {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new("RdtClient", CurrentVersion));
            var response = await httpClient.GetStringAsync($"https://api.github.com{endpoint}", cancellationToken);
            
            return JsonConvert.DeserializeObject<T>(response);
    }
}

public class GitHubReleasesResponse 
{
    [JsonProperty("name")]
    public String? Name { get; set; }
}

public class GitHubSecurityAdvisoriesResponse
{
    [JsonProperty("ghsa_id")]
    public required String GhsaId { get; set; } 
}
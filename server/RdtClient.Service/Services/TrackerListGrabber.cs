namespace RdtClient.Service.Services;

public class TrackerListGrabber : ITrackerListGrabber
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TrackerListGrabber(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<String[]> GetTrackers()
    {
        var trackerUrlList = Settings.Get.General.MagnetTrackerEnrichment;
        if (String.IsNullOrWhiteSpace(trackerUrlList))
        {
            return [];
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var result = await httpClient.GetStringAsync(trackerUrlList);
            return result
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to fetch tracker list for enrichment.", ex);
        }
    }
}
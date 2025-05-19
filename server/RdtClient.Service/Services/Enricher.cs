using System.Web;
using Microsoft.Extensions.Logging;
using MonoTorrent.BEncoding;

namespace RdtClient.Service.Services;

public interface IEnricher
{
    Task<String> EnrichMagnetLink(String magnetLink);
    Task<Byte[]> EnrichTorrentBytes(Byte[] torrentBytes);
}

/// <summary>
/// Enriches magnet links and torrents by adding trackers from the tracker list grabber. 
/// </summary>
public class Enricher(ILogger<Enricher> logger, ITrackerListGrabber trackerListGrabber) : IEnricher
{
    /// <summary>
    /// Add trackers from the tracker list grabber to the magnet link.
    /// </summary>
    /// <param name="magnetLink">Magnet link to add trackres to. Is not modified</param>
    /// <returns>Magnet link with additional trackers</returns>
    public async Task<String> EnrichMagnetLink(String magnetLink)
    {
        var newTrackers = await trackerListGrabber.GetTrackers();

        var uri = new Uri(magnetLink);
        var query = HttpUtility.ParseQueryString(uri.Query);

        var existingTrackers = query.GetValues("tr") ?? [];
        var allTrackers = existingTrackers.Concat(newTrackers).Distinct(StringComparer.OrdinalIgnoreCase);

        var trackerQuery = String.Join("&tr=", allTrackers.Select(Uri.EscapeDataString));

        if (!String.IsNullOrEmpty(trackerQuery))
        {
            trackerQuery = "&tr=" + trackerQuery;
        }

        var baseWithoutTrackers = magnetLink.Split("&tr=")[0];

        var separator = baseWithoutTrackers.Contains('?') ? "&" : "?";

        var newUri = baseWithoutTrackers + separator + trackerQuery.TrimStart('&');
        try
        {
            _ = new Uri(newUri);
        }
        catch (UriFormatException ex)
        {
            logger.LogWarning(ex, "Failed to enrich magnet link: {newUri}", newUri);
            throw new InvalidOperationException($"Failed to enrich magnet link: {newUri}", ex);
        }
        return newUri;
    }

    /// <summary>
    /// Add trackers from the tracker list grabber to the .torrent file bytes.
    /// </summary>
    /// <param name="torrentBytes">Torrent file bytes to add trackers to. Is not modified</param>
    /// <returns>Torrent file bytes with additional trackers</returns>
    public async Task<Byte[]> EnrichTorrentBytes(Byte[] torrentBytes)
    {
        var newTrackers = await trackerListGrabber.GetTrackers();

        if (torrentBytes == null) throw new ArgumentNullException(nameof(torrentBytes));
        var torrentDict = BEncodedValue.Decode<BEncodedDictionary>(torrentBytes);


        if (!torrentDict.TryGetValue("announce-list", out var announceListValue) || announceListValue is not BEncodedList announceList)
        {
            announceList = new BEncodedList();
            if (torrentDict.TryGetValue("announce", out var announceValue) && announceValue is BEncodedString announceStr)
            {
                announceList.Add(new BEncodedList { announceStr });
            }
        }

        var existingTrackers = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (var tier in announceList)
        {
            if (tier is BEncodedList tierList)
            {
                foreach (var tracker in tierList)
                {
                    if (tracker is BEncodedString trackerStr)
                    {
                        existingTrackers.Add(trackerStr.Text);
                    }
                }
            }
        }

        foreach (var tracker in newTrackers)
        {
            if (!existingTrackers.Contains(tracker))
            {
                announceList.Add(new BEncodedList { new BEncodedString(tracker) });
                existingTrackers.Add(tracker);
            }
        }

        torrentDict["announce-list"] = announceList;
        if (announceList.Count > 0 && announceList[0] is BEncodedList firstTier && firstTier.Count > 0 && firstTier[0] is BEncodedString firstTracker)
        {
            torrentDict["announce"] = firstTracker;
        }

        return torrentDict.Encode();
    }
}
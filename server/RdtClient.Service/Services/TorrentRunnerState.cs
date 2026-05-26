using System.Collections.Concurrent;

namespace RdtClient.Service.Services;

public interface ITorrentRunnerState
{
    ConcurrentDictionary<Guid, DownloadClient> ActiveDownloadClients { get; }

    ConcurrentDictionary<Guid, UnpackClient> ActiveUnpackClients { get; }

    Boolean IsPausedForLowDiskSpace { get; set; }

    (Int64 Speed, Int64 BytesTotal, Int64 BytesDone) GetStats(Guid downloadId);

    void Clear();
}

public class TorrentRunnerState : ITorrentRunnerState
{
    public ConcurrentDictionary<Guid, DownloadClient> ActiveDownloadClients { get; } = new();

    public ConcurrentDictionary<Guid, UnpackClient> ActiveUnpackClients { get; } = new();

    public Boolean IsPausedForLowDiskSpace { get; set; }

    public (Int64 Speed, Int64 BytesTotal, Int64 BytesDone) GetStats(Guid downloadId)
    {
        if (ActiveDownloadClients.TryGetValue(downloadId, out var downloadClient))
        {
            return (downloadClient.Speed, downloadClient.BytesTotal, downloadClient.BytesDone);
        }

        if (ActiveUnpackClients.TryGetValue(downloadId, out var unpackClient))
        {
            return (0, 100, unpackClient.Progess);
        }

        return (0, 0, 0);
    }

    public void Clear()
    {
        ActiveDownloadClients.Clear();
        ActiveUnpackClients.Clear();
        IsPausedForLowDiskSpace = false;
    }
}

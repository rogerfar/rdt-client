using System.Web;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;

namespace RdtClient.Service.Helpers;

public static class Logger
{
    public static String ToLog(this Download download)
    {
        var fileName = download.Path;

        if (!String.IsNullOrWhiteSpace(download.Link))
        {
            var uri = new Uri(download.Link);
            fileName = uri.Segments.Last();
            fileName = HttpUtility.UrlDecode(fileName);
        }

        var stats = TorrentRunner.GetStats(download.DownloadId);
        var done = (Int32)((Double)stats.BytesDone / stats.BytesTotal * 100);

        if (done < 0 || Double.IsNaN(done) || Double.IsInfinity(done))
        {
            done = 0;
        }

        return $"for download {fileName}. Completed: {done}%, avg speed: {stats.Speed}bytes/s ({download.DownloadId}) remoteID: {download.RemoteId}";
    }

    public static String ToLog(this Torrent torrent)
    {
        return $"for torrent {torrent.RdName} ({torrent.RdId} - {torrent.RdStatusRaw} {torrent.RdProgress}%) ({torrent.TorrentId})";
    }
}
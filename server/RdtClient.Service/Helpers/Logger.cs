using System.Web;
using RdtClient.Data.Models.Data;

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

        var done = (Int32)((Double)download.BytesDone / download.BytesTotal * 100);

        if (done < 0)
        {
            done = 0;
        }

        return $"for download {fileName}. Completed: {done}%, avg speed: {download.Speed}bytes/s ({download.DownloadId}) remoteID: {download.RemoteId}";
    }

    public static String ToLog(this Torrent torrent)
    {
        return $"for torrent {torrent.RdName} ({torrent.RdId} - {torrent.RdStatusRaw} {torrent.RdProgress}%) ({torrent.TorrentId})";
    }
}
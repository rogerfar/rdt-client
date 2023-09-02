using System.Web;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Helpers;

public static class DownloadHelper
{
    public static String? GetDownloadPath(String downloadPath, Torrent torrent, Download download)
    {
        var fileUrl = download.Link;

        if (String.IsNullOrWhiteSpace(fileUrl) || torrent.RdName == null)
        {
            return null;
        }

        var directory = RemoveInvalidChars(torrent.RdName);

        var uri = new Uri(fileUrl);
        var torrentPath = Path.Combine(downloadPath, directory);

        if (!Directory.Exists(torrentPath))
        {
            Directory.CreateDirectory(torrentPath);
        }

        var fileName = uri.Segments.Last();

        fileName = HttpUtility.UrlDecode(fileName);

        fileName = RemoveInvalidChars(fileName);

        var filePath = Path.Combine(torrentPath, fileName);

        return filePath;
    }

    private static String RemoveInvalidChars(String filename)
    {
        return String.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }
}
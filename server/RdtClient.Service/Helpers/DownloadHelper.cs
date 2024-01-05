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

        var directory = RemoveInvalidPathChars(torrent.RdName);

        var uri = new Uri(fileUrl);
        var torrentPath = Path.Combine(downloadPath, directory);

        if (!Directory.Exists(torrentPath))
        {
            Directory.CreateDirectory(torrentPath);
        }

        var fileName = uri.Segments.Last();

        fileName = HttpUtility.UrlDecode(fileName);

        fileName = FileHelper.RemoveInvalidFileNameChars(fileName);

        var filePath = Path.Combine(torrentPath, fileName);

        return filePath;
    }

    public static String? GetDownloadPath(Torrent torrent, Download download)
    {
        var fileUrl = download.Link;

        if (String.IsNullOrWhiteSpace(fileUrl) || torrent.RdName == null)
        {
            return null;
        }

        var uri = new Uri(fileUrl);
        var torrentPath = RemoveInvalidPathChars(torrent.RdName);
        
        if (!Directory.Exists(torrentPath))
        {
            Directory.CreateDirectory(torrentPath);
        }

        var fileName = uri.Segments.Last();

        fileName = HttpUtility.UrlDecode(fileName);

        fileName = FileHelper.RemoveInvalidFileNameChars(fileName);

        var filePath = Path.Combine(torrentPath, fileName);

        return filePath;
    }

    private static String RemoveInvalidPathChars(String path)
    {
        return String.Concat(path.Split(Path.GetInvalidPathChars()));
    }
}
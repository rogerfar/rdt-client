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

        var uri = new Uri(fileUrl);
        var torrentPath = Path.Combine(downloadPath, torrent.RdName);
        var downloadSubFolderPath = Path.Combine(torrentPath, download.Folder);

        if (!Directory.Exists(downloadSubFolderPath))
        {
            Directory.CreateDirectory(downloadSubFolderPath);
        }

        var fileName = uri.Segments.Last();

        fileName = HttpUtility.UrlDecode(fileName);

        var filePath = Path.Combine(downloadSubFolderPath, fileName);

        return filePath;
    }
}
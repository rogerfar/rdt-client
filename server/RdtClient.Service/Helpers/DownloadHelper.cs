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

        var fileName = download.FileName;

        if (String.IsNullOrWhiteSpace(fileName))
        {
            fileName = uri.Segments.Last();

            fileName = HttpUtility.UrlDecode(fileName);
        }

        fileName = FileHelper.RemoveInvalidFileNameChars(fileName);

        var torrentPath = downloadPath;

        if (torrent.Files.Count > 1)
        {
            torrentPath = Path.Combine(downloadPath, directory);

            var matchingTorrentFiles = torrent.Files.Where(m => m.Path.EndsWith(fileName)).Where(m => !String.IsNullOrWhiteSpace(m.Path)).ToList();

            if (matchingTorrentFiles.Count > 0)
            {
                var matchingTorrentFile = matchingTorrentFiles[0];
                var subPath = Path.GetDirectoryName(matchingTorrentFile.Path);

                if (!String.IsNullOrWhiteSpace(subPath))
                {
                    subPath = subPath.Trim('/').Trim('\\');

                    torrentPath = Path.Combine(torrentPath, subPath);
                }
            }
        }
        else if (torrent.Files.Count == 1)
        {
            // Debrid servers such as RealDebrid store single file torrents in a subfolder, but AllDebrid doesn't.
            // We should replicate this behavior so that both folder structures are equal.
            // See issue: https://github.com/rogerfar/rdt-client/issues/648
            if (torrent.ClientKind != Torrent.TorrentClientKind.AllDebrid)
            {
                torrentPath = Path.Combine(downloadPath, directory);
            }
            
            var torrentFile = torrent.Files[0];
            var subPath = Path.GetDirectoryName(torrentFile.Path);
            
            // What we think is a single file torrent may also be a folder with a single file in it.
            // So make sure we handle that here. If this is not the case, torrentPath will be empty below.
            if (!String.IsNullOrWhiteSpace(subPath))
            {
                subPath = subPath.Trim('/').Trim('\\');

                torrentPath = Path.Combine(torrentPath, subPath);
            }
        }

        if (!String.IsNullOrWhiteSpace(torrentPath) && !Directory.Exists(torrentPath))
        {
            Directory.CreateDirectory(torrentPath);
        }

        var filePath = Path.Combine(torrentPath, fileName);

        return filePath;
    }

    public static String? GetDownloadPath(Torrent torrent, Download download)
    {
        return GetDownloadPath("", torrent, download);
    }

    private static String RemoveInvalidPathChars(String path)
    {
        return String.Concat(path.Split(Path.GetInvalidPathChars()));
    }
}
using System.IO.Abstractions;
using RdtClient.Data.Models.Data;
using System.Web;

namespace RdtClient.Service.Helpers;

public static class DownloadHelper
{
    public static String? GetDownloadPath(String downloadPath, Torrent torrent, Download download, IFileSystem? fileSystem = null)
    {
        var fileUrl = download.Link;

        if (String.IsNullOrWhiteSpace(fileUrl) || torrent.RdName == null)
        {
            return null;
        }

        var directory = RemoveInvalidPathChars(torrent.RdName);
        
        var torrentPath = Path.Combine(downloadPath, directory);

        var fileName = GetFileName(download);

        if (fileName == null)
        {
            return null;
        }

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

        fileSystem ??= new FileSystem();

        if (!fileSystem.Directory.Exists(torrentPath))
        {
            fileSystem.Directory.CreateDirectory(torrentPath);
        }

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

        var fileName = download.FileName;

        if (String.IsNullOrWhiteSpace(fileName))
        {
            fileName = uri.Segments.Last();

            fileName = HttpUtility.UrlDecode(fileName);
        }

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

        var filePath = Path.Combine(torrentPath, fileName);

        return filePath;
    }

    public static String? GetFileName(Download download)
    {
        if (String.IsNullOrWhiteSpace(download.Link))
        {
            return null;
        }

        var fileName = download.FileName;

        if (String.IsNullOrWhiteSpace(fileName))
        {
            fileName = HttpUtility.UrlDecode(new Uri(download.Link).Segments.Last());
        }

        return FileHelper.RemoveInvalidFileNameChars(fileName);
    }

    public static String RemoveInvalidPathChars(String path)
    {
        return String.Concat(path.Split(Path.GetInvalidPathChars()));
    }
}

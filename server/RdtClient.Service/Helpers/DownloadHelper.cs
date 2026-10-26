using System.IO.Abstractions;
using System.Web;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.DebridClient;

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

        var matchingTorrentFile = FindTorrentFile(torrent, download, fileName);

        if (matchingTorrentFile != null)
        {
            var subPath = Path.GetDirectoryName(matchingTorrentFile.Path);

            if (!String.IsNullOrWhiteSpace(subPath))
            {
                subPath = subPath.Trim('/').Trim('\\');

                subPath = StripTorrentNamePrefix(subPath, directory);

                if (!String.IsNullOrWhiteSpace(subPath))
                {
                    torrentPath = Path.Combine(torrentPath, subPath);
                }
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

        var matchingTorrentFile = FindTorrentFile(torrent, download, fileName);

        if (matchingTorrentFile != null)
        {
            var subPath = Path.GetDirectoryName(matchingTorrentFile.Path);

            if (!String.IsNullOrWhiteSpace(subPath))
            {
                subPath = subPath.Trim('/').Trim('\\');

                subPath = StripTorrentNamePrefix(subPath, torrentPath);

                if (!String.IsNullOrWhiteSpace(subPath))
                {
                    torrentPath = Path.Combine(torrentPath, subPath);
                }
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

    public static Int64? GetExpectedFileSize(Torrent torrent, Download download)
    {
        var fileName = GetFileName(download);

        if (String.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var matchingTorrentFiles = torrent.Files
                                          .Where(m => !String.IsNullOrWhiteSpace(m.Path) && m.Path.EndsWith(fileName))
                                          .ToList();

        var matchingTorrentFile = matchingTorrentFiles.Count == 1 ? matchingTorrentFiles[0] : FindByRestrictedLink(matchingTorrentFiles, download);

        if (matchingTorrentFile == null)
        {
            return null;
        }

        var size = matchingTorrentFile.Bytes;

        return size > 0 ? size : null;
    }

    /// <summary>
    ///     Finds the torrent file a download belongs to. File names are not unique inside a torrent (audiobook packs have
    ///     "Track 001.mp3" in every folder, season packs "English.srt" in every Subs folder), so when several files share
    ///     the name, the download's restricted link decides. Without a link that identifies the file, the first match is
    ///     used, as before.
    /// </summary>
    private static DebridClientFile? FindTorrentFile(Torrent torrent, Download download, String fileName)
    {
        var matchingTorrentFiles = torrent.Files.Where(m => m.Path.EndsWith(fileName)).Where(m => !String.IsNullOrWhiteSpace(m.Path)).ToList();

        if (matchingTorrentFiles.Count <= 1)
        {
            return matchingTorrentFiles.FirstOrDefault();
        }

        return FindByRestrictedLink(matchingTorrentFiles, download) ?? matchingTorrentFiles[0];
    }

    /// <summary>
    ///     Picks the candidate whose own download link is the download's restricted link (AllDebrid, DebridLink), or else
    ///     whose id ends the restricted link (TorBox: https://torbox.app/fakedl/{torrentId}/{fileId}, Highway:
    ///     .../{service}/{fileId}). Returns null unless exactly one candidate fits.
    /// </summary>
    private static DebridClientFile? FindByRestrictedLink(IList<DebridClientFile> candidates, Download download)
    {
        if (String.IsNullOrWhiteSpace(download.Path))
        {
            return null;
        }

        var byDownloadLink = candidates.Where(m => String.Equals(m.DownloadLink, download.Path, StringComparison.Ordinal)).ToList();

        if (byDownloadLink.Count > 0)
        {
            return byDownloadLink.Count == 1 ? byDownloadLink[0] : null;
        }

        if (!Uri.TryCreate(download.Path, UriKind.Absolute, out var restrictedLink) ||
            !Int64.TryParse(restrictedLink.Segments.Last().Trim('/'), out var fileId))
        {
            return null;
        }

        var byId = candidates.Where(m => m.Id == fileId).ToList();

        return byId.Count == 1 ? byId[0] : null;
    }

    public static String RemoveInvalidPathChars(String path)
    {
        return String.Concat(path.Split(Path.GetInvalidPathChars()));
    }

    private static String StripTorrentNamePrefix(String subPath, String torrentName)
    {
        var separatorIndex = subPath.IndexOfAny(['/', '\\']);

        String firstComponent;
        String remainder;

        if (separatorIndex < 0)
        {
            firstComponent = subPath;
            remainder = String.Empty;
        }
        else
        {
            firstComponent = subPath[..separatorIndex];
            remainder = subPath[(separatorIndex + 1)..];
        }

        var normalizedFirst = RemoveInvalidPathChars(firstComponent);

        if (normalizedFirst.Equals(torrentName.TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase))
        {
            return remainder;
        }

        return subPath;
    }
}

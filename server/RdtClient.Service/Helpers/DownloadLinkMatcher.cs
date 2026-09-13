using System.Web;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Helpers;

public static class DownloadLinkMatcher
{
    /// <summary>
    ///     Finds the provider link that belongs to a download.
    ///     File names are not unique inside a torrent (e.g. "eboot.bin" in several folders), so a name only counts as a
    ///     match when it identifies exactly one link. An ambiguous name returns null instead of guessing, because a guess
    ///     can hand out the link of a sibling download.
    /// </summary>
    public static DownloadInfo? Match(IList<DownloadInfo> infos, Download download)
    {
        if (infos.Count == 0)
        {
            return null;
        }

        var sameLink = infos.FirstOrDefault(i => String.Equals(i.RestrictedLink, download.Path, StringComparison.Ordinal));

        if (sameLink != null)
        {
            return sameLink;
        }

        if (!String.IsNullOrWhiteSpace(download.FileName))
        {
            var byFileName = infos.Where(i => !String.IsNullOrWhiteSpace(i.FileName) &&
                                              String.Equals(i.FileName, download.FileName, StringComparison.OrdinalIgnoreCase))
                                  .ToList();

            if (byFileName.Count > 0)
            {
                return SingleOrNull(byFileName);
            }
        }

        var pathFileName = GetFileNameFromPath(download.Path);

        if (!String.IsNullOrWhiteSpace(pathFileName))
        {
            var byPathFileName = infos.Where(i => (!String.IsNullOrWhiteSpace(i.FileName) &&
                                                   String.Equals(i.FileName, pathFileName, StringComparison.OrdinalIgnoreCase)) ||
                                                  LinkEndsWithFileName(i.RestrictedLink, pathFileName))
                                      .ToList();

            if (byPathFileName.Count > 0)
            {
                return SingleOrNull(byPathFileName);
            }
        }

        if (infos.Count == 1)
        {
            return infos[0];
        }

        return null;
    }

    private static DownloadInfo? SingleOrNull(IList<DownloadInfo> candidates)
    {
        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static Boolean LinkEndsWithFileName(String link, String fileName)
    {
        var linkFileName = GetFileNameFromPath(link);

        return linkFileName != null && String.Equals(linkFileName, fileName, StringComparison.OrdinalIgnoreCase);
    }

    private static String? GetFileNameFromPath(String path)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return HttpUtility.UrlDecode(uri.Segments.Last());
        }

        return Path.GetFileName(path);
    }
}

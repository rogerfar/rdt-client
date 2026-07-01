using System.Web;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Helpers;

public static class DownloadLinkMatcher
{
    public static DownloadInfo? Match(IList<DownloadInfo> infos, Download download)
    {
        if (infos.Count == 0)
        {
            return null;
        }

        if (!String.IsNullOrWhiteSpace(download.FileName))
        {
            var byFileName = infos.FirstOrDefault(i => !String.IsNullOrWhiteSpace(i.FileName) &&
                                                         String.Equals(i.FileName, download.FileName, StringComparison.OrdinalIgnoreCase));

            if (byFileName != null)
            {
                return byFileName;
            }
        }

        var pathFileName = GetFileNameFromPath(download.Path);

        if (!String.IsNullOrWhiteSpace(pathFileName))
        {
            var byPathFileName = infos.FirstOrDefault(i => (!String.IsNullOrWhiteSpace(i.FileName) &&
                                                            String.Equals(i.FileName, pathFileName, StringComparison.OrdinalIgnoreCase)) ||
                                                           LinkEndsWithFileName(i.RestrictedLink, pathFileName));

            if (byPathFileName != null)
            {
                return byPathFileName;
            }
        }

        if (infos.Count == 1)
        {
            return infos[0];
        }

        return null;
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

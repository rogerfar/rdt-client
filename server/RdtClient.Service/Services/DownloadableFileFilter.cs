using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Services;

public interface IDownloadableFileFilter
{
    public Boolean IsDownloadable(Torrent torrent, String filePath, Int64 fileSize);
}

public class DownloadableFileFilter(ILogger<DownloadableFileFilter> logger) : IDownloadableFileFilter
{
    public Boolean IsDownloadable(Torrent torrent, String filePath, Int64 fileSize)
    {
        var isDownloadable = PassesSizeFilter(torrent, filePath, fileSize) &&
                             PassesFilePathFilter(torrent, filePath);

        if (isDownloadable)
        {
            logger.LogDebug("File {filePath} was included after filtering", filePath);
        }
        
        return isDownloadable;
    }

    private Boolean PassesSizeFilter(Torrent torrent, String filePath, Int64 fileSize)
    {
        if (torrent is { ClientKind: Provider.RealDebrid, DownloadAction: TorrentDownloadAction.DownloadManual })
        {
            return true;
        }

        if (torrent.DownloadMinSize <= 0 || fileSize > torrent.DownloadMinSize * 1024 * 1024)
        {
            return true;
        }

        logger.LogDebug("Not downloading file {filePath} file size {fileSize} smaller than minimum {downloadMinSize}", filePath, fileSize, torrent.DownloadMinSize);

        return false;
    }

    private Boolean PassesFilePathFilter(Torrent torrent, String filePath)
    {
        return PassesIncludeRegexFilter(torrent, filePath) && PassesExcludeRegexFilter(torrent, filePath);
    }

    private Boolean PassesIncludeRegexFilter(Torrent torrent, String filePath)
    {
        if (String.IsNullOrWhiteSpace(torrent.IncludeRegex) || Regex.IsMatch(filePath, torrent.IncludeRegex))
        {
            return true;
        }

        logger.LogDebug("Not downloading file {filePath} does not match regex {includeRegex}", filePath, torrent.IncludeRegex);

        return false;
    }

    private Boolean PassesExcludeRegexFilter(Torrent torrent, String filePath)
    {
        // If the IncludeRegex is set, ignore the ExcludeRegex 
        if (!String.IsNullOrWhiteSpace(torrent.IncludeRegex))
        {
            return true;
        }

        if (String.IsNullOrWhiteSpace(torrent.ExcludeRegex) || !Regex.IsMatch(filePath, torrent.ExcludeRegex))
        {
            return true;
        }

        logger.LogDebug("Not downloading file {filePath} matches regex {excludeRegex}", filePath, torrent.ExcludeRegex);

        return false;
    }
}

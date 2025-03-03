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
        var isDownloadable = SatisfiesMinSize(torrent, filePath, fileSize) &&
                             SatisfiesFileNameRegex(torrent, filePath);

        if (isDownloadable)
        {
            logger.LogDebug("File {filePath} was included after filtering", filePath);
        }
        
        return isDownloadable;
    }

    private Boolean SatisfiesMinSize(Torrent torrent, String filePath, Int64 fileSize)
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

    private Boolean SatisfiesFileNameRegex(Torrent torrent, String filePath)
    {
        return IncludeFileName(torrent, filePath) && ExcludeFileName(torrent, filePath);
    }

    private Boolean IncludeFileName(Torrent torrent, String filePath)
    {
        if (String.IsNullOrWhiteSpace(torrent.IncludeRegex) || Regex.IsMatch(filePath, torrent.IncludeRegex))
        {
            return true;
        }

        logger.LogDebug("Not downloading file {filePath} does not match regex {includeRegex}", filePath, torrent.IncludeRegex);

        return false;
    }

    private Boolean ExcludeFileName(Torrent torrent, String filePath)
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

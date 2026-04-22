using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Helpers;

public static class TorrentDtoMapper
{
    public static TorrentDto ToListDto(Torrent torrent, Func<Guid, (Int64 Speed, Int64 BytesTotal, Int64 BytesDone)> getDownloadStats)
    {
        return ToDto(torrent, getDownloadStats, includeDownloads: false, includeFiles: false, includeFileOrMagnet: false);
    }

    public static TorrentDto ToUpdateDto(Torrent torrent, Func<Guid, (Int64 Speed, Int64 BytesTotal, Int64 BytesDone)> getDownloadStats)
    {
        return ToDto(torrent, getDownloadStats, includeDownloads: true, includeFiles: false, includeFileOrMagnet: false);
    }

    public static TorrentDto ToDetailDto(Torrent torrent, Func<Guid, (Int64 Speed, Int64 BytesTotal, Int64 BytesDone)> getDownloadStats)
    {
        return ToDto(torrent, getDownloadStats, includeDownloads: true, includeFiles: true, includeFileOrMagnet: true);
    }

    private static TorrentDto ToDto(Torrent torrent,
                                    Func<Guid, (Int64 Speed, Int64 BytesTotal, Int64 BytesDone)> getDownloadStats,
                                    Boolean includeDownloads,
                                    Boolean includeFiles,
                                    Boolean includeFileOrMagnet)
    {
        var downloads = includeDownloads ? torrent.Downloads.Select(download => ToDto(download, getDownloadStats)).ToList() : [];

        return new()
        {
            TorrentId = torrent.TorrentId,
            Hash = torrent.Hash,
            Category = torrent.Category,
            DownloadAction = torrent.DownloadAction,
            FinishedAction = torrent.FinishedAction,
            FinishedActionDelay = torrent.FinishedActionDelay,
            HostDownloadAction = torrent.HostDownloadAction,
            DownloadMinSize = torrent.DownloadMinSize,
            IncludeRegex = torrent.IncludeRegex,
            ExcludeRegex = torrent.ExcludeRegex,
            DownloadManualFiles = torrent.DownloadManualFiles,
            DownloadClient = torrent.DownloadClient,
            Added = torrent.Added,
            FilesSelected = torrent.FilesSelected,
            Completed = torrent.Completed,
            Type = torrent.Type,
            FileOrMagnet = includeFileOrMagnet ? torrent.FileOrMagnet : null,
            IsFile = torrent.IsFile,
            Priority = torrent.Priority,
            RetryCount = torrent.RetryCount,
            DownloadRetryAttempts = torrent.DownloadRetryAttempts,
            TorrentRetryAttempts = torrent.TorrentRetryAttempts,
            DeleteOnError = torrent.DeleteOnError,
            Lifetime = torrent.Lifetime,
            Error = torrent.Error,
            RdId = torrent.RdId,
            RdName = torrent.RdName,
            RdSize = torrent.RdSize,
            RdHost = torrent.RdHost,
            RdSplit = torrent.RdSplit,
            RdProgress = torrent.RdProgress,
            RdStatus = torrent.RdStatus,
            RdStatusRaw = torrent.RdStatusRaw,
            RdAdded = torrent.RdAdded,
            RdEnded = torrent.RdEnded,
            RdSpeed = torrent.RdSpeed,
            RdSeeders = torrent.RdSeeders,
            StatusText = GetStatusText(torrent, getDownloadStats),
            FilesCount = torrent.Files.Count,
            DownloadsCount = torrent.Downloads.Count,
            Files = includeFiles ? torrent.Files : [],
            Downloads = downloads
        };
    }

    private static DownloadDto ToDto(Download download, Func<Guid, (Int64 Speed, Int64 BytesTotal, Int64 BytesDone)> getDownloadStats)
    {
        var (speed, bytesTotal, bytesDone) = getDownloadStats(download.DownloadId);

        return new()
        {
            DownloadId = download.DownloadId,
            TorrentId = download.TorrentId,
            Path = download.Path,
            Link = download.Link,
            Added = download.Added,
            DownloadQueued = download.DownloadQueued,
            DownloadStarted = download.DownloadStarted,
            DownloadFinished = download.DownloadFinished,
            UnpackingQueued = download.UnpackingQueued,
            UnpackingStarted = download.UnpackingStarted,
            UnpackingFinished = download.UnpackingFinished,
            Completed = download.Completed,
            RetryCount = download.RetryCount,
            Error = download.Error,
            BytesTotal = bytesTotal,
            BytesDone = bytesDone,
            Speed = speed
        };
    }

    private static String GetStatusText(Torrent torrent, Func<Guid, (Int64 Speed, Int64 BytesTotal, Int64 BytesDone)> getDownloadStats)
    {
        if (!String.IsNullOrWhiteSpace(torrent.Error))
        {
            return torrent.Error;
        }

        if (torrent.Downloads.Count > 0)
        {
            var allFinished = true;
            var downloadingCount = 0;
            var downloadedCount = 0;
            Int64 downloadingBytesDone = 0;
            Int64 downloadingBytesTotal = 0;
            Int64 downloadingSpeed = 0;
            var unpackingCount = 0;
            var unpackedCount = 0;
            Int64 unpackingBytesDone = 0;
            Int64 unpackingBytesTotal = 0;
            var queuedForUnpackingCount = 0;
            var queuedForDownloadingCount = 0;

            foreach (var download in torrent.Downloads)
            {
                if (download.Completed == null)
                {
                    allFinished = false;
                }

                var (speed, bytesTotal, bytesDone) = getDownloadStats(download.DownloadId);

                if (download.DownloadFinished != null)
                {
                    downloadedCount += 1;
                }

                if (download.DownloadStarted != null && download.DownloadFinished == null && bytesDone > 0)
                {
                    downloadingCount += 1;
                    downloadingBytesDone += bytesDone;
                    downloadingBytesTotal += bytesTotal;
                    downloadingSpeed += speed;
                }

                if (download.UnpackingFinished != null)
                {
                    unpackedCount += 1;
                }

                if (download.UnpackingStarted != null && download.UnpackingFinished == null && bytesDone > 0)
                {
                    unpackingCount += 1;
                    unpackingBytesDone += bytesDone;
                    unpackingBytesTotal += bytesTotal;
                }

                if (download.UnpackingQueued != null && download.UnpackingStarted == null)
                {
                    queuedForUnpackingCount += 1;
                }

                if (download.DownloadStarted == null && download.DownloadFinished == null)
                {
                    queuedForDownloadingCount += 1;
                }
            }

            if (allFinished)
            {
                return "Finished";
            }

            if (downloadingCount > 0)
            {
                var progress = downloadingBytesTotal == 0 ? 0 : (Double)downloadingBytesDone / downloadingBytesTotal * 100;

                return $"Downloading file {downloadingCount + downloadedCount}/{torrent.Downloads.Count} ({progress:0.00}% - {FileSizeHelper.FormatSize(downloadingSpeed)}/s)";
            }

            if (unpackingCount > 0)
            {
                var progress = unpackingBytesTotal == 0 ? 0 : (Double)unpackingBytesDone / unpackingBytesTotal * 100;

                return $"Extracting file {unpackingCount + unpackedCount}/{torrent.Downloads.Count} ({progress:0.00}%)";
            }

            if (queuedForUnpackingCount > 0)
            {
                return "Queued for unpacking";
            }

            if (queuedForDownloadingCount > 0)
            {
                return "Queued for downloading";
            }

            if (unpackedCount > 0)
            {
                return "Files unpacked";
            }

            if (downloadedCount > 0)
            {
                return "Files downloaded to host";
            }
        }

        if (torrent.Completed != null)
        {
            return "Finished";
        }

        return torrent.RdStatus switch
        {
            TorrentStatus.Queued => "Not Yet Added to Provider",
            TorrentStatus.Downloading when torrent.RdSeeders < 1 && torrent.Type != DownloadType.Nzb => "Torrent stalled",
            TorrentStatus.Downloading => $"Torrent downloading ({torrent.RdProgress}% - {FileSizeHelper.FormatSize(torrent.RdSpeed)}/s)",
            TorrentStatus.Processing => "Torrent processing",
            TorrentStatus.WaitingForFileSelection => "Torrent waiting for file selection",
            TorrentStatus.Error => $"Torrent error: {torrent.RdStatusRaw}",
            TorrentStatus.Finished => "Torrent finished, waiting for download links",
            TorrentStatus.Uploading => "Torrent uploading",
            _ => "Unknown status"
        };
    }
}

using Microsoft.AspNetCore.SignalR;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Services;

public class RemoteService(IHubContext<RdtHub> hub, Torrents torrents)
{
    public async Task Update()
    {
        var allTorrents = await torrents.Get();

        var torrentDtos = allTorrents.Select(torrent => new TorrentDto
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
            Files = torrent.Files,
            Downloads = torrent.Downloads.Select(download =>
            {
                var (speed, bytesTotal, bytesDone) = torrents.GetDownloadStats(download.DownloadId);

                return new DownloadDto
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
            }).ToList()
        }).ToList();
        await hub.Clients.All.SendCoreAsync("update",
        [
            torrentDtos
        ]);
    }

    public async Task UpdateDiskSpaceStatus(Object status)
    {
        await hub.Clients.All.SendCoreAsync("diskSpaceStatus", [status]);
    }

    public async Task UpdateRateLimitStatus(RateLimitStatus status)
    {
        await hub.Clients.All.SendCoreAsync("rateLimitStatus", [status]);
    }
}

using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Services;

public interface IDownloads
{
    Task<List<Download>> GetForTorrent(Guid torrentId);
    Task<Download?> GetById(Guid downloadId);
    Task<Download?> Get(Guid torrentId, String path);
    Task<Download> Add(Guid torrentId, DownloadInfo downloadInfo);
    Task UpdateUnrestrictedLink(Guid downloadId, String unrestrictedLink);
    Task UpdateFileName(Guid downloadId, String fileName);
    Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateError(Guid downloadId, String? error);
    Task UpdateRetryCount(Guid downloadId, Int32 retryCount);
    Task UpdateRemoteId(Guid downloadId, String remoteId);
    Task DeleteForTorrent(Guid torrentId);
    Task Reset(Guid downloadId);
}

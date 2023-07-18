using RdtClient.Data.Data;
using Download = RdtClient.Data.Models.Data.Download;

namespace RdtClient.Service.Services;

public class Downloads 
{
    private readonly DownloadData _downloadData;

    public Downloads(DownloadData downloadData)
    {
        _downloadData = downloadData;
    }

    public async Task<List<Download>> GetForTorrent(Guid torrentId)
    {
        return await _downloadData.GetForTorrent(torrentId);
    }

    public async Task<Download?> GetById(Guid downloadId)
    {
        return await _downloadData.GetById(downloadId);
    }

    public async Task<Download?> Get(Guid torrentId, String path)
    {
        return await _downloadData.Get(torrentId, path);
    }

    public async Task<Download> Add(Guid torrentId, String path, String folder )
    {
        return await _downloadData.Add(torrentId, path, folder);
    }

    public async Task UpdateUnrestrictedLink(Guid downloadId, String unrestrictedLink)
    {
        await _downloadData.UpdateUnrestrictedLink(downloadId, unrestrictedLink);
    }

    public async Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime)
    {
        await _downloadData.UpdateDownloadStarted(downloadId, dateTime);
    }

    public async Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime)
    {
        await _downloadData.UpdateDownloadFinished(downloadId, dateTime);
    }

    public async Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime)
    {
        await _downloadData.UpdateUnpackingQueued(downloadId, dateTime);
    }

    public async Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime)
    {
        await _downloadData.UpdateUnpackingStarted(downloadId, dateTime);
    }

    public async Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime)
    {
        await _downloadData.UpdateUnpackingFinished(downloadId, dateTime);
    }

    public async Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime)
    {
        await _downloadData.UpdateCompleted(downloadId, dateTime);
    }

    public async Task UpdateError(Guid downloadId, String? error)
    {
        await _downloadData.UpdateError(downloadId, error);
    }

    public async Task UpdateRetryCount(Guid downloadId, Int32 retryCount)
    {
        await _downloadData.UpdateRetryCount(downloadId, retryCount);
    }

    public async Task UpdateRemoteId(Guid downloadId, String remoteId)
    {
        await _downloadData.UpdateRemoteId(downloadId, remoteId);
    }

    public async Task DeleteForTorrent(Guid torrentId)
    {
        await _downloadData.DeleteForTorrent(torrentId);
    }
        
    public async Task Reset(Guid downloadId)
    {
        await _downloadData.Reset(downloadId);
    }
}
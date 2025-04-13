using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;
using Download = RdtClient.Data.Models.Data.Download;

namespace RdtClient.Service.Services;

public class Downloads(DownloadData downloadData) : IDownloads
{
    public async Task<List<Download>> GetForTorrent(Guid torrentId)
    {
        return await downloadData.GetForTorrent(torrentId);
    }

    public async Task<Download?> GetById(Guid downloadId)
    {
        return await downloadData.GetById(downloadId);
    }

    public async Task<Download?> Get(Guid torrentId, String path)
    {
        return await downloadData.Get(torrentId, path);
    }

    public async Task<Download> Add(Guid torrentId, DownloadInfo downloadInfo)
    {
        return await downloadData.Add(torrentId, downloadInfo);
    }

    public async Task UpdateUnrestrictedLink(Guid downloadId, String unrestrictedLink)
    {
        await downloadData.UpdateUnrestrictedLink(downloadId, unrestrictedLink);
    }

    public async Task UpdateFileName(Guid downloadId, String fileName)
    {
        await downloadData.UpdateFileName(downloadId, fileName);
    }

    public async Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime)
    {
        await downloadData.UpdateDownloadStarted(downloadId, dateTime);
    }

    public async Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime)
    {
        await downloadData.UpdateDownloadFinished(downloadId, dateTime);
    }

    public async Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime)
    {
        await downloadData.UpdateUnpackingQueued(downloadId, dateTime);
    }

    public async Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime)
    {
        await downloadData.UpdateUnpackingStarted(downloadId, dateTime);
    }

    public async Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime)
    {
        await downloadData.UpdateUnpackingFinished(downloadId, dateTime);
    }

    public async Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime)
    {
        await downloadData.UpdateCompleted(downloadId, dateTime);
    }

    public async Task UpdateError(Guid downloadId, String? error)
    {
        await downloadData.UpdateError(downloadId, error);
    }
    
    public async Task UpdateRetryCount(Guid downloadId, Int32 retryCount)
    {
        await downloadData.UpdateRetryCount(downloadId, retryCount);
    }

    public async Task UpdateRemoteId(Guid downloadId, String remoteId)
    {
        await downloadData.UpdateRemoteId(downloadId, remoteId);
    }
    
    public async Task DeleteForTorrent(Guid torrentId)
    {
        await downloadData.DeleteForTorrent(torrentId);
    }
        
    public async Task Reset(Guid downloadId)
    {
        await downloadData.Reset(downloadId);
    }
}
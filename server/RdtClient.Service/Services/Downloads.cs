using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RdtClient.Data.Data;
using Download = RdtClient.Data.Models.Data.Download;

namespace RdtClient.Service.Services
{
    public interface IDownloads
    {
        Task<IList<Download>> Get();
        Task<IList<Download>> GetForTorrent(Guid torrentId);
        Task<Download> GetById(Guid downloadId);
        Task<Download> Add(Guid torrentId, String link);
        Task UpdateUnrestrictedLink(Guid downloadId, String unrestrictedLink);
        Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateError(Guid downloadId, String error);
        Task DeleteForTorrent(Guid torrentId);
    }

    public class Downloads : IDownloads
    {
        private readonly IDownloadData _downloadData;

        public Downloads(IDownloadData downloadData)
        {
            _downloadData = downloadData;
        }

        public async Task<IList<Download>> Get()
        {
            return await _downloadData.Get();
        }

        public async Task<IList<Download>> GetForTorrent(Guid torrentId)
        {
            return await _downloadData.GetForTorrent(torrentId);
        }

        public async Task<Download> GetById(Guid downloadId)
        {
            return await _downloadData.GetById(downloadId);
        }

        public async Task<Download> Add(Guid torrentId, String link)
        {
            return await _downloadData.Add(torrentId, link);
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

        public async Task UpdateError(Guid downloadId, String error)
        {
            await _downloadData.UpdateError(downloadId, error);
        }

        public async Task DeleteForTorrent(Guid torrentId)
        {
            await _downloadData.DeleteForTorrent(torrentId);
        }
    }
}

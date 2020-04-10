using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Services
{
    public interface IDownloads
    {
        Task<IList<Download>> Get();
        Task<IList<Download>> GetForTorrent(Guid torrentId);
        Task<Download> Add(Guid torrentId, String link);
        Task UpdateStatus(Guid downloadId, DownloadStatus status);
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

        public async Task<Download> Add(Guid torrentId, String link)
        {
            return await _downloadData.Add(torrentId, link);
        }

        public async Task UpdateStatus(Guid downloadId, DownloadStatus status)
        {
            await _downloadData.UpdateStatus(downloadId, status);
        }

        public async Task DeleteForTorrent(Guid torrentId)
        {
            await _downloadData.DeleteForTorrent(torrentId);
        }
    }
}

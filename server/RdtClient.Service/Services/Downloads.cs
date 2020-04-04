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
        Task<Download> Add(Guid torrentId, String link);
        Task UpdateStatus(Guid downloadId, DownloadStatus status);
        Task DeleteForTorrent(Guid torrentId);
    }

    public class Downloads : IDownloads
    {
        private readonly IDownloadData _downloadData;
        private readonly ISettings _settings;

        public Downloads(IDownloadData downloadData, ISettings settings)
        {
            _downloadData = downloadData;
            _settings = settings;
        }

        public async Task<IList<Download>> Get()
        {
            return await _downloadData.Get();
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

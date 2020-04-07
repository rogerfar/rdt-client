using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data
{
    public interface IDownloadData
    {
        Task<IList<Download>> Get();
        Task<Download> Add(Guid torrentId, String link);
        Task UpdateStatus(Guid downloadId, DownloadStatus status);
        Task DeleteForTorrent(Guid torrentId);
    }

    public class DownloadData : IDownloadData
    {
        private readonly DataContext _dataContext;

        public DownloadData(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IList<Download>> Get()
        {
            return await _dataContext.Downloads
                                     .AsNoTracking()
                                     .Include(m => m.Torrent)
                                     .ToListAsync();
        }

        public async Task<Download> Add(Guid torrentId, String link)
        {
            var download = new Download
            {
                DownloadId = Guid.NewGuid(),
                TorrentId = torrentId,
                Link = link,
                Added = DateTimeOffset.UtcNow,
                Status = DownloadStatus.PendingDownload
            };

            _dataContext.Downloads.Add(download);

            await _dataContext.SaveChangesAsync();

            return download;
        }

        public async Task UpdateStatus(Guid downloadId, DownloadStatus status)
        {
            var download = await _dataContext.Downloads.FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            download.Status = status;

            await _dataContext.SaveChangesAsync();
        }

        public async Task DeleteForTorrent(Guid torrentId)
        {
            var downloads = await _dataContext.Downloads
                                              .Where(m => m.TorrentId == torrentId)
                                              .ToListAsync();

            _dataContext.Downloads.RemoveRange(downloads);

            await _dataContext.SaveChangesAsync();
        }
    }
}
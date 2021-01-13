using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data
{
    public interface IDownloadData
    {
        Task<IList<Download>> Get();
        Task<IList<Download>> GetForTorrent(Guid torrentId);
        Task<Download> GetById(Guid downloadId);
        Task<Download> Add(Guid torrentId, String link);
        Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime);
        Task UpdateError(Guid downloadId, String error);
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

        public async Task<IList<Download>> GetForTorrent(Guid torrentId)
        {
            return await _dataContext.Downloads
                                     .AsNoTracking()
                                     .Where(m => m.TorrentId == torrentId)
                                     .ToListAsync();
        }

        public async Task<Download> GetById(Guid downloadId)
        {
            return await _dataContext.Downloads
                                     .Include(m => m.Torrent)
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(m => m.DownloadId == downloadId);
        }

        public async Task<Download> Add(Guid torrentId, String link)
        {
            var download = new Download
            {
                DownloadId = Guid.NewGuid(),
                TorrentId = torrentId,
                Link = link,
                Added = DateTimeOffset.UtcNow,
                DownloadQueued = DateTimeOffset.UtcNow
            };

            await _dataContext.Downloads.AddAsync(download);

            await _dataContext.SaveChangesAsync();

            return download;
        }
        
        public async Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.DownloadStarted = dateTime;

            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.DownloadFinished = dateTime;

            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.UnpackingQueued = dateTime;

            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.UnpackingStarted = dateTime;

            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.UnpackingFinished = dateTime;

            await _dataContext.SaveChangesAsync();
        }
        
        public async Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.Completed = dateTime;

            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateError(Guid downloadId, String error)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.Error = error;

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
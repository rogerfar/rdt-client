using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Download = RdtClient.Data.Models.Data.Download;

namespace RdtClient.Data.Data
{
    public class DownloadData
    {
        private readonly DataContext _dataContext;
        private readonly TorrentData _torrentData;

        public DownloadData(DataContext dataContext, TorrentData torrentData)
        {
            _dataContext = dataContext;
            _torrentData = torrentData;
        }

        public async Task<Download> GetById(Guid downloadId)
        {
            return await _dataContext.Downloads
                                     .Include(m => m.Torrent)
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(m => m.DownloadId == downloadId);
        }

        public async Task<Download> Get(Guid torrentId, String path)
        {
            return await _dataContext.Downloads
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(m => m.TorrentId == torrentId && m.Path == path);
        }

        public async Task<Download> Add(Guid torrentId, String path)
        {
            var download = new Download
            {
                DownloadId = Guid.NewGuid(),
                TorrentId = torrentId,
                Path = path,
                Added = DateTimeOffset.UtcNow,
                DownloadQueued = DateTimeOffset.UtcNow,
                RetryCount = 0
            };

            await _dataContext.Downloads.AddAsync(download);

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();

            return download;
        }

        public async Task UpdateUnrestrictedLink(Guid downloadId, String unrestrictedLink)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.Link = unrestrictedLink;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.DownloadStarted = dateTime;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            if (dbDownload == null)
            {
                return;
            }
            
            dbDownload.DownloadFinished = dateTime;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.UnpackingQueued = dateTime;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.UnpackingStarted = dateTime;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.UnpackingFinished = dateTime;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }
        
        public async Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.Completed = dateTime;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task UpdateError(Guid downloadId, String error)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.Error = error;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task UpdateRetryCount(Guid downloadId, Int32 retryCount)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.RetryCount = retryCount;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task DeleteForTorrent(Guid torrentId)
        {
            var downloads = await _dataContext.Downloads
                                              .Where(m => m.TorrentId == torrentId)
                                              .ToListAsync();

            _dataContext.Downloads.RemoveRange(downloads);

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }

        public async Task Reset(Guid downloadId)
        {
            var dbDownload = await _dataContext.Downloads
                                               .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            dbDownload.RetryCount = 0;
            dbDownload.Link = null;
            dbDownload.Added = DateTimeOffset.UtcNow;
            dbDownload.DownloadQueued = DateTimeOffset.UtcNow;
            dbDownload.DownloadStarted = null;
            dbDownload.DownloadFinished = null;
            dbDownload.UnpackingQueued = null;
            dbDownload.UnpackingStarted = null;
            dbDownload.UnpackingFinished = null;
            dbDownload.Completed = null;
            dbDownload.Error = null;

            await _dataContext.SaveChangesAsync();

            await _torrentData.VoidCache();
        }
    }
}
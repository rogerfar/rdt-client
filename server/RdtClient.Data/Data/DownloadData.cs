using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Models.Data;
using Download = RdtClient.Data.Models.Data.Download;

namespace RdtClient.Data.Data;

public class DownloadData(DataContext dataContext, ILogger<DownloadData>? logger = null)
{
    public async Task<List<Download>> GetForTorrent(Guid torrentId)
    {
        return await dataContext.Downloads
                                .AsNoTracking()
                                .Where(m => m.TorrentId == torrentId)
                                .ToListAsync();
    }

    public async Task<Download?> GetById(Guid downloadId)
    {
        return await dataContext.Downloads
                                .Include(m => m.Torrent)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(m => m.DownloadId == downloadId);
    }

    public async Task<Download?> Get(Guid torrentId, String path)
    {
        return await dataContext.Downloads
                                .Include(m => m.Torrent)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(m => m.TorrentId == torrentId && m.Path == path);
    }

    public async Task<DownloadAddResult> TryAddForTorrent(Guid torrentId, DownloadInfo downloadInfo)
    {
        if (String.IsNullOrWhiteSpace(downloadInfo.RestrictedLink))
        {
            logger?.LogDebug("Skipped download creation because the restricted link was blank. TorrentId: {torrentId}", torrentId);

            return DownloadAddResult.InvalidInput;
        }

        if (!await dataContext.Torrents.AsNoTracking().AnyAsync(m => m.TorrentId == torrentId))
        {
            logger?.LogDebug("Skipped download creation because the torrent no longer exists. TorrentId: {torrentId}, Path: {path}", torrentId, downloadInfo.RestrictedLink);

            return DownloadAddResult.TorrentMissing;
        }

        if (await dataContext.Downloads.AsNoTracking().AnyAsync(m => m.TorrentId == torrentId && m.Path == downloadInfo.RestrictedLink))
        {
            logger?.LogDebug("Skipped download creation because it already exists. TorrentId: {torrentId}, Path: {path}", torrentId, downloadInfo.RestrictedLink);

            return DownloadAddResult.AlreadyExists;
        }

        var download = new Download
        {
            DownloadId = Guid.NewGuid(),
            TorrentId = torrentId,
            FileName = downloadInfo.FileName,
            Path = downloadInfo.RestrictedLink,
            Added = DateTimeOffset.UtcNow,
            DownloadQueued = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        await dataContext.Downloads.AddAsync(download);

        try
        {
            await dataContext.SaveChangesAsync();

            return DownloadAddResult.Added;
        }
        // These shouldn't be possible any longer, but added for safety and until confirmed.
        catch (DbUpdateException ex)
        {
            dataContext.Entry(download).State = EntityState.Detached;

            if (IsDuplicateDownloadViolation(ex))
            {
                logger?.LogDebug("Skipped download creation after a concurrent duplicate insert. TorrentId: {torrentId}, Path: {path}", torrentId, downloadInfo.RestrictedLink);

                return DownloadAddResult.AlreadyExists;
            }

            if (IsForeignKeyViolation(ex) && !await dataContext.Torrents.AsNoTracking().AnyAsync(m => m.TorrentId == torrentId))
            {
                logger?.LogDebug("Skipped download creation after the torrent was deleted concurrently. TorrentId: {torrentId}, Path: {path}", torrentId, downloadInfo.RestrictedLink);

                return DownloadAddResult.TorrentMissing;
            }

            throw;
        }
    }

    public async Task UpdateUnrestrictedLink(Guid downloadId, String unrestrictedLink)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.Link = unrestrictedLink;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateFileName(Guid downloadId, String fileName)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.FileName = fileName;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.DownloadStarted = dateTime;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.DownloadFinished = dateTime;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.UnpackingQueued = dateTime;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.UnpackingStarted = dateTime;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.UnpackingFinished = dateTime;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.Completed = dateTime;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateError(Guid downloadId, String? error)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.Error = error;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateRetryCount(Guid downloadId, Int32 retryCount)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.RetryCount = retryCount;

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateRemoteId(Guid downloadId, String remoteId)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.RemoteId = remoteId;

        await dataContext.SaveChangesAsync();
    }

    public async Task DeleteForTorrent(Guid torrentId)
    {
        var downloads = await dataContext.Downloads
                                         .Where(m => m.TorrentId == torrentId)
                                         .ToListAsync();

        dataContext.Downloads.RemoveRange(downloads);

        await dataContext.SaveChangesAsync();
    }

    public async Task Reset(Guid downloadId)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId)
                         ?? throw new($"Cannot find download with ID {downloadId}");

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

        await dataContext.SaveChangesAsync();
    }

    private static Boolean IsDuplicateDownloadViolation(DbUpdateException exception)
    {
        var sqliteException = exception.InnerException as SqliteException;

        return sqliteException?.SqliteExtendedErrorCode == 2067
               || sqliteException?.Message.Contains("UNIQUE constraint failed: Downloads.TorrentId, Downloads.Path", StringComparison.Ordinal) == true;
    }

    private static Boolean IsForeignKeyViolation(DbUpdateException exception)
    {
        var sqliteException = exception.InnerException as SqliteException;

        return sqliteException?.SqliteExtendedErrorCode == 787
               || sqliteException?.Message.Contains("FOREIGN KEY constraint failed", StringComparison.Ordinal) == true;
    }
}

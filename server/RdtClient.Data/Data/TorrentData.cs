using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data;

public class TorrentData
{
    private static IList<Torrent>? _torrentCache;

    private static readonly SemaphoreSlim TorrentCacheLock = new(1, 1);

    private readonly DataContext _dataContext;

    public TorrentData(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<IList<Torrent>> Get()
    {
        await TorrentCacheLock.WaitAsync();

        try
        {
            _torrentCache ??= await _dataContext.Torrents
                                                .AsNoTracking()
                                                .Include(m => m.Downloads)
                                                .ToListAsync();

            return _torrentCache.OrderBy(m => m.Priority ?? 9999).ThenBy(m => m.Added).ToList();
        }
        finally
        {
            TorrentCacheLock.Release();
        }
    }

    public async Task<Torrent?> GetById(Guid torrentId)
    {
        var dbTorrent = await _dataContext.Torrents
                                          .AsNoTracking()
                                          .Include(m => m.Downloads)
                                          .FirstOrDefaultAsync(m => m.TorrentId == torrentId);

        if (dbTorrent == null)
        {
            return null;
        }

        foreach (var file in dbTorrent.Downloads)
        {
            file.Torrent = null;
        }

        return dbTorrent;
    }

    public async Task<Torrent?> GetByHash(String hash)
    {
        var dbTorrent = await _dataContext.Torrents
                                          .AsNoTracking()
                                          .Include(m => m.Downloads)
                                          .FirstOrDefaultAsync(m => m.Hash.ToLower() == hash.ToLower());

        if (dbTorrent == null)
        {
            return null;
        }

        foreach (var file in dbTorrent.Downloads)
        {
            file.Torrent = null;
        }

        return dbTorrent;
    }

    public async Task<Torrent> Add(String realDebridId,
                                   String hash,
                                   String? fileOrMagnetContents,
                                   Boolean isFile,
                                   Torrent torrent)
    {
        var newTorrent = new Torrent
        {
            TorrentId = Guid.NewGuid(),
            Added = DateTimeOffset.UtcNow,
            RdId = realDebridId,
            Hash = hash.ToLower(),
            Category = torrent.Category,
            HostDownloadAction = torrent.HostDownloadAction,
            DownloadAction = torrent.DownloadAction,
            FinishedAction = torrent.FinishedAction,
            DownloadMinSize = torrent.DownloadMinSize,
            DownloadManualFiles = torrent.DownloadManualFiles,
            FileOrMagnet = fileOrMagnetContents,
            IsFile = isFile,
            Priority = torrent.Priority,
            TorrentRetryAttempts = torrent.TorrentRetryAttempts,
            DownloadRetryAttempts = torrent.DownloadRetryAttempts,
            DeleteOnError = torrent.DeleteOnError,
            Lifetime = torrent.Lifetime
        };

        await _dataContext.Torrents.AddAsync(newTorrent);

        await _dataContext.SaveChangesAsync();

        await VoidCache();

        return newTorrent;
    }

    public async Task UpdateRdData(Torrent torrent)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrent.TorrentId);

        if (dbTorrent == null)
        {
            return;
        }

        dbTorrent.RdName = torrent.RdName;
        dbTorrent.RdSize = torrent.RdSize;
        dbTorrent.RdHost = torrent.RdHost;
        dbTorrent.RdSplit = torrent.RdSplit;
        dbTorrent.RdProgress = torrent.RdProgress;
        dbTorrent.RdStatus = torrent.RdStatus;
        dbTorrent.RdStatusRaw = torrent.RdStatusRaw;
        dbTorrent.RdAdded = torrent.RdAdded;
        dbTorrent.RdEnded = torrent.RdEnded;
        dbTorrent.RdSpeed = torrent.RdSpeed;
        dbTorrent.RdSeeders = torrent.RdSeeders;
        dbTorrent.RdFiles = torrent.RdFiles;
        
        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public async Task Update(Torrent torrent)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrent.TorrentId);

        if (dbTorrent == null)
        {
            return;
        }

        dbTorrent.HostDownloadAction = torrent.HostDownloadAction;
        dbTorrent.Category = torrent.Category;
        dbTorrent.Priority = torrent.Priority;
        dbTorrent.DownloadRetryAttempts = torrent.DownloadRetryAttempts;
        dbTorrent.TorrentRetryAttempts = torrent.TorrentRetryAttempts;
        dbTorrent.DeleteOnError = torrent.DeleteOnError;
        dbTorrent.Lifetime = torrent.Lifetime;

        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public async Task UpdateCategory(Guid torrentId, String? category)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

        if (dbTorrent == null)
        {
            return;
        }

        dbTorrent.Category = category;

        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public async Task UpdateComplete(Guid torrentId, String? error, DateTimeOffset? datetime, Boolean retry)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

        if (dbTorrent == null)
        {
            return;
        }

        if (String.IsNullOrWhiteSpace(error))
        {
            var downloads = await _dataContext.Downloads.AsNoTracking().Where(m => m.TorrentId == torrentId).ToListAsync();
            var downloadWithErrors = downloads.Where(m => !String.IsNullOrWhiteSpace(m.Error)).ToList();

            if (downloadWithErrors.Any())
            {
                error = $"{downloadWithErrors.Count}/{downloads.Count} downloads failed with errors";
            }
        }

        if (!String.IsNullOrWhiteSpace(error) && retry)
        {
            if (dbTorrent.RetryCount < dbTorrent.TorrentRetryAttempts)
            {
                dbTorrent.RetryCount += 1;
                dbTorrent.Retry = DateTime.UtcNow;
            }
        }

        dbTorrent.Completed = datetime;
        dbTorrent.Error = error;

        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public async Task UpdateFilesSelected(Guid torrentId, DateTimeOffset datetime)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

        if (dbTorrent == null)
        {
            return;
        }

        dbTorrent.FilesSelected = datetime;

        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public async Task UpdatePriority(Guid torrentId, Int32? priority)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

        if (dbTorrent == null)
        {
            return;
        }

        dbTorrent.Priority = priority;

        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public async Task UpdateRetry(Guid torrentId, DateTimeOffset? dateTime, Int32 retryCount)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

        if (dbTorrent == null)
        {
            return;
        }

        dbTorrent.RetryCount = retryCount;
        dbTorrent.Retry = dateTime;

        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public async Task UpdateError(Guid torrentId, String error)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

        if (dbTorrent == null)
        {
            return;
        }

        dbTorrent.Error = error;

        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public async Task Delete(Guid torrentId)
    {
        var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

        if (dbTorrent == null)
        {
            return;
        }

        _dataContext.Torrents.Remove(dbTorrent);

        await _dataContext.SaveChangesAsync();

        await VoidCache();
    }

    public static async Task VoidCache()
    {
        await TorrentCacheLock.WaitAsync();

        try
        {
            _torrentCache = null;
        }
        finally
        {
            TorrentCacheLock.Release();
        }
    }
}
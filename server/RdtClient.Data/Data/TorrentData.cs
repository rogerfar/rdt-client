using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data
{
    public interface ITorrentData
    {
        Task<IList<Torrent>> Get();
        Task<Torrent> GetById(Guid torrentId);
        Task<Torrent> GetByHash(String hash);
        Task<Torrent> Add(String realDebridId, String hash, String category, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete);
        Task UpdateRdData(Torrent torrent);
        Task UpdateCategory(Guid torrentId, String category);
        Task UpdateComplete(Guid torrentId, DateTimeOffset datetime);
        Task Delete(Guid torrentId);
    }

    public class TorrentData : ITorrentData
    {
        private readonly DataContext _dataContext;

        public TorrentData(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IList<Torrent>> Get()
        {
            var results = await _dataContext.Torrents
                                            .AsNoTracking()
                                            .Include(m => m.Downloads)
                                            .ToListAsync();

            return results.OrderByDescending(m => m.Added).ToList();
        }

        public async Task<Torrent> GetById(Guid torrentId)
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

        public async Task<Torrent> GetByHash(String hash)
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

        public async Task<Torrent> Add(String realDebridId, String hash, String category, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete)
        {
            var torrent = new Torrent
            {
                TorrentId = Guid.NewGuid(),
                Added = DateTimeOffset.UtcNow,
                RdId = realDebridId,
                Hash = hash.ToLower(),
                Category = category,
                AutoDownload = autoDownload,
                AutoUnpack = autoUnpack,
                AutoDelete = autoDelete
            };

            await _dataContext.Torrents.AddAsync(torrent);

            await _dataContext.SaveChangesAsync();

            return torrent;
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

            if (torrent.Files != null)
            {
                dbTorrent.RdFiles = torrent.RdFiles;
            }

            await _dataContext.SaveChangesAsync();
        }
        
        public async Task UpdateCategory(Guid torrentId, String category)
        {
            var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

            if (dbTorrent == null)
            {
                return;
            }

            dbTorrent.Category = category;

            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateComplete(Guid torrentId, DateTimeOffset datetime)
        {
            var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);
            
            dbTorrent.Completed = datetime;

            await _dataContext.SaveChangesAsync();
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
        }
    }
}
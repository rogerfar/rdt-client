using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data
{
    public interface ITorrentData
    {
        Task<IList<Torrent>> Get();
        Task<Torrent> GetById(Guid id);
        Task<Torrent> GetByHash(String hash);
        Task<Torrent> Add(String realDebridId, String hash);
        Task UpdateRdData(Torrent torrent);
        Task UpdateStatus(Guid torrentId, TorrentStatus status);
        Task UpdateCategory(Guid torrentId, String category);
        Task Delete(Guid id);
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

            foreach (var torrent in results)
            {
                foreach (var file in torrent.Downloads)
                {
                    file.Torrent = null;
                }
            }

            return results;
        }

        public async Task<Torrent> GetById(Guid id)
        {
            var results = await _dataContext.Torrents
                                            .AsNoTracking()
                                            .Include(m => m.Downloads)
                                            .FirstOrDefaultAsync(m => m.TorrentId == id);

            foreach (var file in results.Downloads)
            {
                file.Torrent = null;
            }

            return results;
        }

        public async Task<Torrent> GetByHash(String hash)
        {
            var results = await _dataContext.Torrents
                                            .AsNoTracking()
                                            .Include(m => m.Downloads)
                                            .FirstOrDefaultAsync(m => m.Hash == hash);

            foreach (var file in results.Downloads)
            {
                file.Torrent = null;
            }

            return results;
        }

        public async Task<Torrent> Add(String realDebridId, String hash)
        {
            var torrent = new Torrent
            {
                TorrentId = Guid.NewGuid(),
                RdId = realDebridId,
                Hash = hash,
                Status = TorrentStatus.RealDebrid
            };

            _dataContext.Torrents.Add(torrent);

            await _dataContext.SaveChangesAsync();

            return torrent;
        }

        public async Task UpdateRdData(Torrent torrent)
        {
            var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrent.TorrentId);

            dbTorrent.Status = torrent.Status;

            dbTorrent.RdName = torrent.RdName;
            dbTorrent.RdSize = torrent.RdSize;
            dbTorrent.RdHost = torrent.RdHost;
            dbTorrent.RdSplit = torrent.RdSplit;
            dbTorrent.RdProgress = torrent.RdProgress;
            dbTorrent.RdStatus = torrent.RdStatus;
            dbTorrent.RdAdded = torrent.RdAdded;
            dbTorrent.RdEnded = torrent.RdEnded;
            dbTorrent.RdSpeed = torrent.RdSpeed;
            dbTorrent.RdSeeders = torrent.RdSeeders;

            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateStatus(Guid torrentId, TorrentStatus status)
        {
            var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

            dbTorrent.Status = status;

            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateCategory(Guid torrentId, String category)
        {
            var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == torrentId);

            dbTorrent.Category = category;

            await _dataContext.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            var dbTorrent = await _dataContext.Torrents.FirstOrDefaultAsync(m => m.TorrentId == id);

            _dataContext.Torrents.Remove(dbTorrent);

            await _dataContext.SaveChangesAsync();
        }
    }
}
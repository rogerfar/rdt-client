using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RDNET;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Models;

namespace RdtClient.Service.Services
{
    public interface ITorrents
    {
        Task<IList<Torrent>> Get();
        Task<Torrent> GetById(Guid id);
        Task<Torrent> GetByHash(String hash);
        Task<IList<Torrent>> Update();
        Task UpdateCategory(String hash, String category);
        Task UploadMagnet(String magnetLink);
        Task UploadFile(Byte[] bytes);
        Task Delete(Guid id);
        Task Download(Guid id);
        void Reset();
        Task<Profile> GetProfile();
    }

    public class Torrents : ITorrents
    {
        private readonly ITorrentData _torrentData;
        private readonly ISettings _settings;
        private readonly IDownloads _downloads;

        private static RdNetClient _rdtClient;

        private static DateTime _rdtLastUpdate = DateTime.UtcNow;

        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1,1);

        private RdNetClient RdNetClient
        {
            get
            {
                if (_rdtClient == null)
                {
                    var apiKey = _settings.GetString("RealDebridApiKey").Result;

                    if (String.IsNullOrWhiteSpace(apiKey))
                    {
                        throw new Exception("RealDebrid API Key not set in the settings");
                    }

                    _rdtClient = new RdNetClient("X245A4XAIBGVM", null, null, null, null, apiKey);
                }

                return _rdtClient;
            }
        }

        public Torrents(ITorrentData torrentData, ISettings settings, IDownloads downloads)
        {
            _torrentData = torrentData;
            _settings = settings;
            _downloads = downloads;
        }

        public async Task<IList<Torrent>> Get()
        {
            var torrents = await _torrentData.Get();

            if (DateTime.UtcNow > _rdtLastUpdate)
            {
                _rdtLastUpdate = DateTime.UtcNow.AddSeconds(10);

                torrents = await Update();
            }

            foreach (var torrent in torrents)
            {
                var downloads = DownloadManager.ActiveDownloads.Where(m => m.Value.TorrentId == torrent.TorrentId).ToList();

                if (torrent.Files.Count > 0)
                {
                    torrent.DownloadProgress = downloads.Sum(m => m.Value.Progress) / torrent.Files.Count;
                }
            }

            return torrents;
        }

        public async Task<Torrent> GetById(Guid id)
        {
            var torrent = await _torrentData.GetById(id);

            if (torrent != null)
            {
                var rdTorrent = await RdNetClient.TorrentInfoAsync(torrent.RdId);

                await Update(torrent, rdTorrent);
            }

            return torrent;
        }

        public async Task<Torrent> GetByHash(String hash)
        {
            var torrent = await _torrentData.GetByHash(hash);

            if (torrent != null)
            {
                var rdTorrent = await RdNetClient.TorrentInfoAsync(torrent.RdId);

                await Update(torrent, rdTorrent);
            }

            return torrent;
        }

        public async Task<IList<Torrent>> Update()
        {
            var torrents = await _torrentData.Get();

            var w = await SemaphoreSlim.WaitAsync(10);
            if (!w)
            {
                return torrents;
            }

            try
            {
                var rdTorrents = await RdNetClient.TorrentsAsync(0, 100);

                foreach (var rdTorrent in rdTorrents)
                {
                    var torrent = torrents.FirstOrDefault(m => m.RdId == rdTorrent.Id);

                    if (torrent == null)
                    {
                        var newTorrent = await _torrentData.Add(rdTorrent.Id, rdTorrent.Hash);
                        await GetById(newTorrent.TorrentId);
                    }
                    else
                    {
                        await Update(torrent, rdTorrent);
                    }
                }

                foreach (var torrent in torrents)
                {
                    var rdTorrent = rdTorrents.FirstOrDefault(m => m.Id == torrent.RdId);

                    if (rdTorrent == null)
                    {
                        await _torrentData.Delete(torrent.TorrentId);
                    }
                }

                return torrents;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task UpdateCategory(String hash, String category)
        {
            var torrent = await _torrentData.GetByHash(hash);

            if (torrent == null)
            {
                return;
            }

            await _torrentData.UpdateCategory(torrent.TorrentId, category);
        }

        public async Task UploadMagnet(String magnetLink)
        {
            var magnet = MonoTorrent.MagnetLink.Parse(magnetLink);

            var rdTorrent = await RdNetClient.TorrentAddMagnet(magnetLink);

            await Add(rdTorrent.Id, magnet.InfoHash.ToHex());
        }

        public async Task UploadFile(Byte[] bytes)
        {
            var torrent = MonoTorrent.Torrent.Load(bytes);

            var rdTorrent = await RdNetClient.TorrentAddFile(bytes);

            await Add(rdTorrent.Id, torrent.InfoHash.ToHex());
        }

        public async Task Delete(Guid id)
        {
            var torrent = await GetById(id);

            if (torrent != null)
            {
                await _torrentData.Delete(id);
                await RdNetClient.TorrentDelete(torrent.RdId);
            }
        }

        public async Task Download(Guid id)
        {
            var torrent = await _torrentData.GetById(id);

            await _downloads.DeleteForTorrent(id);

            var rdTorrent = await RdNetClient.TorrentInfoAsync(torrent.RdId);

            foreach (var file in rdTorrent.Files)
            {
                var unrestrictedLink = await RdNetClient.UnrestrictLinkAsync(file.Path);

                await _downloads.Add(torrent.TorrentId, unrestrictedLink.Download);
            }
        }

        public void Reset()
        {
            _rdtClient = null;
            _rdtLastUpdate = DateTime.UtcNow;
        }

        public async Task<Profile> GetProfile()
        {
            var user = await _rdtClient.UserAsync();

            var profile = new Profile
            {
                UserName = user.Username,
                Expiration = user.Expiration
            };

            return profile;
        }

        private async Task Add(String rdTorrentId, String infoHash)
        {
            var newTorrent = await _torrentData.Add(rdTorrentId, infoHash);

            var rdTorrent = await RdNetClient.TorrentInfoAsync(rdTorrentId);

            if (rdTorrent.Files != null && rdTorrent.Files.Count > 0)
            {
                if (!rdTorrent.Files.Any(m => m.Selected))
                {
                    var fileIds = rdTorrent.Files.Select(m => m.Id.ToString()).ToArray();

                    await RdNetClient.TorrentSelectFiles(rdTorrentId, fileIds);
                }
            }

            await Update(newTorrent, rdTorrent);
        }

        private async Task Update(Torrent torrent, RDNET.Models.Torrent rdTorrent)
        {
            if (!String.IsNullOrWhiteSpace(rdTorrent.Filename))
            {
                torrent.RdName = rdTorrent.Filename;
            }
            else if (!String.IsNullOrWhiteSpace(rdTorrent.OriginalFilename))
            {
                torrent.RdName = rdTorrent.OriginalFilename;
            }

            if (rdTorrent.Bytes > 0)
            {
                torrent.RdSize = rdTorrent.Bytes;
            }
            else if (rdTorrent.OriginalBytes > 0)
            {
                torrent.RdSize = rdTorrent.OriginalBytes;
            }

            if (rdTorrent.Files != null)
            {
                torrent.RdFiles = JsonConvert.SerializeObject(rdTorrent.Files);
            }

            torrent.RdHost = rdTorrent.Host;
            torrent.RdSplit = rdTorrent.Split;
            torrent.RdProgress = rdTorrent.Progress;
            torrent.RdStatus = rdTorrent.Status;
            torrent.RdAdded = rdTorrent.Added;
            torrent.RdEnded = rdTorrent.Ended;
            torrent.RdSpeed = rdTorrent.Speed;
            torrent.RdSeeders = rdTorrent.Seeders;

            await _torrentData.UpdateRdData(torrent);

            if (torrent.Status == TorrentStatus.RealDebrid && torrent.RdProgress == 100)
            {
                await _torrentData.UpdateStatus(torrent.TorrentId, TorrentStatus.WaitingForDownload);
            }
        }
    }
}

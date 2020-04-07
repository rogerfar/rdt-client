using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoTorrent;
using Newtonsoft.Json;
using RDNET;
using RdtClient.Data.Enums;
using RdtClient.Service.Models;
using ITorrentData = RdtClient.Data.Data.ITorrentData;
using Torrent = RdtClient.Data.Models.Data.Torrent;

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
        private static RdNetClient _rdtClient;

        private static DateTime _rdtLastUpdate = DateTime.UtcNow;

        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly IDownloads _downloads;
        private readonly ISettings _settings;
        private readonly ITorrentData _torrentData;

        public Torrents(ITorrentData torrentData, ISettings settings, IDownloads downloads)
        {
            _torrentData = torrentData;
            _settings = settings;
            _downloads = downloads;
        }

        private RdNetClient RdNetClient
        {
            get
            {
                if (_rdtClient == null)
                {
                    var apiKey = _settings.GetString("RealDebridApiKey")
                                          .Result;

                    if (String.IsNullOrWhiteSpace(apiKey))
                    {
                        throw new Exception("RealDebrid API Key not set in the settings");
                    }

                    _rdtClient = new RdNetClient("X245A4XAIBGVM", null, null, null, null, apiKey);
                }

                return _rdtClient;
            }
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
                foreach (var download in torrent.Downloads)
                {
                    if (DownloadManager.ActiveDownloads.TryGetValue(download.DownloadId, out var activeDownload))
                    {
                        download.Speed = activeDownload.Speed;
                        download.Progress = activeDownload.Progress;
                    }
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
                        await _downloads.DeleteForTorrent(torrent.TorrentId);
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
            var magnet = MagnetLink.Parse(magnetLink);

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
                await _downloads.DeleteForTorrent(torrent.TorrentId);
                await _torrentData.Delete(id);
                await RdNetClient.TorrentDelete(torrent.RdId);
            }
        }

        public async Task Download(Guid id)
        {
            var torrent = await _torrentData.GetById(id);

            await _downloads.DeleteForTorrent(id);

            var rdTorrent = await RdNetClient.TorrentInfoAsync(torrent.RdId);

            foreach (var link in rdTorrent.Links)
            {
                var unrestrictedLink = await RdNetClient.UnrestrictLinkAsync(link);

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
                    var fileIds = rdTorrent.Files.Select(m => m.Id.ToString())
                                           .ToArray();

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

            if (torrent.Status == TorrentStatus.RealDebrid)
            {
                if (torrent.Status == TorrentStatus.RealDebrid && torrent.RdProgress == 100)
                {
                    torrent.Status = TorrentStatus.WaitingForDownload;
                }
                else
                {
                    // Current status of the torrent: magnet_error, magnet_conversion, waiting_files_selection, queued, downloading, downloaded, error, virus, compressing, uploading, dead
                    torrent.Status = rdTorrent.Status switch
                    {
                        "magnet_error" => TorrentStatus.Error,
                        "error" => TorrentStatus.Error,
                        "virus" => TorrentStatus.Error,
                        "dead" => TorrentStatus.Error,
                        _ => TorrentStatus.RealDebrid
                    };
                }
            }

            await _torrentData.UpdateRdData(torrent);
        }
    }
}
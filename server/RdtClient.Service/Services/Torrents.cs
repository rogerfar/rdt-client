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
        Task UpdateStatus(Guid torrentId, TorrentStatus status);
        Task UpdateCategory(String hash, String category);
        Task UploadMagnet(String magnetLink, Boolean autoDownload, Boolean autoDelete);
        Task UploadFile(Byte[] bytes, Boolean autoDownload, Boolean autoDelete);
        Task Delete(Guid id);
        Task Download(Guid id);
        void Reset();
        Task<Profile> GetProfile();
    }

    public class Torrents : ITorrents
    {
        private static RdNetClient _rdtClient;

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

                    _rdtClient = new RdNetClient("X245A4XAIBGVM", null, null, null, apiKey);
                }

                return _rdtClient;
            }
        }

        public async Task<IList<Torrent>> Get()
        {
            var torrents = await _torrentData.Get();
            
            foreach (var torrent in torrents)
            {
                foreach (var download in torrent.Downloads)
                {
                    if (TaskRunner.ActiveDownloads.TryGetValue(download.DownloadId, out var activeDownload))
                    {
                        download.Speed = activeDownload.Speed;
                        download.BytesSize = activeDownload.BytesSize;
                        download.BytesDownloaded = activeDownload.BytesDownloaded;

                        if (activeDownload.NewStatus.HasValue)
                        {
                            download.Status = activeDownload.NewStatus.Value;
                        }
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
                var rdTorrent = await RdNetClient.GetTorrentInfoAsync(torrent.RdId);

                await Update(torrent, rdTorrent);
            }

            return torrent;
        }

        public async Task<Torrent> GetByHash(String hash)
        {
            var torrent = await _torrentData.GetByHash(hash);

            if (torrent != null)
            {
                var rdTorrent = await RdNetClient.GetTorrentInfoAsync(torrent.RdId);

                await Update(torrent, rdTorrent);
            }

            return torrent;
        }

        public async Task<IList<Torrent>> Update()
        {
            var torrents = await _torrentData.Get();

            var w = await SemaphoreSlim.WaitAsync(1);
            if (!w)
            {
                return torrents;
            }

            try
            {
                var rdTorrents = await RdNetClient.GetTorrentsAsync(0, 100);

                foreach (var rdTorrent in rdTorrents)
                {
                    var torrent = torrents.FirstOrDefault(m => m.RdId == rdTorrent.Id);

                    if (torrent == null)
                    {
                        var newTorrent = await _torrentData.Add(rdTorrent.Id, rdTorrent.Hash, false, false);
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

        public async Task UpdateStatus(Guid torrentId, TorrentStatus status)
        {
            await _torrentData.UpdateStatus(torrentId, status);
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

        public async Task UploadMagnet(String magnetLink, Boolean autoDownload, Boolean autoDelete)
        {
            var magnet = MagnetLink.Parse(magnetLink);

            var rdTorrent = await RdNetClient.AddTorrentMagnetAsync(magnetLink);

            await Add(rdTorrent.Id, magnet.InfoHash.ToHex(), autoDownload, autoDelete);
        }

        public async Task UploadFile(Byte[] bytes, Boolean autoDownload, Boolean autoDelete)
        {
            var torrent = await MonoTorrent.Torrent.LoadAsync(bytes);

            var rdTorrent = await RdNetClient.AddTorrentFileAsync(bytes);

            await Add(rdTorrent.Id, torrent.InfoHash.ToHex(), autoDownload, autoDelete);
        }

        public async Task Delete(Guid id)
        {
            var torrent = await GetById(id);

            if (torrent != null)
            {
                await _downloads.DeleteForTorrent(torrent.TorrentId);
                await _torrentData.Delete(id);
                await RdNetClient.DeleteTorrentAsync(torrent.RdId);
            }
        }

        public async Task Download(Guid id)
        {
            var torrent = await _torrentData.GetById(id);

            await _downloads.DeleteForTorrent(id);

            await _torrentData.UpdateStatus(id, TorrentStatus.DownloadQueued);

            var rdTorrent = await RdNetClient.GetTorrentInfoAsync(torrent.RdId);

            var existingDownloads = await _downloads.GetForTorrent(id);

            foreach (var link in rdTorrent.Links)
            {
                var unrestrictedLink = await RdNetClient.UnrestrictLinkAsync(link);

                if (existingDownloads.Any(m => m.Link == unrestrictedLink.Download))
                {
                    continue;
                }

                await _downloads.Add(torrent.TorrentId, unrestrictedLink.Download);
            }
        }

        public void Reset()
        {
            _rdtClient = null;
        }

        public async Task<Profile> GetProfile()
        {
            if (RdNetClient == null)
            {
                return new Profile();
            }

            var user = await RdNetClient.GetUserAsync();

            var profile = new Profile
            {
                UserName = user.Username,
                Expiration = user.Expiration
            };

            return profile;
        }

        private async Task Add(String rdTorrentId, String infoHash, Boolean autoDownload, Boolean autoDelete)
        {
            var w = await SemaphoreSlim.WaitAsync(1);
            if (!w)
            {
                return;
            }

            try
            {
                var torrent = await _torrentData.GetByHash(infoHash);

                if (torrent != null)
                {
                    return;
                }

                var newTorrent = await _torrentData.Add(rdTorrentId, infoHash, autoDownload, autoDelete);

                var rdTorrent = await RdNetClient.GetTorrentInfoAsync(rdTorrentId);

                await Update(newTorrent, rdTorrent);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        private async Task Update(Torrent torrent, RDNET.Torrent rdTorrent)
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

            if (torrent.Status == TorrentStatus.RealDebrid)
            {
                if (torrent.Status == TorrentStatus.RealDebrid && torrent.RdProgress == 100)
                {
                    await _torrentData.UpdateStatus(torrent.TorrentId, TorrentStatus.WaitingForDownload);
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

                    await _torrentData.UpdateStatus(torrent.TorrentId, torrent.Status);
                }
            }

            if (rdTorrent.Files != null && rdTorrent.Files.Count > 0)
            {
                if (!rdTorrent.Files.Any(m => m.Selected))
                {
                    var fileIds = rdTorrent.Files
                                           .Where(m => m.Bytes > 1024 * 10)
                                           .Select(m => m.Id.ToString())
                                           .ToArray();

                    await RdNetClient.SelectTorrentFilesAsync(rdTorrent.Id, fileIds);
                }
            }
        }
    }
}
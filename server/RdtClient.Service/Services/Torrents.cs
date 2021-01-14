using System;
using System.Collections.Generic;
using System.IO;
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
        Task<Torrent> GetById(Guid torrentId);
        Task<Torrent> GetByHash(String hash);
        Task UpdateCategory(String hash, String category);
        Task UploadMagnet(String magnetLink, String category, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete);
        Task UploadFile(Byte[] bytes, String category, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete);
        Task SelectFiles(String torrentId, IList<String> fileIds);
        Task Delete(Guid torrentId, Boolean deleteData, Boolean deleteRdTorrent, Boolean deleteLocalFiles);
        Task Unrestrict(Guid torrentId);
        Task Download(Guid downloadId);
        Task Unpack(Guid downloadId);
        void Reset();
        Task<Profile> GetProfile();
        Task UpdateComplete(Guid torrentId, DateTimeOffset datetime);
        Task Update();
    }

    public class Torrents : ITorrents
    {
        private static RdNetClient _rdtNetClient;

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

        private RdNetClient GetRdNetClient()
        {
            if (_rdtNetClient == null)
            {
                var apiKey = _settings.GetString("RealDebridApiKey")
                                      .Result;

                if (String.IsNullOrWhiteSpace(apiKey))
                {
                    throw new Exception("RealDebrid API Key not set in the settings");
                }

                _rdtNetClient = new RdNetClient("X245A4XAIBGVM", null, null, null, apiKey);
            }

            return _rdtNetClient;
        }

        public async Task<IList<Torrent>> Get()
        {
            var torrents = await _torrentData.Get();
            
            foreach (var torrent in torrents)
            {
                foreach (var download in torrent.Downloads)
                {
                    if (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
                    {
                        download.Speed = downloadClient.Speed;
                        download.BytesTotal = downloadClient.BytesTotal;
                        download.BytesDone = downloadClient.BytesDone;
                    }
                    
                    if (TorrentRunner.ActiveUnpackClients.TryGetValue(download.DownloadId, out var unpackClient))
                    {
                        download.BytesTotal = unpackClient.BytesTotal;
                        download.BytesDone = unpackClient.BytesDone;
                    }
                }
            }

            return torrents;
        }

        public async Task<Torrent> GetById(Guid torrentId)
        {
            var torrent = await _torrentData.GetById(torrentId);

            if (torrent != null)
            {
                var rdTorrent = await GetRdNetClient().GetTorrentInfoAsync(torrent.RdId);

                await Update(torrent, rdTorrent);
            }

            return torrent;
        }

        public async Task<Torrent> GetByHash(String hash)
        {
            var torrent = await _torrentData.GetByHash(hash);

            if (torrent != null)
            {
                var rdTorrent = await GetRdNetClient().GetTorrentInfoAsync(torrent.RdId);

                await Update(torrent, rdTorrent);
            }

            return torrent;
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

        public async Task UploadMagnet(String magnetLink, String category, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete)
        {
            var magnet = MagnetLink.Parse(magnetLink);

            var rdTorrent = await GetRdNetClient().AddTorrentMagnetAsync(magnetLink);

            await Add(rdTorrent.Id, magnet.InfoHash.ToHex(), category, autoDownload, autoUnpack, autoDelete);
        }

        public async Task UploadFile(Byte[] bytes, String category, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete)
        {
            var torrent = await MonoTorrent.Torrent.LoadAsync(bytes);

            var rdTorrent = await GetRdNetClient().AddTorrentFileAsync(bytes);

            await Add(rdTorrent.Id, torrent.InfoHash.ToHex(), category, autoDownload, autoUnpack, autoDelete);
        }

        public async Task SelectFiles(String torrentId, IList<String> fileIds)
        {
            await GetRdNetClient().SelectTorrentFilesAsync(torrentId, fileIds.ToArray());
        }

        public async Task Delete(Guid torrentId, Boolean deleteData, Boolean deleteRdTorrent, Boolean deleteLocalFiles)
        {
            var torrent = await GetById(torrentId);

            if (torrent != null)
            {
                foreach (var download in torrent.Downloads)
                {
                    if (TorrentRunner.ActiveUnpackClients.TryRemove(download.DownloadId, out var activeUnpack))
                    {
                        activeUnpack.Cancel();
                    }

                    if (TorrentRunner.ActiveDownloadClients.TryRemove(download.DownloadId, out var activeDownload))
                    {
                        activeDownload.Cancel();
                    }
                }

                if (deleteLocalFiles)
                {
                    var downloadPath = await DownloadPath(torrent);
                    downloadPath = Path.Combine(downloadPath, torrent.RdName);
                    
                    if (Directory.Exists(downloadPath))
                    {
                        Directory.Delete(downloadPath, true);
                    }
                }
                
                if (deleteData)
                {
                    await _downloads.DeleteForTorrent(torrent.TorrentId);
                    await _torrentData.Delete(torrentId);
                }

                if (deleteRdTorrent)
                {
                    await GetRdNetClient().DeleteTorrentAsync(torrent.RdId);
                }
            }
        }

        public async Task Unrestrict(Guid torrentId)
        {
            var torrent = await _torrentData.GetById(torrentId);

            var rdTorrent = await GetRdNetClient().GetTorrentInfoAsync(torrent.RdId);

            foreach (var link in rdTorrent.Links)
            {
                var unrestrictedLink = await GetRdNetClient().UnrestrictLinkAsync(link);

                if (torrent.Downloads.Any(m => m.Link == unrestrictedLink.Download))
                {
                    continue;
                }

                await _downloads.Add(torrent.TorrentId, unrestrictedLink.Download);
            }
        }

        public async Task Download(Guid downloadId)
        {
            var settingDownloadLimit = await _settings.GetNumber("DownloadLimit");
            
            var download = await _downloads.GetById(downloadId);
            
            var downloadPath = await DownloadPath(download.Torrent);
            
            download.DownloadFinished = null;
            await _downloads.UpdateDownloadFinished(download.DownloadId, null);

            download.Completed = null;
            await _downloads.UpdateCompleted(download.DownloadId, null);
            
            download.Error = null;
            await _downloads.UpdateError(download.DownloadId, null);
            
            // Check if we have reached the download limit, if so queue the download, but don't start it.
            if (TorrentRunner.ActiveDownloadClients.Count >= settingDownloadLimit)
            {
                return;
            }

            if (TorrentRunner.ActiveDownloadClients.ContainsKey(downloadId))
            {
                return;
            }
            
            download.DownloadStarted = DateTimeOffset.UtcNow;
            await _downloads.UpdateDownloadStarted(download.DownloadId, download.DownloadStarted);
            
            // Start the download process
            var downloadClient = new DownloadClient(download, downloadPath);

            if (TorrentRunner.ActiveDownloadClients.TryAdd(downloadId, downloadClient))
            {
                await downloadClient.Start();
            }
        }

        public async Task Unpack(Guid downloadId)
        {
            var settingUnpackLimit = await _settings.GetNumber("UnpackLimit");
            
            var download = await _downloads.GetById(downloadId);
            
            var downloadPath = await DownloadPath(download.Torrent);
            
            // Check if the file is even unpackable.
            var uri = new Uri(download.Link);
            var fileName = uri.Segments.Last();
            var extension = Path.GetExtension(fileName);

            if (extension != ".rar")
            {
                await _downloads.UpdateUnpackingQueued(downloadId, DateTimeOffset.UtcNow);
                await _downloads.UpdateUnpackingStarted(downloadId, DateTimeOffset.UtcNow);
                await _downloads.UpdateUnpackingFinished(downloadId, DateTimeOffset.UtcNow);
                
                return;
            }
            
            // This property can have a value when it is a retry.
            if (download.UnpackingQueued == null)
            {
                download.UnpackingQueued = DateTimeOffset.UtcNow;
                await _downloads.UpdateUnpackingQueued(download.DownloadId, download.UnpackingQueued);
            }

            // Check if we have reached the download limit, if so queue the download, but don't start it.
            if (TorrentRunner.ActiveUnpackClients.Count >= settingUnpackLimit)
            {
                return;
            }
            
            if (TorrentRunner.ActiveUnpackClients.ContainsKey(downloadId))
            {
                return;
            }
            
            download.UnpackingStarted = DateTimeOffset.UtcNow;
            await _downloads.UpdateUnpackingStarted(download.DownloadId, download.UnpackingStarted);
            
            // Start the unpacking process
            var unpackClient = new UnpackClient(download, downloadPath);

            if (TorrentRunner.ActiveUnpackClients.TryAdd(downloadId, unpackClient))
            {
                await unpackClient.Start();
            }
        }

        public void Reset()
        {
            _rdtNetClient = null;
        }

        public async Task<Profile> GetProfile()
        {
            if (_rdtNetClient == null)
            {
                return new Profile();
            }

            var user = await GetRdNetClient().GetUserAsync();

            var profile = new Profile
            {
                UserName = user.Username,
                Expiration = user.Expiration
            };

            return profile;
        }

        private async Task Add(String rdTorrentId, String infoHash, String category, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete)
        {
            var w = await SemaphoreSlim.WaitAsync(60000);
            if (!w)
            {
                throw new Exception("Unable to add torrent, could not obtain lock");
            }

            try
            {
                var torrent = await _torrentData.GetByHash(infoHash);

                if (torrent != null)
                {
                    return;
                }

                var newTorrent = await _torrentData.Add(rdTorrentId, infoHash, category, autoDownload, autoUnpack, autoDelete);

                var rdTorrent = await GetRdNetClient().GetTorrentInfoAsync(rdTorrentId);

                await Update(newTorrent, rdTorrent);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task Update()
        {
            var w = await SemaphoreSlim.WaitAsync(1);

            if (!w)
            {
                

                return;
            }

            var torrents = await Get();

            try
            {
                var rdTorrents = await GetRdNetClient().GetTorrentsAsync(0, 100);

                foreach (var rdTorrent in rdTorrents)
                {
                    var torrent = torrents.FirstOrDefault(m => m.RdId == rdTorrent.Id);

                    if (torrent == null)
                    {
                        continue;
                    }
                    if (rdTorrent.Files == null)
                    {
                        var rdTorrentInfo = await GetRdNetClient().GetTorrentInfoAsync(rdTorrent.Id);
                        await Update(torrent, rdTorrentInfo);
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
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task UpdateComplete(Guid torrentId, DateTimeOffset datetime)
        {
            await _torrentData.UpdateComplete(torrentId, datetime);
        }

        private async Task<String> DownloadPath(Torrent torrent)
        {
            var settingDownloadPath = await _settings.GetString("DownloadPath");
            
            if (!String.IsNullOrWhiteSpace(torrent.Category))
            {
                settingDownloadPath = Path.Combine(settingDownloadPath, torrent.Category);
            }

            return settingDownloadPath;
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
            torrent.RdAdded = rdTorrent.Added;
            torrent.RdEnded = rdTorrent.Ended;
            torrent.RdSpeed = rdTorrent.Speed;
            torrent.RdSeeders = rdTorrent.Seeders;
            torrent.RdStatusRaw = rdTorrent.Status;

            torrent.RdStatus = rdTorrent.Status switch
            {
                "magnet_error" => RealDebridStatus.Error,
                "magnet_conversion" => RealDebridStatus.Processing,
                "waiting_files_selection" => RealDebridStatus.WaitingForFileSelection,
                "queued" => RealDebridStatus.Downloading,
                "downloading" => RealDebridStatus.Downloading,
                "downloaded" => RealDebridStatus.Finished,
                "error" => RealDebridStatus.Error,
                "virus" => RealDebridStatus.Error,
                "compressing" => RealDebridStatus.Downloading,
                "uploading" => RealDebridStatus.Finished,
                "dead" => RealDebridStatus.Error,
                _ => RealDebridStatus.Error
            };

            await _torrentData.UpdateRdData(torrent);
        }
    }
}
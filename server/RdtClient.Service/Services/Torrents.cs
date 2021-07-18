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
        Task<Torrent> GetByHash(String hash);
        Task UpdateCategory(String hash, String category);
        Task UploadMagnet(String magnetLink, String category, Boolean autoDelete);
        Task UploadFile(Byte[] bytes, String category, Boolean autoDelete);
        Task<List<String>> GetAvailableFiles(String hash);
        Task SelectFiles(Guid torrentId, IList<String> fileIds);
        Task Delete(Guid torrentId, Boolean deleteData, Boolean deleteRdTorrent, Boolean deleteLocalFiles);
        Task<String> UnrestrictLink(Guid downloadId);
        Task Download(Guid downloadId);
        Task Unpack(Guid downloadId);
        void Reset();
        Task<Profile> GetProfile();
        Task UpdateComplete(Guid torrentId, DateTimeOffset datetime);
        Task Update();
        Task Retry(Guid id, Int32 retry);
    }

    public class Torrents : ITorrents
    {
        private static RdNetClient _rdtNetClient;

        private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);
        
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
                await Update(torrent);
            }

            return torrent;
        }

        public async Task<Torrent> GetByHash(String hash)
        {
            var torrent = await _torrentData.GetByHash(hash);

            if (torrent != null)
            {
                await Update(torrent);
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

        public async Task UploadMagnet(String magnetLink, String category, Boolean autoDelete)
        {
            MagnetLink magnet;

            try
            {
                magnet = MagnetLink.Parse(magnetLink);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}, trying to parse {magnetLink}");
            }

            var rdTorrent = await GetRdNetClient().AddTorrentMagnetAsync(magnetLink);

            await Add(rdTorrent.Id, magnet.InfoHash.ToHex(), category, autoDelete, magnetLink, false);
        }

        public async Task UploadFile(Byte[] bytes, String category, Boolean autoDelete)
        {
            MonoTorrent.Torrent torrent;

            var fileAsBase64 = Convert.ToBase64String(bytes);

            try
            {
                torrent = await MonoTorrent.Torrent.LoadAsync(bytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}, trying to parse {fileAsBase64}");
            }

            var rdTorrent = await GetRdNetClient().AddTorrentFileAsync(bytes);
            
            await Add(rdTorrent.Id, torrent.InfoHash.ToHex(), category, autoDelete, fileAsBase64, true);
        }

        public async Task<List<String>> GetAvailableFiles(String hash)
        {
            var result = await GetRdNetClient().GetAvailableFiles(hash);

            var files = result.SelectMany(m => m.Value).SelectMany(m => m.Value).SelectMany(m => m.Values);

            var groups = files.GroupBy(m => m.Filename);

            return groups.Select(m => m.Key).ToList();
        }

        public async Task SelectFiles(Guid torrentId, IList<String> fileIds)
        {
            var torrent = await GetById(torrentId);

            RDNET.Torrent rdTorrent = null;

            for (var i = 0; i < 5; i++)
            {
                await GetRdNetClient().SelectTorrentFilesAsync(torrent.RdId, fileIds.ToArray());

                await Task.Delay(5000);

                rdTorrent = await GetRdNetClient().GetTorrentInfoAsync(torrent.RdId);

                if (fileIds.Count == rdTorrent.Links.Count)
                {
                    break;
                }

                await Task.Delay(10000);
            }

            if (rdTorrent == null)
            {
                return;
            }

            foreach (var file in rdTorrent.Links)
            {
                // Make sure downloads don't get added multiple times
                var downloadExists = await _downloads.Get(torrent.TorrentId, file);

                if (downloadExists == null && !String.IsNullOrWhiteSpace(file))
                {
                    await _downloads.Add(torrent.TorrentId, file);
                }
            }
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

                if (deleteData)
                {
                    await _torrentData.UpdateComplete(torrent.TorrentId, DateTimeOffset.UtcNow);
                    await _downloads.DeleteForTorrent(torrent.TorrentId);
                    await _torrentData.Delete(torrentId);
                }

                if (deleteRdTorrent)
                {
                    await GetRdNetClient().DeleteTorrentAsync(torrent.RdId);
                }

                if (deleteLocalFiles)
                {
                    var downloadPath = await DownloadPath(torrent);
                    downloadPath = Path.Combine(downloadPath, torrent.RdName);
                    
                    if (Directory.Exists(downloadPath))
                    {
                        var retry = 0;

                        while (true)
                        {
                            try
                            {
                                Directory.Delete(downloadPath, true);

                                break;
                            }
                            catch
                            {
                                retry++;
                                if (retry >= 3)
                                {
                                    throw;
                                }

                                await Task.Delay(1000);
                            }
                        }
                    }
                }
            }
        }

        public async Task<String> UnrestrictLink(Guid downloadId)
        {
            var download = await _downloads.GetById(downloadId);

            var unrestrictedLink = await GetRdNetClient().UnrestrictLinkAsync(download.Path);

            await _downloads.UpdateUnrestrictedLink(downloadId, unrestrictedLink.Download);

            return unrestrictedLink.Download;
        }

        public async Task Download(Guid downloadId)
        {
            await _downloads.UpdateDownloadStarted(downloadId, null);
            await _downloads.UpdateDownloadFinished(downloadId, null);
            await _downloads.UpdateUnpackingQueued(downloadId, null);
            await _downloads.UpdateUnpackingStarted(downloadId, null);
            await _downloads.UpdateUnpackingFinished(downloadId, null);
            await _downloads.UpdateCompleted(downloadId, null);
            await _downloads.UpdateError(downloadId, null);
        }

        public async Task Unpack(Guid downloadId)
        {
            await _downloads.UpdateUnpackingQueued(downloadId, DateTimeOffset.UtcNow);
            await _downloads.UpdateUnpackingStarted(downloadId, null);
            await _downloads.UpdateUnpackingFinished(downloadId, null);
            await _downloads.UpdateCompleted(downloadId, null);
            await _downloads.UpdateError(downloadId, null);
        }

        public void Reset()
        {
            _rdtNetClient = null;
        }

        public async Task<Profile> GetProfile()
        {
            var user = await GetRdNetClient().GetUserAsync();

            var profile = new Profile
            {
                UserName = user.Username,
                Expiration = user.Expiration
            };

            return profile;
        }

        private async Task Add(String rdTorrentId, String infoHash, String category, Boolean autoDelete, String fileOrMagnetContents, Boolean isFile)
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

                var newTorrent = await _torrentData.Add(rdTorrentId, infoHash, category, autoDelete, fileOrMagnetContents, isFile);

                await Update(newTorrent);
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
                    
                    await Update(torrent);
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

        public async Task Retry(Guid id, Int32 retry)
        {
            var torrent = await _torrentData.GetById(id);

            if (retry == 0)
            {
                await Delete(id, true, true, true);

                if (String.IsNullOrWhiteSpace(torrent.FileOrMagnet))
                {
                    throw new Exception($"Cannot re-add this torrent, original magnet or file not found");
                }

                if (torrent.IsFile)
                {
                    var bytes = Convert.FromBase64String(torrent.FileOrMagnet);

                    await UploadFile(bytes, torrent.Category, torrent.AutoDelete);
                }
                else
                {
                    await UploadMagnet(torrent.FileOrMagnet, torrent.Category, torrent.AutoDelete);
                }
            }
            else if (retry == 1)
            {
                await Delete(id, false, false, true);

                await _torrentData.UpdateComplete(id, null);
                await _downloads.DeleteForTorrent(id);
            }
            else
            {
                throw new Exception($"Invalid retry option {retry}");
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

        private async Task Update(Torrent torrent)
        {
            var originalTorrent = JsonConvert.SerializeObject(torrent, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            var rdTorrent = await GetRdNetClient().GetTorrentInfoAsync(torrent.RdId);

            if (!String.IsNullOrWhiteSpace(rdTorrent.Filename))
            {
                torrent.RdName = rdTorrent.Filename;
            }

            if (!String.IsNullOrWhiteSpace(rdTorrent.OriginalFilename))
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

            var newTorrent = JsonConvert.SerializeObject(torrent, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            if (originalTorrent != newTorrent)
            {
                await _torrentData.UpdateRdData(torrent);
            }
        }
    }
}
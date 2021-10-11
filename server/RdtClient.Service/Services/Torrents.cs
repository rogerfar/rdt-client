using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MonoTorrent;
using Newtonsoft.Json;
using RDNET;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Service.Helpers;
using RdtClient.Service.Models;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services
{
    public class Torrents
    {
        public static readonly SemaphoreSlim TorrentResetLock = new(1, 1);

        private static readonly SemaphoreSlim RealDebridUpdateLock = new(1, 1);

        private readonly Downloads _downloads;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TorrentData _torrentData;

        public Torrents(IHttpClientFactory httpClientFactory, TorrentData torrentData, Downloads downloads)
        {
            _httpClientFactory = httpClientFactory;
            _torrentData = torrentData;
            _downloads = downloads;
        }

        private RdNetClient GetRdNetClient()
        {
            var apiKey = Settings.Get.RealDebridApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("Real-Debrid API Key not set in the settings");
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var rdtNetClient = new RdNetClient(null, httpClient);
            rdtNetClient.UseApiAuthentication(apiKey);

            return rdtNetClient;
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

        public async Task UploadMagnet(String magnetLink,
                                       String category,
                                       TorrentDownloadAction downloadAction,
                                       TorrentFinishedAction finishedAction,
                                       Int32 downloadMinSize,
                                       String downloadManualFiles)
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

            var rdTorrent = await GetRdNetClient().Torrents.AddMagnetAsync(magnetLink);

            await Add(rdTorrent.Id, magnet.InfoHash.ToHex(), category, downloadAction, finishedAction, downloadMinSize, downloadManualFiles, magnetLink, false);
        }

        public async Task UploadFile(Byte[] bytes,
                                     String category,
                                     TorrentDownloadAction downloadAction,
                                     TorrentFinishedAction finishedAction,
                                     Int32 downloadMinSize,
                                     String downloadManualFiles)
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

            var rdTorrent = await GetRdNetClient().Torrents.AddFileAsync(bytes);

            await Add(rdTorrent.Id, torrent.InfoHash.ToHex(), category, downloadAction, finishedAction, downloadMinSize, downloadManualFiles, fileAsBase64, true);
        }

        public async Task<List<TorrentInstantAvailabilityFile>> GetAvailableFiles(String hash)
        {
            var result = await GetRdNetClient().Torrents.GetAvailableFiles(hash);

            var files = result.SelectMany(m => m.Value).SelectMany(m => m.Value).SelectMany(m => m.Values);

            var groups = files.GroupBy(m => $"{m.Filename}-{m.Filesize}");

            return groups.Select(m => m.First()).ToList();
        }

        public async Task SelectFiles(Guid torrentId, IList<String> fileIds)
        {
            var torrent = await GetById(torrentId);

            await GetRdNetClient().Torrents.SelectFilesAsync(torrent.RdId, fileIds.ToArray());
        }

        public async Task CheckForLinks(Guid torrentId)
        {
            var torrent = await GetById(torrentId);

            var rdTorrent = await GetRdNetClient().Torrents.GetInfoAsync(torrent.RdId);

            // Sometimes RD will give you 1 rar with all files, sometimes it will give you 1 link per file.
            if (torrent.Files.Count(m => m.Selected) != rdTorrent.Links.Count && 
                torrent.ManualFiles.Count != rdTorrent.Links.Count &&
                rdTorrent.Links.Count != 1)
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
                    await GetRdNetClient().Torrents.DeleteAsync(torrent.RdId);
                }

                if (deleteLocalFiles)
                {
                    var downloadPath = DownloadPath(torrent);
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

        private async Task DeleteDownload(Guid torrentId, Guid downloadId)
        {
            var torrent = await GetById(torrentId);
            var download = await _downloads.GetById(downloadId);
            
            var downloadPath = DownloadPath(torrent);
            
            var filePath = DownloadHelper.GetDownloadPath(downloadPath, torrent, download);

            if (filePath == null)
            {
                return;
            }

            if (File.Exists(filePath))
            {
                var retry = 0;

                while (true)
                {
                    try
                    {
                        File.Delete(filePath);

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

        public async Task<String> UnrestrictLink(Guid downloadId)
        {
            var download = await _downloads.GetById(downloadId);

            var unrestrictedLink = await GetRdNetClient().Unrestrict.LinkAsync(download.Path);

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

        public async Task<Profile> GetProfile()
        {
            var user = await GetRdNetClient().User.GetAsync();

            var profile = new Profile
            {
                UserName = user.Username,
                Expiration = user.Expiration
            };

            return profile;
        }

        public async Task Update()
        {
            await RealDebridUpdateLock.WaitAsync();

            var torrents = await Get();

            try
            {
                var rdTorrents = await GetRdNetClient().Torrents.GetAsync(0, 100);

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
                RealDebridUpdateLock.Release();
            }
        }

        public async Task RetryTorrent(Guid torrentId)
        {
            var torrent = await _torrentData.GetById(torrentId);

            foreach (var download in torrent.Downloads)
            {
                while (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
                {
                    downloadClient.Cancel();

                    await Task.Delay(1000);
                }

                while (TorrentRunner.ActiveUnpackClients.TryGetValue(download.DownloadId, out var unpackClient))
                {
                    unpackClient.Cancel();

                    await Task.Delay(1000);
                }
            }

            await Delete(torrentId, true, true, true);

            if (String.IsNullOrWhiteSpace(torrent.FileOrMagnet))
            {
                throw new Exception($"Cannot re-add this torrent, original magnet or file not found");
            }

            await TorrentResetLock.WaitAsync();

            try
            {
                if (torrent.IsFile)
                {
                    var bytes = Convert.FromBase64String(torrent.FileOrMagnet);

                    await UploadFile(bytes, torrent.Category, torrent.DownloadAction, torrent.FinishedAction, torrent.DownloadMinSize, torrent.DownloadManualFiles);
                }
                else
                {
                    await UploadMagnet(torrent.FileOrMagnet,
                                       torrent.Category,
                                       torrent.DownloadAction,
                                       torrent.FinishedAction,
                                       torrent.DownloadMinSize,
                                       torrent.DownloadManualFiles);
                }
            }
            finally
            {
                TorrentResetLock.Release();
            }
        }

        public async Task RetryDownload(Guid downloadId)
        {
            var download = await _downloads.GetById(downloadId);

            if (download == null)
            {
                return;
            }

            while (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
            {
                downloadClient.Cancel();

                await Task.Delay(100);
            }

            while (TorrentRunner.ActiveUnpackClients.TryGetValue(download.DownloadId, out var unpackClient))
            {
                unpackClient.Cancel();

                await Task.Delay(100);
            }

            await TorrentResetLock.WaitAsync();

            try
            {

                await DeleteDownload(download.TorrentId, download.DownloadId);

                await _torrentData.UpdateComplete(download.TorrentId, null);

                await _downloads.Reset(downloadId);
            }
            finally
            {
                TorrentResetLock.Release();
            }
        }
        
        public async Task UpdateComplete(Guid torrentId, DateTimeOffset datetime)
        {
            await _torrentData.UpdateComplete(torrentId, datetime);
        }

        public async Task UpdateFilesSelected(Guid torrentId, DateTimeOffset datetime)
        {
            await _torrentData.UpdateFilesSelected(torrentId, datetime);
        }

        public async Task<Torrent> GetById(Guid torrentId)
        {
            var torrent = await _torrentData.GetById(torrentId);

            if (torrent != null)
            {
                await Update(torrent);

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

            return torrent;
        }

        private static String DownloadPath(Torrent torrent)
        {
            var settingDownloadPath = Settings.Get.DownloadPath;

            if (!String.IsNullOrWhiteSpace(torrent.Category))
            {
                settingDownloadPath = Path.Combine(settingDownloadPath, torrent.Category);
            }

            return settingDownloadPath;
        }

        private async Task Add(String rdTorrentId,
                               String infoHash,
                               String category,
                               TorrentDownloadAction downloadAction,
                               TorrentFinishedAction finishedAction,
                               Int32 downloadMinSize,
                               String downloadManualFiles,
                               String fileOrMagnetContents,
                               Boolean isFile)
        {
            await RealDebridUpdateLock.WaitAsync();
            
            try
            {
                var torrent = await _torrentData.GetByHash(infoHash);

                if (torrent != null)
                {
                    return;
                }

                var newTorrent = await _torrentData.Add(rdTorrentId,
                                                        infoHash,
                                                        category,
                                                        downloadAction,
                                                        finishedAction,
                                                        downloadMinSize,
                                                        downloadManualFiles,
                                                        fileOrMagnetContents,
                                                        isFile);

                await Update(newTorrent);
            }
            finally
            {
                RealDebridUpdateLock.Release();
            }
        }

        private async Task Update(Torrent torrent)
        {
            var originalTorrent = JsonConvert.SerializeObject(torrent,
                                                              new JsonSerializerSettings
                                                              {
                                                                  ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                                              });

            var rdTorrent = await GetRdNetClient().Torrents.GetInfoAsync(torrent.RdId);

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

            var newTorrent = JsonConvert.SerializeObject(torrent,
                                                         new JsonSerializerSettings
                                                         {
                                                             ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                                         });

            if (originalTorrent != newTorrent)
            {
                await _torrentData.UpdateRdData(torrent);
            }
        }

        public async Task UpdateRetryCount(Guid torrentId, Int32 retryCount)
        {
            await _torrentData.UpdateRetryCount(torrentId, retryCount);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RdtClient.Data.Enums;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services
{
    public interface ITorrentRunner
    {
        Task Initialize();
        Task Tick();
    }

    public class TorrentRunner : ITorrentRunner
    {
        private static DateTime _nextUpdate = DateTime.UtcNow;

        public static readonly ConcurrentDictionary<Guid, DownloadClient> ActiveDownloadClients = new ConcurrentDictionary<Guid, DownloadClient>();
        public static readonly ConcurrentDictionary<Guid, UnpackClient> ActiveUnpackClients = new ConcurrentDictionary<Guid, UnpackClient>();
        private readonly IDownloads _downloads;
        private readonly IRemoteService _remoteService;

        private readonly ISettings _settings;
        private readonly ITorrents _torrents;

        public TorrentRunner(ISettings settings, ITorrents torrents, IDownloads downloads, IRemoteService remoteService)
        {
            _settings = settings;
            _torrents = torrents;
            _downloads = downloads;
            _remoteService = remoteService;
        }

        public async Task Initialize()
        {
            // When starting up reset any pending downloads or unpackings so that they are restarted.
            var torrents = await _torrents.Get();
            
            torrents = torrents.Where(m => m.Completed == null).ToList();
            
            var downloads = torrents.SelectMany(m => m.Downloads)
                                    .Where(m => m.DownloadQueued != null && m.DownloadStarted != null && m.DownloadFinished == null)
                                    .OrderBy(m => m.DownloadQueued);

            foreach (var download in downloads)
            {
                await _downloads.UpdateDownloadStarted(download.DownloadId, null);
            }

            var unpacks = torrents.SelectMany(m => m.Downloads)
                                  .Where(m => m.UnpackingQueued != null && m.UnpackingStarted != null && m.UnpackingFinished == null)
                                  .OrderBy(m => m.DownloadQueued);

            foreach (var download in unpacks)
            {
                await _downloads.UpdateUnpackingStarted(download.DownloadId, null);
            }
        }

        public async Task Tick()
        {
            var settings = await _settings.GetAll();
            
            var settingApiKey = settings.GetString("RealDebridApiKey");
            if (String.IsNullOrWhiteSpace(settingApiKey))
            {
                return;
            }

            var settingMinFileSize = settings.GetNumber("MinFileSize");
            if (settingMinFileSize <= 0)
            {
                settingMinFileSize = 0;
            }

            settingMinFileSize = settingMinFileSize * 1024 * 1024;

            var settingOnlyDownloadAvailableFilesRaw = settings.GetNumber("OnlyDownloadAvailableFiles");
            var settingOnlyDownloadAvailableFiles = settingOnlyDownloadAvailableFilesRaw == 1;

            var settingDownloadLimit = settings.GetNumber("DownloadLimit");
            if (settingDownloadLimit < 1)
            {
                settingDownloadLimit = 1;
            }

            var settingUnpackLimit = settings.GetNumber("UnpackLimit");
            if (settingUnpackLimit < 1)
            {
                settingUnpackLimit = 1;
            }

            var settingDownloadPath = settings.GetString("DownloadPath");
            if (String.IsNullOrWhiteSpace(settingDownloadPath))
            {
                return;
            }

            // Check if any torrents are finished downloading to the host, remove them from the active download list.
            var completedActiveDownloads = ActiveDownloadClients.Where(m => m.Value.Finished).ToList();

            if (completedActiveDownloads.Count > 0)
            {
                foreach (var (downloadId, downloadClient) in completedActiveDownloads)
                {
                    if (downloadClient.Error != null)
                    {
                        await _downloads.UpdateError(downloadId, downloadClient.Error);
                        await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        await _downloads.UpdateDownloadFinished(downloadId, DateTimeOffset.UtcNow);
                        await _downloads.UpdateUnpackingQueued(downloadId, DateTimeOffset.UtcNow);
                    }

                    ActiveDownloadClients.TryRemove(downloadId, out _);
                }
            }

            // Check if any torrents are finished unpacking, remove them from the active unpack list.
            var completedUnpacks = ActiveUnpackClients.Where(m => m.Value.Finished).ToList();

            if (completedUnpacks.Count > 0)
            {
                foreach (var (downloadId, unpackClient) in completedUnpacks)
                {
                    if (unpackClient.Error != null)
                    {
                        await _downloads.UpdateError(downloadId, unpackClient.Error);
                        await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        await _downloads.UpdateUnpackingFinished(downloadId, DateTimeOffset.UtcNow);
                        await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                    }

                    ActiveUnpackClients.TryRemove(downloadId, out _);
                }
            }
            
            var torrents = await _torrents.Get();

            // Only poll RealDebrid every second when a hub is connected, otherwise every 30 seconds
            if (_nextUpdate < DateTime.UtcNow && torrents.Count > 0)
            {
                var updateTime = 30;

                if (RdtHub.HasConnections)
                {
                    updateTime = 1;
                }

                _nextUpdate = DateTime.UtcNow.AddSeconds(updateTime);

                await _torrents.Update();
                
                // Re-get torrents to account for updated info
                torrents = await _torrents.Get();
            }

            torrents = torrents.Where(m => m.Completed == null).ToList();

            // Check if there are any downloads that are queued and can be started.
            var queuedDownloads = torrents.SelectMany(m => m.Downloads)
                                          .Where(m => m.Completed == null && m.DownloadQueued != null && m.DownloadStarted == null)
                                          .OrderBy(m => m.DownloadQueued);

            foreach (var download in queuedDownloads)
            {
                if (TorrentRunner.ActiveDownloadClients.Count >= settingDownloadLimit)
                {
                    return;
                }

                if (TorrentRunner.ActiveDownloadClients.ContainsKey(download.DownloadId))
                {
                    return;
                }

                download.DownloadStarted = DateTime.UtcNow;
                await _downloads.UpdateDownloadStarted(download.DownloadId, download.DownloadStarted);

                var downloadPath = settingDownloadPath;
                
                if (!String.IsNullOrWhiteSpace(download.Torrent.Category))
                {
                    downloadPath = Path.Combine(downloadPath, download.Torrent.Category);
                }

                // Start the download process
                var downloadClient = new DownloadClient(download, downloadPath);
                
                if (TorrentRunner.ActiveDownloadClients.TryAdd(download.DownloadId, downloadClient))
                {
                    await downloadClient.Start(settings);
                }
            }

            // Check if there are any unpacks that are queued and can be started.
            var queuedUnpacks = torrents.SelectMany(m => m.Downloads)
                                        .Where(m => m.Completed == null && m.UnpackingQueued != null && m.UnpackingStarted == null)
                                        .OrderBy(m => m.DownloadQueued);

            foreach (var download in queuedUnpacks)
            {
                // Check if the unpacking process is even needed
                var uri = new Uri(download.Link);
                var fileName = uri.Segments.Last();
                var extension = Path.GetExtension(fileName);
                        
                if (extension != ".rar")
                {
                    download.UnpackingStarted = DateTimeOffset.UtcNow;
                    download.UnpackingFinished = DateTimeOffset.UtcNow;
                    download.Completed = DateTimeOffset.UtcNow;

                    await _downloads.UpdateUnpackingStarted(download.DownloadId, download.UnpackingStarted);
                    await _downloads.UpdateUnpackingFinished(download.DownloadId, download.UnpackingFinished);
                    await _downloads.UpdateCompleted(download.DownloadId, download.Completed);
                    
                    continue;
                }
                
                // Check if we have reached the download limit, if so queue the download, but don't start it.
                if (TorrentRunner.ActiveUnpackClients.Count >= settingUnpackLimit)
                {
                    return;
                }
            
                if (TorrentRunner.ActiveUnpackClients.ContainsKey(download.DownloadId))
                {
                    return;
                }

                download.UnpackingStarted = DateTimeOffset.UtcNow;
                await _downloads.UpdateUnpackingStarted(download.DownloadId, download.UnpackingStarted);
                
                var downloadPath = settingDownloadPath;
                
                if (!String.IsNullOrWhiteSpace(download.Torrent.Category))
                {
                    downloadPath = Path.Combine(downloadPath, download.Torrent.Category);
                }

                // Start the unpacking process
                var unpackClient = new UnpackClient(download, downloadPath);

                if (TorrentRunner.ActiveUnpackClients.TryAdd(download.DownloadId, unpackClient))
                {
                    await unpackClient.Start();
                }
            }

            foreach (var torrent in torrents)
            {
                // If torrent is erroring out on the RealDebrid side, skip processing this torrent.
                if (torrent.RdStatus == RealDebridStatus.Error)
                {
                    continue;
                }

                // RealDebrid is waiting for file selection, select which files to download.
                if (torrent.AutoDownload && torrent.RdStatus == RealDebridStatus.WaitingForFileSelection)
                {
                    var files = torrent.Files;

                    if (settingOnlyDownloadAvailableFiles)
                    {
                        var availableFiles = await _torrents.GetAvailableFiles(torrent.Hash);

                        files = torrent.Files.Where(m => availableFiles.Any(f => m.Path.EndsWith(f))).ToList();
                    }

                    if (settingMinFileSize > 0)
                    {
                        files = files.Where(m => m.Bytes > settingMinFileSize).ToList();
                    }

                    var fileIds = files.Select(m => m.Id.ToString()).ToArray();

                    await _torrents.SelectFiles(torrent.RdId, fileIds);
                }

                // If the torrent doesn't have any files at this point, don't process it further.
                if (torrent.Files.Count == 0)
                {
                    continue;
                }

                // RealDebrid finished downloading the torrent, process the file to host.
                if (torrent.AutoDownload && torrent.RdStatus == RealDebridStatus.Finished)
                {
                    // If the torrent doesn't have any Downloads, unrestrict the links and add them to the database.
                    if (torrent.Downloads.Count == 0)
                    {
                        await _torrents.Unrestrict(torrent.TorrentId);

                        continue;
                    }

                    // If the torrent has any files that need starting to be downloaded, download them.
                    var downloadsPending = torrent.Downloads
                                                  .Where(m => m.Completed == null &&
                                                              m.DownloadStarted == null &&
                                                              m.DownloadFinished == null)
                                                  .ToList();

                    if (downloadsPending.Count > 0)
                    {
                        foreach (var download in downloadsPending)
                        {
                            await _torrents.Download(download.DownloadId);
                        }

                        continue;
                    }
                }

                if (torrent.AutoUnpack && torrent.RdStatus == RealDebridStatus.Finished)
                {
                    // If all files are finished downloading, move to the unpacking step.
                    var unpackingPending = torrent.Downloads
                                                  .Where(m => m.Completed == null &&
                                                              m.DownloadFinished != null &&
                                                              m.UnpackingStarted == null &&
                                                              m.UnpackingFinished == null)
                                                  .ToList();

                    if (unpackingPending.Count > 0)
                    {
                        foreach (var download in unpackingPending)
                        {
                            await _torrents.Unpack(download.DownloadId);
                        }

                        continue;
                    }
                }

                // Check if torrent is complete
                if (torrent.Downloads.Count > 0)
                {
                    var allComplete = torrent.Downloads.All(m => m.Completed != null);

                    if (allComplete)
                    {
                        await _torrents.UpdateComplete(torrent.TorrentId, DateTimeOffset.UtcNow);

                        // Remove the torrent from RealDebrid
                        if (torrent.AutoDelete)
                        {
                            await _torrents.Delete(torrent.TorrentId, true, true, false);
                        }
                    }
                }
            }
            
            await _remoteService.Update();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using RdtClient.Data.Enums;
using RdtClient.Service.Helpers;
using Serilog;

namespace RdtClient.Service.Services
{
    public interface ITorrentRunner
    {
        Task Initialize();
        Task Tick();
    }

    public class TorrentRunner : ITorrentRunner
    {
        private const Int32 RetryCount = 3;

        private static DateTime _nextUpdate = DateTime.UtcNow;

        public static readonly ConcurrentDictionary<Guid, DownloadClient> ActiveDownloadClients = new();
        public static readonly ConcurrentDictionary<Guid, UnpackClient> ActiveUnpackClients = new();
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
                                    .Where(m => m.DownloadQueued != null && m.DownloadStarted != null && m.DownloadFinished == null && m.Error == null)
                                    .OrderBy(m => m.DownloadQueued);

            foreach (var download in downloads)
            {
                await _downloads.UpdateDownloadStarted(download.DownloadId, null);
            }

            var unpacks = torrents.SelectMany(m => m.Downloads)
                                  .Where(m => m.UnpackingQueued != null && m.UnpackingStarted != null && m.UnpackingFinished == null && m.Error == null)
                                  .OrderBy(m => m.DownloadQueued);

            foreach (var download in unpacks)
            {
                await _downloads.UpdateUnpackingStarted(download.DownloadId, null);
            }
        }

        public async Task Tick()
        {
            var sw = new Stopwatch();
            sw.Start();

            Log.Debug("TorrentRunner Tick Start");

            var settings = await _settings.GetAll();
            
            var settingApiKey = settings.GetString("RealDebridApiKey");
            if (String.IsNullOrWhiteSpace(settingApiKey))
            {
                Log.Debug($"No RealDebridApiKey set!");
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

            Log.Debug($"Currently {ActiveDownloadClients.Count} active downloads");
            Log.Debug($"Currently {ActiveUnpackClients.Count} active unpacks");

            // Check if any torrents are finished downloading to the host, remove them from the active download list.
            var completedActiveDownloads = ActiveDownloadClients.Where(m => m.Value.Finished).ToList();

            Log.Debug($"Processing {completedActiveDownloads.Count} completed downloads");

            foreach (var (downloadId, downloadClient) in completedActiveDownloads)
            {
                if (downloadClient.Error != null)
                {
                    // Retry the download
                    Log.Debug($"Processing active download {downloadId}: error {downloadClient.Error}");
                    var download = await _downloads.GetById(downloadId);

                    if (download.RetryCount < RetryCount)
                    {
                        Log.Debug($"Processing active download {downloadId}: error {downloadClient.Error}, retry count {download.RetryCount}/{RetryCount}");

                        await _downloads.UpdateRetryCount(downloadId, download.RetryCount + 1);
                        await _torrents.Download(downloadId);
                    }
                    else
                    {
                        Log.Debug($"Processing active download {downloadId}: error {downloadClient.Error}, not retrying");

                        await _downloads.UpdateError(downloadId, downloadClient.Error);
                        await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                    }
                }
                else
                {
                    Log.Debug($"Processing active download {downloadId}: finished succesfully");

                    await _downloads.UpdateDownloadFinished(downloadId, DateTimeOffset.UtcNow);
                    await _downloads.UpdateUnpackingQueued(downloadId, DateTimeOffset.UtcNow);
                }

                ActiveDownloadClients.TryRemove(downloadId, out _);

                Log.Debug($"Removed active download {downloadId} from queue");
            }
            
            // Check if any torrents are finished unpacking, remove them from the active unpack list.
            var completedUnpacks = ActiveUnpackClients.Where(m => m.Value.Finished).ToList();
            
            Log.Debug($"Processing {completedUnpacks.Count} completed extractions");

            foreach (var (downloadId, unpackClient) in completedUnpacks)
            {
                if (unpackClient.Error != null)
                {
                    Log.Debug($"Processing active unpack {downloadId}: error {unpackClient.Error}");

                    await _downloads.UpdateError(downloadId, unpackClient.Error);
                    await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                }
                else
                {
                    Log.Debug($"Processing active unpack {downloadId}: finished succesfully");

                    await _downloads.UpdateUnpackingFinished(downloadId, DateTimeOffset.UtcNow);
                    await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                }

                ActiveUnpackClients.TryRemove(downloadId, out _);

                Log.Debug($"Removed active unpack {downloadId} from queue");
            }
            
            var torrents = await _torrents.Get();

            Log.Debug($"Found {torrents.Count} torrents");

            // Only poll RealDebrid every second when a hub is connected, otherwise every 30 seconds
            if (_nextUpdate < DateTime.UtcNow && torrents.Count > 0)
            {
                Log.Debug($"Updating torrent info from RealDebrid");

                var updateTime = 30;

                if (RdtHub.HasConnections)
                {
                    updateTime = 1;
                }

                _nextUpdate = DateTime.UtcNow.AddSeconds(updateTime);

                await _torrents.Update();
                
                // Re-get torrents to account for updated info
                torrents = await _torrents.Get();

                Log.Debug($"Finished updating torrent info from RealDebrid, next update in {updateTime} seconds");
            }

            torrents = torrents.Where(m => m.Completed == null).ToList();

            // Check if there are any downloads that are queued and can be started.
            var queuedDownloads = torrents.SelectMany(m => m.Downloads)
                                          .Where(m => m.Completed == null && m.DownloadQueued != null && m.DownloadStarted == null && m.Error == null)
                                          .OrderBy(m => m.DownloadQueued)
                                          .ToList();

            Log.Debug($"Found {queuedDownloads.Count} torrents queued for download");

            foreach (var download in queuedDownloads)
            {
                Log.Debug($"Starting download {download.DownloadId}");

                var torrentDownload = torrents.First(m => m.TorrentId == download.TorrentId);

                if (TorrentRunner.ActiveDownloadClients.Count >= settingDownloadLimit)
                {
                    Log.Debug($"Not starting download {download.DownloadId} because there are already the max number of downloads active");

                    continue;
                }

                if (TorrentRunner.ActiveDownloadClients.ContainsKey(download.DownloadId))
                {
                    Log.Debug($"Not starting download {download.DownloadId} because this download is already active");

                    continue;
                }

                try
                {
                    var downloadLink = await _torrents.UnrestrictLink(download.DownloadId);
                    download.Link = downloadLink;
                }
                catch (Exception ex)
                {
                    await _downloads.UpdateError(download.DownloadId, ex.Message);
                    await _downloads.UpdateCompleted(download.DownloadId, DateTimeOffset.UtcNow);
                    download.Error = ex.Message;
                    download.Completed = DateTimeOffset.UtcNow;
                    continue;
                }

                download.DownloadStarted = DateTime.UtcNow;
                await _downloads.UpdateDownloadStarted(download.DownloadId, download.DownloadStarted);

                var downloadPath = settingDownloadPath;
                
                if (!String.IsNullOrWhiteSpace(torrentDownload.Category))
                {
                    downloadPath = Path.Combine(downloadPath, torrentDownload.Category);
                }

                Log.Debug($"Setting download path for {download.DownloadId} to {downloadPath}");

                // Start the download process
                var downloadClient = new DownloadClient(download, torrentDownload, downloadPath);
                
                if (TorrentRunner.ActiveDownloadClients.TryAdd(download.DownloadId, downloadClient))
                {
                    Log.Debug($"Added download {download.DownloadId} to active downloads");

                    await downloadClient.Start(settings);

                    Log.Debug($"Download {download.DownloadId} started");
                }
            }

            // Check if there are any unpacks that are queued and can be started.
            var queuedUnpacks = torrents.SelectMany(m => m.Downloads)
                                        .Where(m => m.Completed == null && m.UnpackingQueued != null && m.UnpackingStarted == null && m.Error == null)
                                        .OrderBy(m => m.DownloadQueued)
                                        .ToList();

            Log.Debug($"Found {queuedUnpacks.Count} torrents queued for unpacking");

            foreach (var download in queuedUnpacks)
            {
                Log.Debug($"Starting unpack {download.DownloadId}");

                var torrentDownload = torrents.First(m => m.TorrentId == download.TorrentId);

                // Check if the unpacking process is even needed
                var uri = new Uri(download.Link);
                var fileName = uri.Segments.Last();

                fileName = HttpUtility.UrlDecode(fileName);

                Log.Debug($"Found file name {fileName} for {download.DownloadId}");

                var extension = Path.GetExtension(fileName);
                        
                if (extension != ".rar")
                {
                    Log.Debug($"No need to unpack {download.DownloadId}, setting it as unpacked");

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
                    Log.Debug($"Not starting unpack {download.DownloadId} because there are already the max number of unpacks active");

                    continue;
                }
            
                if (TorrentRunner.ActiveUnpackClients.ContainsKey(download.DownloadId))
                {
                    Log.Debug($"Not starting unpack {download.DownloadId} because this download is already active");

                    continue;
                }

                download.UnpackingStarted = DateTimeOffset.UtcNow;
                await _downloads.UpdateUnpackingStarted(download.DownloadId, download.UnpackingStarted);
                
                var downloadPath = settingDownloadPath;
                
                if (!String.IsNullOrWhiteSpace(torrentDownload.Category))
                {
                    downloadPath = Path.Combine(downloadPath, torrentDownload.Category);
                }

                Log.Debug($"Setting unpack path for {download.DownloadId} to {downloadPath}");

                // Start the unpacking process
                var unpackClient = new UnpackClient(download, downloadPath);

                if (TorrentRunner.ActiveUnpackClients.TryAdd(download.DownloadId, unpackClient))
                {
                    Log.Debug($"Added unpack {download.DownloadId} to active unpacks");

                    await unpackClient.Start();

                    Log.Debug($"Unpack {download.DownloadId} started");
                }
            }

            foreach (var torrent in torrents)
            {
                // If torrent is erroring out on the RealDebrid side, skip processing this torrent.
                if (torrent.RdStatus == RealDebridStatus.Error)
                {
                    Log.Debug($"Torrent {torrent.RdId} has an error: {torrent.RdStatusRaw}, not processing further");

                    continue;
                }

                // RealDebrid is waiting for file selection, select which files to download.
                if ((torrent.RdStatus == RealDebridStatus.WaitingForFileSelection || torrent.RdStatus == RealDebridStatus.Finished) &&
                    torrent.Downloads.Count == 0)
                {
                    Log.Debug($"Torrent {torrent.RdId} selecting files");

                    var files = torrent.Files;

                    if (settingOnlyDownloadAvailableFiles)
                    {
                        Log.Debug($"Torrent {torrent.RdId} determining which files are already available");

                        var availableFiles = await _torrents.GetAvailableFiles(torrent.Hash);

                        Log.Debug($"Found {availableFiles.Count} available files for torrent {torrent.RdId}");

                        if (availableFiles.Count > 0)
                        {
                            files = torrent.Files.Where(m => availableFiles.Any(f => m.Path.EndsWith(f))).ToList();

                            Log.Debug($"Selecting {files.Count} available files for torrent {torrent.RdId}");
                        }
                    }

                    if (settingMinFileSize > 0)
                    {
                        Log.Debug($"Torrent {torrent.RdId} determining which files are over {settingMinFileSize} bytes");

                        files = files.Where(m => m.Bytes > settingMinFileSize).ToList();

                        Log.Debug($"Found {files.Count} files that match the minimum file size criterea for torrent {torrent.RdId}");
                    }

                    if (files.Count == 0)
                    {
                        Log.Debug($"Filtered all files out for {torrent.RdId}! Downloading ALL files!");

                        files = torrent.Files;
                    }

                    var fileIds = files.Select(m => m.Id.ToString()).ToArray();

                    Log.Debug($"Selecting files for torrent {torrent.RdId}: {String.Join(", ", fileIds)}");

                    await _torrents.SelectFiles(torrent.TorrentId, fileIds);
                }

                // If the torrent doesn't have any files at this point, don't process it further.
                if (torrent.Files.Count == 0)
                {
                    Log.Debug($"No files found for torrent {torrent.RdId}!");
                    continue;
                }

                // RealDebrid finished downloading the torrent, process the file to host.
                if (torrent.RdStatus == RealDebridStatus.Finished)
                {
                    // If the torrent has any files that need starting to be downloaded, download them.
                    var downloadsPending = torrent.Downloads
                                                  .Where(m => m.Completed == null &&
                                                              m.DownloadStarted == null &&
                                                              m.DownloadFinished == null &&
                                                              m.Error == null)
                                                  .OrderBy(m => m.Added)
                                                  .ToList();

                    Log.Debug($"Torrent {torrent.RdId} found {downloadsPending.Count} downloads pending");

                    if (downloadsPending.Count > 0)
                    {
                        foreach (var download in downloadsPending)
                        {
                            Log.Debug($"Torrent {torrent.RdId} starting download {download.DownloadId}");

                            await _torrents.Download(download.DownloadId);
                        }

                        continue;
                    }

                    // If all files are finished downloading, move to the unpacking step.
                    var unpackingPending = torrent.Downloads
                                                  .Where(m => m.Completed == null &&
                                                              m.DownloadFinished != null &&
                                                              m.UnpackingStarted == null &&
                                                              m.UnpackingFinished == null)
                                                  .ToList();

                    Log.Debug($"Torrent {torrent.RdId} found {unpackingPending.Count} unpacks pending");

                    if (unpackingPending.Count > 0)
                    {
                        foreach (var download in unpackingPending)
                        {
                            Log.Debug($"Torrent {torrent.RdId} starting unpack {download.DownloadId}");

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
                        Log.Debug($"Torrent {torrent.RdId} all downloads complete");

                        await _torrents.UpdateComplete(torrent.TorrentId, DateTimeOffset.UtcNow);

                        // Remove the torrent from RealDebrid
                        if (torrent.AutoDelete)
                        {
                            Log.Debug($"Torrent {torrent.RdId} removing");

                            await _torrents.Delete(torrent.TorrentId, true, true, false);
                        }
                    }
                }
            }
            
            await _remoteService.Update();

            sw.Stop();

            Log.Debug($"TorrentRunner Tick End (took {sw.ElapsedMilliseconds}ms)");
        }
    }
}

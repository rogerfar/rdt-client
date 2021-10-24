using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using RdtClient.Data.Enums;
using Serilog;

namespace RdtClient.Service.Services
{
    public class TorrentRunner
    {
        private const Int32 DownloadRetryCount = 3;
        private const Int32 TorrentRetryCount = 2;

        private static DateTime _nextUpdate = DateTime.UtcNow;

        public static readonly ConcurrentDictionary<Guid, DownloadClient> ActiveDownloadClients = new();
        public static readonly ConcurrentDictionary<Guid, UnpackClient> ActiveUnpackClients = new();
        private readonly Downloads _downloads;
        private readonly RemoteService _remoteService;
        
        private readonly Torrents _torrents;

        public TorrentRunner(Torrents torrents, Downloads downloads, RemoteService remoteService)
        {
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
          
            if (String.IsNullOrWhiteSpace(Settings.Get.RealDebridApiKey))
            {
                Log.Debug($"No RealDebridApiKey set!");
                return;
            }
            
            var settingDownloadLimit = Settings.Get.DownloadLimit;
            if (settingDownloadLimit < 1)
            {
                settingDownloadLimit = 1;
            }

            var settingUnpackLimit = Settings.Get.UnpackLimit;
            if (settingUnpackLimit < 1)
            {
                settingUnpackLimit = 1;
            }

            var settingDownloadPath = Settings.Get.DownloadPath;
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

                    if (download == null)
                    {
                        ActiveDownloadClients.TryRemove(downloadId, out _);

                        Log.Debug($"Download with ID {downloadId} not found! Removed from queue");
                        continue;
                    }

                    if (download.RetryCount < DownloadRetryCount)
                    {
                        Log.Debug($"Processing active download {downloadId}: error {downloadClient.Error}, download retry count {download.RetryCount}/{DownloadRetryCount}, torrent retry count {download.Torrent.RetryCount}/{TorrentRetryCount}, retrying download");

                        await _downloads.UpdateRetryCount(downloadId, download.RetryCount + 1);
                        await _torrents.Download(downloadId);
                    }
                    else if (download.Torrent.RetryCount < TorrentRetryCount)
                    {
                        Log.Debug($"Processing active download {downloadId}: error {downloadClient.Error}, download retry count {download.RetryCount}/{DownloadRetryCount}, torrent retry count {download.Torrent.RetryCount}/{TorrentRetryCount}, retrying torrent");

                        Task.Run(async () =>
                        {
                            await _torrents.RetryTorrent(download.TorrentId);
                        });
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

            // Only poll Real-Debrid every second when a hub is connected, otherwise every 30 seconds
            if (_nextUpdate < DateTime.UtcNow && torrents.Count > 0)
            {
                Log.Debug($"Updating torrent info from Real-Debrid");

                var updateTime = 30;

                if (RdtHub.HasConnections)
                {
                    updateTime = 1;
                }

                _nextUpdate = DateTime.UtcNow.AddSeconds(updateTime);

                await _torrents.Update();
                
                // Re-get torrents to account for updated info
                torrents = await _torrents.Get();

                Log.Debug($"Finished updating torrent info from Real-Debrid, next update in {updateTime} seconds");
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

                    Log.Information($"Cannot unrestrict: {ex.Message}");
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

                    var remoteId = await downloadClient.Start(Settings.Get);

                    if (!String.IsNullOrWhiteSpace(remoteId) && download.RemoteId != remoteId)
                    {
                        Log.Debug($"Received ID {remoteId} for download {download.DownloadId}");

                        await _downloads.UpdateRemoteId(download.DownloadId, remoteId);
                    }

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

                if (download.Link == null)
                {
                    await _downloads.UpdateError(download.DownloadId, "Download Link cannot be null");

                    continue;
                }

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

                    unpackClient.Start();

                    Log.Debug($"Unpack {download.DownloadId} started");
                }
            }

            foreach (var torrent in torrents)
            {
                // If torrent is erroring out on the Real-Debrid side, skip processing this torrent.
                if (torrent.RdStatus == RealDebridStatus.Error)
                {
                    Log.Debug($"Torrent {torrent.RdId} has an error: {torrent.RdStatusRaw}, not processing further");

                    continue;
                }

                // The files are selected but there are no downloads yet, check if Real-Debrid has generated links yet.
                if (torrent.Downloads.Count == 0 && torrent.FilesSelected != null)
                {
                    Log.Debug($"Torrent {torrent.RdId} checking for links");

                    await _torrents.CheckForLinks(torrent.TorrentId);
                }

                // Real-Debrid is waiting for file selection, select which files to download.
                if ((torrent.RdStatus == RealDebridStatus.WaitingForFileSelection || torrent.RdStatus == RealDebridStatus.Finished) &&
                    torrent.FilesSelected == null &&
                    torrent.Downloads.Count == 0 &&
                    torrent.FilesSelected == null)
                {
                    Log.Debug($"Torrent {torrent.RdId} selecting files");

                    var files = torrent.Files;

                    if (torrent.DownloadAction == TorrentDownloadAction.DownloadAvailableFiles)
                    {
                        Log.Debug($"Torrent {torrent.RdId} determining which files are already available");

                        var availableFiles = await _torrents.GetAvailableFiles(torrent.Hash);

                        Log.Debug($"Found {files.Count}/{torrent.Files.Count} available files for torrent {torrent.RdId}");

                        files = torrent.Files.Where(m => availableFiles.Any(f => m.Path.EndsWith(f.Filename))).ToList();
                    }
                    else if (torrent.DownloadAction == TorrentDownloadAction.DownloadAll)
                    {
                        Log.Debug("Selecting all files");
                        files = torrent.Files.ToList();
                    }
                    else if (torrent.DownloadAction == TorrentDownloadAction.DownloadManual)
                    {
                        Log.Debug("Selecting manual selected files");
                        files = torrent.Files.Where(m => torrent.ManualFiles.Any(f => m.Path.EndsWith(f))).ToList();
                    }

                    Log.Debug($"Selecting {files.Count}/{torrent.Files.Count} files for torrent {torrent.RdId}");

                    if (torrent.DownloadAction != TorrentDownloadAction.DownloadManual && torrent.DownloadMinSize > 0)
                    {
                        var minFileSize = torrent.DownloadMinSize * 1024 * 1024;

                        Log.Debug($"Torrent {torrent.RdId} determining which files are over {minFileSize} bytes");

                        files = files.Where(m => m.Bytes > minFileSize)
                                     .ToList();

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

                    await _torrents.UpdateFilesSelected(torrent.TorrentId, DateTime.UtcNow);
                }

                // Real-Debrid finished downloading the torrent, process the file to host.
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

                        switch (torrent.FinishedAction)
                        {
                            case TorrentFinishedAction.RemoveAllTorrents:
                                Log.Debug($"Torrent {torrent.RdId} removing torrents from Real-Debrid and Real-Debrid Client, no files");
                                await _torrents.Delete(torrent.TorrentId, true, true, false);
                                break;
                            case TorrentFinishedAction.RemoveRealDebrid:
                                Log.Debug($"Torrent {torrent.RdId} removing torrents from Real-Debrid, no files");
                                await _torrents.Delete(torrent.TorrentId, false, true, false);
                                break;
                            case TorrentFinishedAction.None:
                                Log.Debug($"Torrent {torrent.RdId} not removing torrents or files");
                                break;
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

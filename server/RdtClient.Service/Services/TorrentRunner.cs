using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Web;
using Aria2NET;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.Downloaders;

namespace RdtClient.Service.Services;

public class TorrentRunner
{
    private static DateTime _nextUpdate = DateTime.UtcNow;

    public static readonly ConcurrentDictionary<Guid, DownloadClient> ActiveDownloadClients = new();
    public static readonly ConcurrentDictionary<Guid, UnpackClient> ActiveUnpackClients = new();

    private readonly ILogger<TorrentRunner> _logger;
    private readonly Torrents _torrents;
    private readonly Downloads _downloads;
    private readonly RemoteService _remoteService;
    private readonly HttpClient _httpClient;

    public TorrentRunner(ILogger<TorrentRunner> logger, Torrents torrents, Downloads downloads, RemoteService remoteService)
    {
        _logger = logger;
        _torrents = torrents;
        _downloads = downloads;
        _remoteService = remoteService;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task Initialize()
    {
        Log("Initializing TorrentRunner");

        var settingsCopy = JsonSerializer.Deserialize<DbSettings>(JsonSerializer.Serialize(Settings.Get));

        if (settingsCopy != null)
        {
            settingsCopy.RealDebridApiKey = "*****";
            settingsCopy.Aria2cSecret = "*****";

            Log(JsonSerializer.Serialize(settingsCopy));
        }

        // When starting up reset any pending downloads or unpackings so that they are restarted.
        var torrents = await _torrents.Get();
            
        torrents = torrents.Where(m => m.Completed == null).ToList();

        Log($"Found {torrents.Count} not completed torrents");

        foreach (var torrent in torrents)
        {
            foreach (var download in torrent.Downloads)
            {
                if (download.DownloadQueued != null && download.DownloadStarted != null && download.DownloadFinished == null && download.Error == null)
                {
                    Log("Resetting download status", download, torrent);

                    await _downloads.UpdateDownloadStarted(download.DownloadId, null);
                }

                if (download.UnpackingQueued != null && download.UnpackingStarted != null && download.UnpackingFinished == null && download.Error == null)
                {
                    Log("Resetting unpack status", download, torrent);

                    await _downloads.UpdateUnpackingStarted(download.DownloadId, null);
                }
            }
        }

        Log("TorrentRunner Initialized");
    }

    public async Task Tick()
    {
        if (String.IsNullOrWhiteSpace(Settings.Get.RealDebridApiKey))
        {
            Log($"No RealDebridApiKey set in settings");
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
            _logger.LogError("No DownloadPath set in settings");
            return;
        }

        var sw = new Stopwatch();
        sw.Start();

        if (!ActiveDownloadClients.IsEmpty || !ActiveUnpackClients.IsEmpty)
        {
            Log($"TorrentRunner Tick Start, {ActiveDownloadClients.Count} active downloads, {ActiveUnpackClients.Count} active unpacks");
        }

        if (ActiveDownloadClients.Any(m => m.Value.Type == "Aria2c"))
        {
            Log("Updating Aria2 status");

            var aria2NetClient = new Aria2NetClient(Settings.Get.Aria2cUrl, Settings.Get.Aria2cSecret, _httpClient, 1);

            var allDownloads = await aria2NetClient.TellAll();

            Log($"Found {allDownloads.Count} Aria2 downloads");

            foreach (var activeDownload in ActiveDownloadClients)
            {
                if (activeDownload.Value.Downloader is Aria2cDownloader aria2Downloader)
                {
                    await aria2Downloader.Update(allDownloads);
                }
            }

            Log("Finished updating Aria2 status");
        }

        // Check if any torrents are finished downloading to the host, remove them from the active download list.
        var completedActiveDownloads = ActiveDownloadClients.Where(m => m.Value.Finished).ToList();

        if (completedActiveDownloads.Count > 0)
        {
            Log($"Processing {completedActiveDownloads.Count} completed downloads");

            foreach (var (downloadId, downloadClient) in completedActiveDownloads)
            {
                var download = await _downloads.GetById(downloadId);

                if (download == null)
                {
                    ActiveDownloadClients.TryRemove(downloadId, out _);

                    Log($"Download with ID {downloadId} not found! Removed from download queue");

                    continue;
                }

                Log("Processing download", download, download.Torrent);

                if (!String.IsNullOrWhiteSpace(downloadClient.Error))
                {
                    // Retry the download if an error is encountered.
                    Log($"Download reported an error: {downloadClient.Error}", download, download.Torrent);
                    Log($"Download retry count {download.RetryCount}/{download.Torrent.DownloadRetryAttempts}, torrent retry count {download.Torrent.RetryCount}/{download.Torrent.TorrentRetryAttempts}", download, download.Torrent);
                        
                    if (download.RetryCount < download.Torrent.DownloadRetryAttempts)
                    {
                        Log($"Retrying download", download, download.Torrent);

                        await _downloads.Reset(downloadId);
                        await _downloads.UpdateRetryCount(downloadId, download.RetryCount + 1);
                    }
                    else
                    {
                        Log($"Not retrying download", download, download.Torrent);

                        await _downloads.UpdateError(downloadId, downloadClient.Error);
                        await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                    }
                }
                else
                {
                    Log($"Download finished successfully", download, download.Torrent);

                    await _downloads.UpdateDownloadFinished(downloadId, DateTimeOffset.UtcNow);
                    await _downloads.UpdateUnpackingQueued(downloadId, DateTimeOffset.UtcNow);
                }

                ActiveDownloadClients.TryRemove(downloadId, out _);

                Log($"Removed from ActiveDownloadClients", download, download.Torrent);
            }
        }

        // Check if any torrents are finished unpacking, remove them from the active unpack list.
        var completedUnpacks = ActiveUnpackClients.Where(m => m.Value.Finished).ToList();

        if (completedUnpacks.Count > 0)
        {
            Log($"Processing {completedUnpacks.Count} completed unpacks");

            foreach (var (downloadId, unpackClient) in completedUnpacks)
            {
                var download = await _downloads.GetById(downloadId);

                if (download == null)
                {
                    ActiveUnpackClients.TryRemove(downloadId, out _);

                    Log($"Download with ID {downloadId} not found! Removed from unpack queue");

                    continue;
                }

                if (unpackClient.Error != null)
                {
                    Log($"Unpack reported an error: {unpackClient.Error}", download, download.Torrent);
                        
                    await _downloads.UpdateError(downloadId, unpackClient.Error);
                    await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                }
                else
                {
                    Log($"Unpack finished successfully", download, download.Torrent);

                    await _downloads.UpdateUnpackingFinished(downloadId, DateTimeOffset.UtcNow);
                    await _downloads.UpdateCompleted(downloadId, DateTimeOffset.UtcNow);
                }

                ActiveUnpackClients.TryRemove(downloadId, out _);

                Log($"Removed from ActiveUnpackClients", download, download.Torrent);
            }
        }

        var torrents = await _torrents.Get();

        // Process torrent retries
        foreach (var torrent in torrents.Where(m => m.Retry != null))
        {
            try
            {
                Log($"Retrying torrent {torrent.RetryCount}/{torrent.TorrentRetryAttempts}", torrent);

                if (torrent.RetryCount > torrent.TorrentRetryAttempts)
                {
                    await _torrents.UpdateRetry(torrent.TorrentId, null, torrent.RetryCount);
                    Log($"Torrent reach max retry count");
                    continue;
                }

                await _torrents.RetryTorrent(torrent.TorrentId, torrent.RetryCount);
            }
            catch (Exception ex)
            {
                await _torrents.UpdateRetry(torrent.TorrentId, null, torrent.RetryCount);
                await _torrents.UpdateError(torrent.TorrentId, ex.Message);
            }
        }

        // Process torrent errors
        foreach (var torrent in torrents.Where(m => m.Error != null && m.DeleteOnError > 0))
        {
            if (torrent.Completed == null)
            {
                continue;
            }

            if (torrent.Completed.Value.AddMinutes(torrent.DeleteOnError) > DateTime.UtcNow)
            {
                continue;
            }

            Log($"Removing torrent because it has been {torrent.DeleteOnError} minutes in the error state", torrent);

            await _torrents.Delete(torrent.TorrentId, true, true, true);
        }
            
        // Process torrent lifetime
        foreach (var torrent in torrents.Where(m => m.Downloads.Count == 0 && m.Completed == null && m.Lifetime > 0))
        {
            if (torrent.Added.AddMinutes(torrent.Lifetime) > DateTime.UtcNow)
            {
                continue;
            }

            Log($"Torrent has reached its {torrent.Lifetime} minutes lifetime, marking as error", torrent);

            await _torrents.UpdateRetry(torrent.TorrentId, null, torrent.TorrentRetryAttempts);
            await _torrents.UpdateComplete(torrent.TorrentId, $"Torrent lifetime of {torrent.Lifetime} minutes reached", DateTimeOffset.UtcNow, false);
        }

        torrents = await _torrents.Get();

        torrents = torrents.Where(m => m.Completed == null).ToList();

        // Only poll Real-Debrid every second when a hub is connected, otherwise every 30 seconds
        if (_nextUpdate < DateTime.UtcNow && ((torrents.Count > 0 && Settings.Get.ProviderAutoImport == 0) || Settings.Get.ProviderAutoImport == 1))
        {
            Log($"Updating torrent info from Real-Debrid");

#if DEBUG
            var updateTime = 0;

            _nextUpdate = DateTime.UtcNow;
#else
                var updateTime = 30;

                if (RdtHub.HasConnections)
                {
                    updateTime = 10;
                }

                _nextUpdate = DateTime.UtcNow.AddSeconds(updateTime);
#endif

            await _torrents.UpdateRdData();
                
            // Re-get torrents to account for updated info
            torrents = await _torrents.Get();
            torrents = torrents.Where(m => m.Completed == null).ToList();

            Log($"Finished updating torrent info from Real-Debrid, next update in {updateTime} seconds");
        }

        if (torrents.Count > 0)
        {
            Log($"Processing {torrents.Count} torrents");
        }

        foreach (var torrent in torrents)
        {
            try
            {
                // Check if there are any downloads that are queued and can be started.
                var queuedDownloads = torrent.Downloads
                                             .Where(m => m.Completed == null && m.DownloadQueued != null && m.DownloadStarted == null && m.Error == null)
                                             .OrderBy(m => m.DownloadQueued)
                                             .ToList();

                foreach (var download in queuedDownloads)
                {
                    Log($"Processing to download", download, torrent);

                    if (ActiveDownloadClients.Count >= settingDownloadLimit)
                    {
                        Log($"Not starting download because there are already the max number of downloads active", download, torrent);

                        continue;
                    }

                    if (ActiveDownloadClients.ContainsKey(download.DownloadId))
                    {
                        Log($"Not starting download because this download is already active", download, torrent);

                        continue;
                    }

                    try
                    {
                        Log($"Unrestricting links", download, torrent);

                        var downloadLink = await _torrents.UnrestrictLink(download.DownloadId);
                        download.Link = downloadLink;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Cannot unrestrict link: {ex.Message}", ex.Message);

                        await _downloads.UpdateError(download.DownloadId, ex.Message);
                        await _downloads.UpdateCompleted(download.DownloadId, DateTimeOffset.UtcNow);
                        download.Error = ex.Message;
                        download.Completed = DateTimeOffset.UtcNow;

                        continue;
                    }

                    Log($"Marking download as started", download, torrent);

                    download.DownloadStarted = DateTime.UtcNow;
                    await _downloads.UpdateDownloadStarted(download.DownloadId, download.DownloadStarted);

                    var downloadPath = settingDownloadPath;

                    if (!String.IsNullOrWhiteSpace(torrent.Category))
                    {
                        downloadPath = Path.Combine(downloadPath, torrent.Category);
                    }

                    Log($"Setting download path to {downloadPath}", download, torrent);

                    // Start the download process
                    var downloadClient = new DownloadClient(download, torrent, downloadPath);

                    if (ActiveDownloadClients.TryAdd(download.DownloadId, downloadClient))
                    {
                        Log($"Starting download", download, torrent);

                        var remoteId = await downloadClient.Start(Settings.Get);

                        if (!String.IsNullOrWhiteSpace(remoteId) && download.RemoteId != remoteId)
                        {
                            Log($"Received ID {remoteId}", download, torrent);

                            await _downloads.UpdateRemoteId(download.DownloadId, remoteId);
                        }
                        else
                        {
                            Log($"No ID received", download, torrent);
                        }
                    }
                }

                // Check if there are any unpacks that are queued and can be started.
                var queuedUnpacks = torrent.Downloads
                                           .Where(m => m.Completed == null && m.UnpackingQueued != null && m.UnpackingStarted == null && m.Error == null)
                                           .OrderBy(m => m.DownloadQueued)
                                           .ToList();

                foreach (var download in queuedUnpacks)
                {
                    Log($"Starting unpack", download, torrent);

                    if (download.Link == null)
                    {
                        Log($"No download link found", download, torrent);

                        await _downloads.UpdateError(download.DownloadId, "Download Link cannot be null");
                        await _downloads.UpdateCompleted(download.DownloadId, DateTimeOffset.UtcNow);

                        continue;
                    }

                    // Check if the unpacking process is even needed
                    var uri = new Uri(download.Link);
                    var fileName = uri.Segments.Last();

                    fileName = HttpUtility.UrlDecode(fileName);

                    Log($"Found file name {fileName}", download, torrent);

                    var extension = Path.GetExtension(fileName);

                    if (extension != ".rar")
                    {
                        Log($"No need to unpack, setting it as unpacked", download, torrent);

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
                        Log($"Not starting unpack because there are already the max number of unpacks active", download, torrent);

                        continue;
                    }

                    if (TorrentRunner.ActiveUnpackClients.ContainsKey(download.DownloadId))
                    {
                        Log($"Not starting unpack because this download is already active", download, torrent);

                        continue;
                    }

                    download.UnpackingStarted = DateTimeOffset.UtcNow;
                    await _downloads.UpdateUnpackingStarted(download.DownloadId, download.UnpackingStarted);

                    var downloadPath = settingDownloadPath;

                    if (!String.IsNullOrWhiteSpace(torrent.Category))
                    {
                        downloadPath = Path.Combine(downloadPath, torrent.Category);
                    }

                    Log($"Setting unpack path to {downloadPath}", download, torrent);

                    // Start the unpacking process
                    var unpackClient = new UnpackClient(download, downloadPath);

                    if (TorrentRunner.ActiveUnpackClients.TryAdd(download.DownloadId, unpackClient))
                    {
                        Log($"Starting unpack", download, torrent);

                        unpackClient.Start();
                    }
                }

                Log("Processing", torrent);

                // If torrent is erroring out on the Real-Debrid side.
                if (torrent.RdStatus == TorrentStatus.Error)
                {
                    Log($"Torrent reported an error: {torrent.RdStatusRaw}", torrent);
                    Log($"Torrent retry count {torrent.RetryCount}/{torrent.TorrentRetryAttempts}", torrent);

                    Log($"Received RealDebrid error: {torrent.RdStatusRaw}, not processing further", torrent);

                    await _torrents.UpdateComplete(torrent.TorrentId, $"Received RealDebrid error: {torrent.RdStatusRaw}.", DateTimeOffset.UtcNow, true);

                    continue;
                }

                // Real-Debrid is waiting for file selection, select which files to download.
                if ((torrent.RdStatus == TorrentStatus.WaitingForFileSelection || torrent.RdStatus == TorrentStatus.Finished) &&
                    torrent.FilesSelected == null &&
                    torrent.Downloads.Count == 0)
                {
                    Log($"Selecting files", torrent);

                    await _torrents.SelectFiles(torrent.TorrentId);

                    await _torrents.UpdateFilesSelected(torrent.TorrentId, DateTime.UtcNow);
                }

                // Real-Debrid finished downloading the torrent, process the file to host.
                if (torrent.RdStatus == TorrentStatus.Finished)
                {
                    // The files are selected but there are no downloads yet, check if Real-Debrid has generated links yet.
                    if (torrent.Downloads.Count == 0 && torrent.FilesSelected != null)
                    {
                        Log($"Creating downloads", torrent);

                        await _torrents.CreateDownloads(torrent.TorrentId);
                    }
                }

                // Check if torrent is complete
                if (torrent.Downloads.Count > 0)
                {
                    var allComplete = torrent.Downloads.Count(m => m.Completed != null);

                    if (allComplete == torrent.Downloads.Count)
                    {
                        Log($"All downloads complete, marking torrent as complete", torrent);

                        await _torrents.UpdateComplete(torrent.TorrentId, null, DateTimeOffset.UtcNow, true);

                        switch (torrent.FinishedAction)
                        {
                            case TorrentFinishedAction.RemoveAllTorrents:
                                Log($"Removing torrents from Real-Debrid and Real-Debrid Client, no files", torrent);
                                await _torrents.Delete(torrent.TorrentId, true, true, false);

                                break;
                            case TorrentFinishedAction.RemoveRealDebrid:
                                Log($"Removing torrents from Real-Debrid, no files", torrent);
                                await _torrents.Delete(torrent.TorrentId, false, true, false);

                                break;
                            case TorrentFinishedAction.None:
                                Log($"Not removing torrents or files", torrent);

                                break;
                            default:
                                Log($"Invalid torrent FinishedAction {torrent.FinishedAction}", torrent);

                                break;
                        }

                        await _torrents.RunTorrentComplete(torrent.TorrentId);
                    }
                    else
                    {
                        Log($"Waiting for downloads to complete. {allComplete}/{torrent.Downloads.Count} complete", torrent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Torrent processing result in an unexpected exception: {Message}", ex.Message);
                await _torrents.UpdateComplete(torrent.TorrentId, ex.Message, DateTimeOffset.UtcNow, true);
            }
        }
            
        await _remoteService.Update();

        sw.Stop();

        if (sw.ElapsedMilliseconds > 1000)
        {
            Log($"TorrentRunner Tick End (took {sw.ElapsedMilliseconds}ms)");
        }
    }

    private void Log(String message, Download download, Torrent torrent)
    {
        if (download != null)
        {
            message = $"{message} {download.ToLog()}";
        }

        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        _logger.LogDebug(message);
    }

    private void Log(String message, Torrent torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        _logger.LogDebug(message);
    }
}
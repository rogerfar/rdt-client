using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RdtClient.Data.Enums;

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
            var settingApiKey = await _settings.GetString("RealDebridApiKey");
            var minFileSizeSetting = await _settings.GetNumber("MinFileSize");

            if (String.IsNullOrWhiteSpace(settingApiKey))
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

            // Only poll RealDebrid every 5 when a hub is connected, otherwise ever 30 seconds
            if (_nextUpdate < DateTime.UtcNow)
            {
                var updateTime = 30;

                if (RdtHub.HasConnections)
                {
                    updateTime = 5;
                }

                _nextUpdate = DateTime.UtcNow.AddSeconds(updateTime);

                await _torrents.Update();
            }

            var torrents = await _torrents.Get();

            torrents = torrents.Where(m => m.Completed == null).ToList();

            // Check if there are any downloads that are queued and can be started.
            var queuedDownloads = torrents.SelectMany(m => m.Downloads)
                                          .Where(m => m.DownloadQueued != null && m.DownloadStarted == null)
                                          .OrderBy(m => m.DownloadQueued);

            foreach (var download in queuedDownloads)
            {
                await _torrents.Download(download.DownloadId);
            }

            // Check if there are any unpacks that are queued and can be started.
            var queuedUnpacks = torrents.SelectMany(m => m.Downloads)
                                        .Where(m => m.UnpackingQueued != null && m.UnpackingStarted == null)
                                        .OrderBy(m => m.DownloadQueued);

            foreach (var download in queuedUnpacks)
            {
                await _torrents.Unpack(download.DownloadId);
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
                    var fileIds = torrent.Files
                                         .Select(m => m.Id.ToString())
                                         .ToArray();

                    if (minFileSizeSetting > 0)
                    {
                        fileIds = torrent.Files
                                         .Where(m => m.Bytes * 1024 * 1024 > minFileSizeSetting)
                                         .Select(m => m.Id.ToString())
                                         .ToArray();
                    }

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
                                                  .Where(m => m.DownloadStarted == null &&
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
                                                  .Where(m => m.DownloadFinished != null &&
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
                    var allComplete = torrent.Downloads.All(m => m.UnpackingFinished != null);

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

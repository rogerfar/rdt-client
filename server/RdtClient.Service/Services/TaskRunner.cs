using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;

namespace RdtClient.Service.Services
{
    public class TaskRunner : BackgroundService
    {
        public static readonly ConcurrentDictionary<Guid, DownloadManager> ActiveDownloads = new ConcurrentDictionary<Guid, DownloadManager>();

        private readonly ILogger<TaskRunner> _logger;
        private readonly IServiceProvider _services;

        public TaskRunner(ILogger<TaskRunner> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            _logger.LogInformation("TaskRunner started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWork();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("TaskRunner stopped.");
        }

        private async Task DoWork()
        {
            try
            {
                using var scope = _services.CreateScope();

                var downloads = scope.ServiceProvider.GetRequiredService<IDownloads>();
                var settings = scope.ServiceProvider.GetRequiredService<ISettings>();
                var torrents = scope.ServiceProvider.GetRequiredService<ITorrents>();

                var rdKey = await settings.GetString("RealDebridApiKey");

                if (String.IsNullOrWhiteSpace(rdKey))
                {
                    return;
                }

                await torrents.Update();

                await ProcessAutoDownloads(downloads, settings, torrents);
                await ProcessDownloads(downloads, settings, torrents);
                await ProcessStatus(downloads, settings, torrents);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private async Task ProcessAutoDownloads(IDownloads downloads, ISettings settings, ITorrents torrents)
        {
            var allTorrents = await torrents.Get();

            allTorrents = allTorrents.Where(m => (m.Status == TorrentStatus.WaitingForDownload && m.AutoDownload && m.Downloads.Count == 0) || m.Status == TorrentStatus.DownloadQueued)
                                     .ToList();

            foreach (var torrent in allTorrents)
            {
                await torrents.Download(torrent.TorrentId);
            }
        }

        private async Task ProcessDownloads(IDownloads downloads, ISettings settings, ITorrents torrents)
        {
            var allDownloads = await downloads.Get();

            allDownloads = allDownloads.Where(m => m.Status != DownloadStatus.Finished)
                                       .OrderByDescending(m => m.Status)
                                       .ThenByDescending(m => m.Added)
                                       .ToList();

            var maxDownloads = await settings.GetNumber("DownloadLimit");
            var destinationFolderPath = await settings.GetString("DownloadFolder");

            foreach (var download in allDownloads)
            {
                if (ActiveDownloads.ContainsKey(download.DownloadId))
                {
                    continue;
                }

                if (ActiveDownloads.Count >= maxDownloads)
                {
                    return;
                }

                // Prevent circular references
                download.Torrent.Downloads = null;

                await torrents.UpdateStatus(download.TorrentId, TorrentStatus.Downloading);

                await Task.Factory.StartNew(async delegate
                {
                    var downloadManager = new DownloadManager();

                    if (ActiveDownloads.TryAdd(download.DownloadId, downloadManager))
                    {
                        var folderPath = Path.Combine(destinationFolderPath, download.Torrent.RdName);

                        downloadManager.Download = download;
                        await downloadManager.Start(folderPath, destinationFolderPath);
                    }
                });
            }
        }

        private async Task ProcessStatus(IDownloads downloads, ISettings settings, ITorrents torrents)
        {
            foreach (var (downloadId, download) in ActiveDownloads)
            {
                if (download.NewStatus.HasValue)
                {
                    download.Download.Status = download.NewStatus.Value;
                    download.NewStatus = null;

                    await downloads.UpdateStatus(downloadId, download.Download.Status);

                    if (download.Download.Status == DownloadStatus.Finished)
                    {
                        ActiveDownloads.TryRemove(downloadId, out _);

                        // Check if all downloads are completed and update the torrent
                        var allDownloads = await downloads.GetForTorrent(download.Download.TorrentId);

                        if (allDownloads.All(m => m.Status == DownloadStatus.Finished))
                        {
                            await torrents.UpdateStatus(download.Download.TorrentId, TorrentStatus.Finished);

                            if (download.Download.Torrent.AutoDelete)
                            {
                                await torrents.Delete(download.Download.TorrentId);
                            }
                        }
                    }
                }
            }
        }
    }
}
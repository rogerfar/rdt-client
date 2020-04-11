using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;

namespace RdtClient.Service.Services
{
    public class TaskRunner : IHostedService, IDisposable
    {
        public static readonly ConcurrentDictionary<Guid, DownloadManager> ActiveDownloads = new ConcurrentDictionary<Guid, DownloadManager>();

        private readonly ILogger<TaskRunner> _logger;
        private readonly IServiceProvider _services;

        private Timer _timer;
        
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public TaskRunner(ILogger<TaskRunner> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async void DoWork(Object state)
        {
            // Make sure only 1 process enters the lock
            var obtainLock = await _semaphoreSlim.WaitAsync(100);

            if (!obtainLock)
            {
                return;
            }

            try
            {
                using (var scope = _services.CreateScope())
                {
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
            }
            finally
            {
                _semaphoreSlim.Release(1);
            }
        }

        private async Task ProcessAutoDownloads(IDownloads downloads, ISettings settings, ITorrents torrents)
        {
            var allTorrents = await torrents.Get();

            allTorrents = allTorrents.Where(m => m.Status == TorrentStatus.WaitingForDownload && m.AutoDownload && m.Downloads.Count == 0)
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
                        downloadManager.Download = download;
                        await downloadManager.Start(destinationFolderPath);
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
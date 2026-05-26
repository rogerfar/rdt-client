using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace RdtClient.Service.BackgroundServices;

public class WatchFolderChecker(ILogger<WatchFolderChecker> logger, IServiceProvider serviceProvider, ISettings settings) : BackgroundService
{
    private DateTime _prevCheck = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Startup.Ready)
        {
            await Task.Delay(1000, stoppingToken);
        }

        using var scope = serviceProvider.CreateScope();
        var torrentService = scope.ServiceProvider.GetRequiredService<Torrents>();

        logger.LogInformation("WatchFolderChecker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextCheck = _prevCheck.AddSeconds(Math.Max(settings.Current.Watch.Interval, 10));

                if (DateTime.Now < nextCheck)
                {
                    var delay = nextCheck - DateTime.Now;
                    await Task.Delay(delay, stoppingToken);
                }

                _prevCheck = DateTime.Now;

                if (String.IsNullOrWhiteSpace(settings.Current.Watch.Path))
                {
                    continue;
                }

                var processedStorePath = Path.Combine(settings.Current.Watch.Path, "processed");
                var errorStorePath = Path.Combine(settings.Current.Watch.Path, "error");

                if (!String.IsNullOrWhiteSpace(settings.Current.Watch.ProcessedPath))
                {
                    processedStorePath = settings.Current.Watch.ProcessedPath;
                }

                if (!String.IsNullOrWhiteSpace(settings.Current.Watch.ErrorPath))
                {
                    errorStorePath = settings.Current.Watch.ErrorPath;
                }

                var torrentFiles = Directory.GetFiles(settings.Current.Watch.Path, "*.*", SearchOption.TopDirectoryOnly);

                foreach (var torrentFile in torrentFiles)
                {
                    var fileInfo = new FileInfo(torrentFile);

                    if (fileInfo.Extension != ".magnet" && fileInfo.Extension != ".torrent" && fileInfo.Extension != ".nzb")
                    {
                        continue;
                    }

                    if (IsFileLocked(fileInfo))
                    {
                        continue;
                    }

                    try
                    {
                        logger.Log(LogLevel.Debug, "Processing {torrentFile}", torrentFile);

                        var torrent = new Torrent
                        {
                            DownloadClient = settings.Current.DownloadClient.Client,
                            Category = settings.Current.Watch.Default.Category,
                            HostDownloadAction = settings.Current.Watch.Default.HostDownloadAction,
                            FinishedActionDelay = settings.Current.Watch.Default.FinishedActionDelay,
                            DownloadAction = settings.Current.Watch.Default.OnlyDownloadAvailableFiles
                                ? TorrentDownloadAction.DownloadAvailableFiles
                                : TorrentDownloadAction.DownloadAll,
                            FinishedAction = settings.Current.Watch.Default.FinishedAction,
                            DownloadMinSize = settings.Current.Watch.Default.MinFileSize,
                            IncludeRegex = settings.Current.Watch.Default.IncludeRegex,
                            ExcludeRegex = settings.Current.Watch.Default.ExcludeRegex,
                            TorrentRetryAttempts = settings.Current.Watch.Default.TorrentRetryAttempts,
                            DownloadRetryAttempts = settings.Current.Watch.Default.DownloadRetryAttempts,
                            DeleteOnError = settings.Current.Watch.Default.DeleteOnError,
                            Lifetime = settings.Current.Watch.Default.TorrentLifetime,
                            Priority = settings.Current.Watch.Default.Priority > 0 ? settings.Current.Watch.Default.Priority : null
                        };

                        if (fileInfo.Extension == ".torrent")
                        {
                            var torrentFileContents = await File.ReadAllBytesAsync(torrentFile, stoppingToken);
                            await torrentService.AddFileToDebridQueue(torrentFileContents, torrent);
                        }
                        else if (fileInfo.Extension == ".magnet")
                        {
                            var magnetLink = await File.ReadAllTextAsync(torrentFile, stoppingToken);
                            await torrentService.AddMagnetToDebridQueue(magnetLink, torrent);
                        }
                        else if (fileInfo.Extension == ".nzb")
                        {
                            var nzbFileContents = await File.ReadAllBytesAsync(torrentFile, stoppingToken);
                            await torrentService.AddNzbFileToDebridQueue(nzbFileContents, fileInfo.Name, torrent);
                        }

                        if (!Directory.Exists(processedStorePath))
                        {
                            Directory.CreateDirectory(processedStorePath);
                        }

                        var processedPath = Path.Combine(processedStorePath, fileInfo.Name);

                        if (File.Exists(processedPath))
                        {
                            File.Delete(processedPath);

                            logger.Log(LogLevel.Warning,
                                       "File {torrentFileName} replaced in {processedStorePath} - it already existed and new torrent with same filename was added",
                                       fileInfo.Name,
                                       processedStorePath);
                        }

                        File.Move(torrentFile, processedPath);

                        logger.Log(LogLevel.Debug, "Moved {torrentFile} to {processedPath}", torrentFile, processedPath);
                    }
                    catch
                    {
                        if (!Directory.Exists(errorStorePath))
                        {
                            Directory.CreateDirectory(errorStorePath);
                        }

                        var processedPath = Path.Combine(errorStorePath, fileInfo.Name);

                        if (File.Exists(processedPath))
                        {
                            File.Delete(processedPath);

                            logger.Log(LogLevel.Warning,
                                       "File {torrentFileName} replaced in {errorStorePath} - it already existed and new torrent with same filename was added",
                                       fileInfo.Name,
                                       errorStorePath);
                        }

                        File.Move(torrentFile, processedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error occurred in WatchFolderChecker: {ex.Message}");
            }
        }
    }

    private static Boolean IsFileLocked(FileInfo file)
    {
        try
        {
            using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
        }
        catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32)
        {
            return true;
        }

        return false;
    }
}

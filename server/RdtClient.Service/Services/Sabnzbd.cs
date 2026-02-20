using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Data.Models.Sabnzbd;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services;

public class Sabnzbd(ILogger<Sabnzbd> logger, Torrents torrents, AppSettings appSettings)
{
    public virtual async Task<SabnzbdQueue> GetQueue()
    {
        var allTorrents = await torrents.Get();
        var activeTorrents = allTorrents.Where(t => t.Type == DownloadType.Nzb && t.Completed == null).ToList();

        var queue = new SabnzbdQueue
        {
            NoOfSlots = activeTorrents.Count,
            Slots = activeTorrents.Select((t, index) =>
            {
                var rdProgress = Math.Clamp(t.RdProgress ?? 0.0, 0.0, 100.0) / 100.0;
                Double progress;

                var dlStats = t.Downloads.Select(m => torrents.GetDownloadStats(m.DownloadId)).ToList();
                if (dlStats.Count > 0)
                {
                    var bytesDone = dlStats.Sum(m => m.BytesDone);
                    var bytesTotal = dlStats.Sum(m => m.BytesTotal);
                    var downloadProgress = bytesTotal > 0 ? Math.Clamp((Double)bytesDone / bytesTotal, 0.0, 1.0) : 0;
                    progress = (rdProgress + downloadProgress) / 2.0;
                }
                else
                {
                    progress = rdProgress;
                }

                var timeLeft = "0:00:00";
                var startTime = t.Retry > t.Added ? t.Retry.Value : t.Added;
                var elapsed = DateTimeOffset.UtcNow - startTime;

                if (progress is > 0 and < 1.0)
                {
                    var totalEstimatedTime = TimeSpan.FromTicks((Int64)(elapsed.Ticks / progress));
                    var remaining = totalEstimatedTime - elapsed;
                    if (remaining.TotalSeconds > 0)
                    {
                        timeLeft = $"{(Int32)remaining.TotalHours}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                    }
                }

                return new SabnzbdQueueSlot
                {
                    Index = index,
                    NzoId = t.Hash,
                    Filename = t.RdName ?? t.Hash,
                    Size = FileSizeHelper.FormatSize(dlStats.Sum(d => d.BytesTotal)),
                    SizeLeft = FileSizeHelper.FormatSize(dlStats.Sum(d => d.BytesTotal - d.BytesDone)),
                    Percentage = (progress * 100.0).ToString("0"),

                    Status = t.RdStatus switch
                    {
                        TorrentStatus.Queued => "Queued",
                        TorrentStatus.Processing => "Downloading",
                        TorrentStatus.WaitingForFileSelection => "Downloading",
                        TorrentStatus.Downloading => "Downloading",
                        TorrentStatus.Uploading => "Downloading",
                        TorrentStatus.Finished => "Completed",
                        TorrentStatus.Error => "Failed",
                        _ => "Downloading"
                    },
                    Category = t.Category ?? "*",
                    Priority = "Normal",
                    TimeLeft = timeLeft
                };
            }).ToList()
        };

        return queue;
    }

    public virtual async Task<SabnzbdHistory> GetHistory()
    {
        var allTorrents = await torrents.Get();
        var completedTorrents = allTorrents.Where(t => t.Type == DownloadType.Nzb && t.Completed != null).ToList();

        var savePath = Settings.AppDefaultSavePath;

        var history = new SabnzbdHistory
        {
            NoOfSlots = completedTorrents.Count,
            TotalSlots = completedTorrents.Count,
            Slots = completedTorrents.Select(t =>
            {
                var path = savePath;

                if (!String.IsNullOrWhiteSpace(t.Category))
                {
                    path = Path.Combine(path, t.Category);
                }

                if (!String.IsNullOrWhiteSpace(t.RdName))
                {
                    path = Path.Combine(path, t.RdName);
                }

                return new SabnzbdHistorySlot
                {
                    NzoId = t.Hash,
                    Name = t.RdName ?? t.Hash,
                    Size = FileSizeHelper.FormatSize(t.Downloads.Sum(d => d.BytesTotal)),
                    Status = String.IsNullOrWhiteSpace(t.Error) ? "Completed" : "Failed",
                    Category = t.Category ?? "Default",
                    Path = path
                };
            }).ToList()
        };

        return history;
    }

    public virtual async Task<String> AddFile(Byte[] fileBytes, String? fileName, String? category, Int32? priority)
    {
        logger.LogDebug($"Add file {category}");

        var torrent = new Torrent
        {
            Category = category,
            DownloadClient = Settings.Get.DownloadClient.Client,
            HostDownloadAction = Settings.Get.Integrations.Default.HostDownloadAction,
            FinishedActionDelay = Settings.Get.Integrations.Default.FinishedActionDelay,
            DownloadAction = Settings.Get.Integrations.Default.OnlyDownloadAvailableFiles ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll,
            FinishedAction = TorrentFinishedAction.None,
            DownloadMinSize = Settings.Get.Integrations.Default.MinFileSize,
            IncludeRegex = Settings.Get.Integrations.Default.IncludeRegex,
            ExcludeRegex = Settings.Get.Integrations.Default.ExcludeRegex,
            TorrentRetryAttempts = Settings.Get.Integrations.Default.TorrentRetryAttempts,
            DownloadRetryAttempts = Settings.Get.Integrations.Default.DownloadRetryAttempts,
            DeleteOnError = Settings.Get.Integrations.Default.DeleteOnError,
            Lifetime = Settings.Get.Integrations.Default.TorrentLifetime,
            Priority = (priority ?? Settings.Get.Integrations.Default.Priority) > 0 ? 1 : null
        };

        var result = await torrents.AddNzbFileToDebridQueue(fileBytes, fileName, torrent);
        return result.Hash;
    }

    public virtual async Task<String> AddUrl(String url, String? category, Int32? priority)
    {
        logger.LogDebug($"Add url {category}");

        var torrent = new Torrent
        {
            Category = category,
            DownloadClient = Settings.Get.DownloadClient.Client,
            HostDownloadAction = Settings.Get.Integrations.Default.HostDownloadAction,
            FinishedActionDelay = Settings.Get.Integrations.Default.FinishedActionDelay,
            DownloadAction = Settings.Get.Integrations.Default.OnlyDownloadAvailableFiles ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll,
            FinishedAction = TorrentFinishedAction.None,
            DownloadMinSize = Settings.Get.Integrations.Default.MinFileSize,
            IncludeRegex = Settings.Get.Integrations.Default.IncludeRegex,
            ExcludeRegex = Settings.Get.Integrations.Default.ExcludeRegex,
            TorrentRetryAttempts = Settings.Get.Integrations.Default.TorrentRetryAttempts,
            DownloadRetryAttempts = Settings.Get.Integrations.Default.DownloadRetryAttempts,
            DeleteOnError = Settings.Get.Integrations.Default.DeleteOnError,
            Lifetime = Settings.Get.Integrations.Default.TorrentLifetime,
            Priority = priority ?? (Settings.Get.Integrations.Default.Priority > 0 ? Settings.Get.Integrations.Default.Priority : null)
        };

        var result = await torrents.AddNzbLinkToDebridQueue(url, torrent);
        return result.Hash;
    }

    public virtual async Task Delete(String hash)
    {
        var torrent = await torrents.GetByHash(hash);

        if (torrent == null || torrent.Type != DownloadType.Nzb)
        {
            return;
        }
        
        switch (Settings.Get.Integrations.Default.FinishedAction)
        {
            case TorrentFinishedAction.RemoveAllTorrents:
                logger.LogDebug("Removing nzb from debrid provider and RDT-Client, no files");
                await torrents.Delete(torrent.TorrentId, true, true, true);

                break;
            case TorrentFinishedAction.RemoveRealDebrid:
                logger.LogDebug("Removing nzb from debrid provider, no files");
                await torrents.Delete(torrent.TorrentId, false, true, true);

                break;
            case TorrentFinishedAction.RemoveClient:
                logger.LogDebug("Removing nzb from client, no files");
                await torrents.Delete(torrent.TorrentId, true, false, true);

                break;
            case TorrentFinishedAction.None:
                logger.LogDebug("Not removing nzb files");

                break;
            default:
                logger.LogDebug($"Invalid nzb FinishedAction {torrent.FinishedAction}", torrent);

                break;
        }
    }

    public virtual List<String> GetCategories()
    {
        var categoryList = (Settings.Get.General.Categories ?? "")
                           .Split(",", StringSplitOptions.RemoveEmptyEntries)
                           .Select(m => m.Trim())
                           .Where(m => m != "*")
                           .Distinct(StringComparer.CurrentCultureIgnoreCase)
                           .ToList();

        categoryList.Insert(0, "*");

        return categoryList;
    }

    public virtual SabnzbdConfig GetConfig()
    {
        var savePath = Settings.AppDefaultSavePath;

        var categoryList = GetCategories();

        var categories = categoryList.Select((c, i) => new SabnzbdCategory
        {
            Name = c,
            Order = i,
            Dir = c == "*" ? "" : Path.Combine(savePath, c)
        }).ToList();

        var config = new SabnzbdConfig
        {
            Misc = new()
            {
                CompleteDir = savePath,
                DownloadDir = savePath,
                Port = appSettings.Port.ToString(),
                Version = "4.4.0"
            },
            Categories = categories
        };

        return config;
    }
}

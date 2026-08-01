using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Data.Models.Sabnzbd;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services;

public class Sabnzbd(ILogger<Sabnzbd> logger, Torrents torrents, AppSettings appSettings, ISettings settings)
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

                                      var dlBytesTotal = dlStats.Sum(m => m.BytesTotal);
                                      var dlBytesDone = dlStats.Sum(m => m.BytesDone);

                                      if (dlStats.Count > 0)
                                      {
                                          var downloadProgress = dlBytesTotal > 0 ? Math.Clamp((Double)dlBytesDone / dlBytesTotal, 0.0, 1.0) : 0;
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

                                      var mbBytes = dlBytesTotal > 0 ? dlBytesTotal : (t.RdSize ?? 0);

                                      var mbLeftBytes = dlBytesTotal > 0
                                          ? dlBytesTotal - dlBytesDone
                                          : (t.RdSize.HasValue
                                              ? (Int64)(t.RdSize.Value * (1.0 - rdProgress))
                                              : 0);

                                      return new SabnzbdQueueSlot
                                      {
                                          Index = index,
                                          NzoId = t.Hash,
                                          Filename = t.RdName ?? t.Hash,
                                          Size = dlBytesTotal > 0
                                              ? FileSizeHelper.FormatSize(dlBytesTotal)
                                              : FileSizeHelper.FormatSize(t.RdSize),
                                          SizeLeft = dlBytesTotal > 0
                                              ? FileSizeHelper.FormatSize(dlBytesTotal - dlBytesDone)
                                              : FileSizeHelper.FormatSize(t.RdSize.HasValue ? (Int64)(t.RdSize.Value * (1.0 - rdProgress)) : null),
                                          Mb = (mbBytes / 1048576.0).ToString("0.00"),
                                          MbLeft = (Math.Max(mbLeftBytes, 0) / 1048576.0).ToString("0.00"),
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
                                  })
                                  .ToList()
        };

        queue.Mb = queue.Slots.Sum(s => Double.Parse(s.Mb)).ToString("0.00");
        queue.MbLeft = queue.Slots.Sum(s => Double.Parse(s.MbLeft)).ToString("0.00");

        return queue;
    }

    public virtual async Task<SabnzbdHistory> GetHistory()
    {
        var allTorrents = await torrents.Get();
        var completedTorrents = allTorrents.Where(t => t.Type == DownloadType.Nzb && t.Completed != null).ToList();

        var savePath = settings.DefaultSavePath;

        var history = new SabnzbdHistory
        {
            NoOfSlots = completedTorrents.Count,
            TotalSlots = completedTorrents.Count,
            Slots = completedTorrents.Select(t =>
                                     {
                                         var path = PathHelper.CombineWithCategory(savePath, t.Category);

                                         if (!String.IsNullOrWhiteSpace(t.RdName))
                                         {
                                             path = Path.Combine(path, t.RdName);
                                         }

                                         var historyBytesTotal = t.Downloads.Sum(d => d.BytesTotal);
                                         var totalBytes = historyBytesTotal > 0 ? historyBytesTotal : (t.RdSize ?? 0);

                                         return new SabnzbdHistorySlot
                                         {
                                             NzoId = t.Hash,
                                             Name = t.RdName ?? t.Hash,
                                             Size = FileSizeHelper.FormatSize(totalBytes),
                                             Bytes = totalBytes,
                                             Downloaded = String.IsNullOrWhiteSpace(t.Error) ? totalBytes : 0,
                                             Status = String.IsNullOrWhiteSpace(t.Error) ? "Completed" : "Failed",
                                             Category = t.Category ?? "Default",
                                             Path = path
                                         };
                                     })
                                     .ToList()
        };

        return history;
    }

    public virtual async Task<String> AddFile(Byte[] fileBytes, String? fileName, String? category, Int32? priority)
    {
        logger.LogDebug($"Add file {category}");

        var torrent = new Torrent
        {
            Category = category,
            DownloadClient = settings.Current.DownloadClient.Client,
            HostDownloadAction = settings.Current.Integrations.Default.HostDownloadAction,
            FinishedActionDelay = settings.Current.Integrations.Default.FinishedActionDelay,
            DownloadAction = settings.Current.Integrations.Default.OnlyDownloadAvailableFiles ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll,
            FinishedAction = TorrentFinishedAction.None,
            DownloadMinSize = settings.Current.Integrations.Default.MinFileSize,
            IncludeRegex = settings.Current.Integrations.Default.IncludeRegex,
            ExcludeRegex = settings.Current.Integrations.Default.ExcludeRegex,
            TorrentRetryAttempts = settings.Current.Integrations.Default.TorrentRetryAttempts,
            DownloadRetryAttempts = settings.Current.Integrations.Default.DownloadRetryAttempts,
            DeleteOnError = settings.Current.Integrations.Default.DeleteOnError,
            Lifetime = settings.Current.Integrations.Default.TorrentLifetime,
            Priority = (priority ?? settings.Current.Integrations.Default.Priority) > 0 ? 1 : null
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
            DownloadClient = settings.Current.DownloadClient.Client,
            HostDownloadAction = settings.Current.Integrations.Default.HostDownloadAction,
            FinishedActionDelay = settings.Current.Integrations.Default.FinishedActionDelay,
            DownloadAction = settings.Current.Integrations.Default.OnlyDownloadAvailableFiles ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll,
            FinishedAction = TorrentFinishedAction.None,
            DownloadMinSize = settings.Current.Integrations.Default.MinFileSize,
            IncludeRegex = settings.Current.Integrations.Default.IncludeRegex,
            ExcludeRegex = settings.Current.Integrations.Default.ExcludeRegex,
            TorrentRetryAttempts = settings.Current.Integrations.Default.TorrentRetryAttempts,
            DownloadRetryAttempts = settings.Current.Integrations.Default.DownloadRetryAttempts,
            DeleteOnError = settings.Current.Integrations.Default.DeleteOnError,
            Lifetime = settings.Current.Integrations.Default.TorrentLifetime,
            Priority = priority ?? (settings.Current.Integrations.Default.Priority > 0 ? settings.Current.Integrations.Default.Priority : null)
        };

        var result = await torrents.AddNzbLinkToDebridQueue(url, torrent);

        return result.Hash;
    }

    public virtual async Task Delete(String hash, Boolean deleteFiles = false)
    {
        var torrent = await torrents.GetByHash(hash);

        if (torrent == null || torrent.Type != DownloadType.Nzb)
        {
            return;
        }

        switch (settings.Current.Integrations.Default.FinishedAction)
        {
            case TorrentFinishedAction.RemoveAllTorrents:
                logger.LogDebug("Removing nzb from debrid provider and RDT-Client, {Files}", deleteFiles ? "with files" : "no files");
                await torrents.Delete(torrent.TorrentId, true, true, deleteFiles);

                break;
            case TorrentFinishedAction.RemoveRealDebrid:
                logger.LogDebug("Removing nzb from debrid provider, {Files}", deleteFiles ? "with files" : "no files");
                await torrents.Delete(torrent.TorrentId, false, true, deleteFiles);

                break;
            case TorrentFinishedAction.RemoveClient:
                logger.LogDebug("Removing nzb from client, {Files}", deleteFiles ? "with files" : "no files");
                await torrents.Delete(torrent.TorrentId, true, false, deleteFiles);

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
        var categoryList = (settings.Current.General.Categories ?? "")
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
        var savePath = settings.DefaultSavePath;

        var categoryList = GetCategories();

        var categories = categoryList.Select((c, i) => new SabnzbdCategory
                                     {
                                         Name = c,
                                         Order = i,
                                         Dir = c == "*" ? "" : Path.Combine(savePath, c)
                                     })
                                     .ToList();

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

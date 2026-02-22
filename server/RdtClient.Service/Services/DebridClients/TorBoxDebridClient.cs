using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.DebridClient;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
using TorBoxNET;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services.DebridClients;

public class TorBoxDebridClient(ILogger<TorBoxDebridClient> logger, IHttpClientFactory httpClientFactory, IDownloadableFileFilter fileFilter) : IDebridClient
{
    private TimeSpan? _offset;
    protected virtual ITorBoxNetClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new("TorBox API Key not set in the settings");
            }

            var httpClient = httpClientFactory.CreateClient(DiConfig.TORBOX_CLIENT); 
            var torBoxNetClient = new TorBoxNetClient(null, httpClient, 1);
            torBoxNetClient.UseApiAuthentication(apiKey);

            // Get the server time to fix up the timezones on results
            if (_offset == null)
            {
                var serverTime = DateTimeOffset.UtcNow;
                _offset = serverTime.Offset;
            }

            return torBoxNetClient;
        }
        catch (AggregateException ae)
        {
            foreach (var inner in ae.InnerExceptions)
            {
                logger.LogError(inner, $"The connection to TorBox has failed: {inner.Message}");
            }

            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogError(ex, $"The connection to TorBox has timed out: {ex.Message}");

            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"The connection to TorBox has timed out: {ex.Message}");

            throw;
        }
    }

    protected virtual async Task<IEnumerable<TorrentInfoResult>?> GetCurrentTorrents()
    {
        return await GetClient().Torrents.GetCurrentAsync(true);
    }

    protected virtual async Task<IEnumerable<TorrentInfoResult>?> GetQueuedTorrents()
    {
        return await GetClient().Torrents.GetQueuedAsync(true);
    }

    protected virtual async Task<IEnumerable<UsenetInfoResult>?> GetCurrentUsenet()
    {
        return await GetClient().Usenet.GetCurrentAsync(true);
    }

    protected virtual async Task<IEnumerable<UsenetInfoResult>?> GetQueuedUsenet()
    {
        return await GetClient().Usenet.GetQueuedAsync(true);
    }

    protected virtual async Task<Response<List<AvailableTorrent?>>> GetTorrentAvailability(String hash)
    {
        return await GetClient().Torrents.GetAvailabilityAsync(hash, listFiles: true);
    }

    protected virtual async Task<Response<List<AvailableUsenet?>>> GetUsenetAvailability(String hash)
    {
        return await GetClient().Usenet.GetAvailabilityAsync(hash, listFiles: true);
    }

    private DebridClientTorrent Map(TorrentInfoResult torrent)
    {
        return new()
        {
            Id = torrent.Hash,
            Filename = torrent.Name,
            OriginalFilename = torrent.Name,
            Hash = torrent.Hash,
            Bytes = torrent.Size,
            OriginalBytes = torrent.Size,
            Host = torrent.DownloadPresent.ToString(),
            Split = 0,
            Progress = (Int64)(torrent.Progress * 100.0),
            Status = torrent.DownloadState,
            Type = DownloadType.Torrent,
            Added = ChangeTimeZone(torrent.CreatedAt)!.Value,
            Files = (torrent.Files ?? []).Select(m => new DebridClientFile
            {
                Path = String.Join("/", m.Name.Split('/').Skip(1)),
                Bytes = m.Size,
                Id = m.Id,
                Selected = true
            }).ToList(),
            Links = [],
            Ended = ChangeTimeZone(torrent.UpdatedAt),
            Speed = torrent.DownloadSpeed,
            Seeders = torrent.Seeds,
        };
    }

    private DebridClientTorrent Map(UsenetInfoResult usenet)
    {
        return new()
        {
            Id = usenet.Hash,
            Filename = usenet.Name,
            OriginalFilename = usenet.Name,
            Hash = usenet.Hash,
            Bytes = usenet.Size,
            OriginalBytes = usenet.Size,
            Host = usenet.DownloadPresent.ToString(),
            Split = 0,
            Progress = (Int64)(usenet.Progress * 100.0),
            Status = usenet.DownloadState,
            Type = DownloadType.Nzb,
            Added = ChangeTimeZone(usenet.CreatedAt)!.Value,
            Files = (usenet.Files ?? []).Select(m => new DebridClientFile
            {
                Path = String.Join("/", m.Name.Split('/').Skip(1)),
                Bytes = m.Size,
                Id = m.Id,
                Selected = true
            }).ToList(),
            Links = [],
            Ended = ChangeTimeZone(usenet.UpdatedAt),
            Speed = usenet.DownloadSpeed,
            Seeders = 0,
        };
    }

    public async Task<IList<DebridClientTorrent>> GetDownloads()
    {
        var results = new List<DebridClientTorrent>();

        var currentTorrents = await GetCurrentTorrents();
        if (currentTorrents != null)
        {
            results.AddRange(currentTorrents.Select(Map));
        }

        var queuedTorrents = await GetQueuedTorrents();
        if (queuedTorrents != null)
        {
            results.AddRange(queuedTorrents.Select(Map));
        }

        var currentNzbs = await GetCurrentUsenet();
        if (currentNzbs != null)
        {
            results.AddRange(currentNzbs.Select(Map));
        }

        var queuedNzbs = await GetQueuedUsenet();
        if (queuedNzbs != null)
        {
            results.AddRange(queuedNzbs.Select(Map));
        }

        return results;
    }

    public async Task<DebridClientUser> GetUser()
    {
        var user = await GetClient().User.GetAsync(false);

        return new()
        {
            Username = user.Data!.Email,
            Expiration = user.Data!.Plan != 0 ? user.Data!.PremiumExpiresAt!.Value : null
        };
    }

    private async Task<String> HandleAddTorrentErrors(Func<Boolean, Task<String>> action)
    {
        try
        {
            return await action(false);
        }
        catch (RateLimitException)
        {
            throw;
        }
        catch (Exception ex) when (ex.InnerException is RateLimitException rateLimitException)
        {
            throw rateLimitException;
        }
        catch (TorBoxException ex) when (ex.Error.Equals("active_limit", StringComparison.OrdinalIgnoreCase))
        {
            throw new RateLimitException(ex.Message, TimeSpan.FromMinutes(2));
        }
        catch (Exception ex) when (ex.Message.Contains("slow_down", StringComparison.OrdinalIgnoreCase))
        {
            throw new RateLimitException(ex.Message, TimeSpan.FromMinutes(2));
        }
    }

    private async Task<String> HandleAddUsenetErrors(Func<Boolean, Task<String>> action)
    {
        try
        {
            return await action(false);
        }
        catch (RateLimitException)
        {
            throw;
        }
        catch (Exception ex) when (ex.InnerException is RateLimitException rateLimitException)
        {
            throw rateLimitException;
        }
        catch (TorBoxException ex) when (ex.Error.Equals("active_limit", StringComparison.OrdinalIgnoreCase))
        {
            throw new RateLimitException(ex.Message, TimeSpan.FromMinutes(2));
        }
        catch (Exception ex) when (ex.Message.Contains("slow_down", StringComparison.OrdinalIgnoreCase))
        {
            throw new RateLimitException(ex.Message, TimeSpan.FromMinutes(2));
        }
    }

    public async Task<String> AddTorrentMagnet(String magnetLink)
    {
        return await HandleAddTorrentErrors(async asQueued =>
        {
            var user = await GetClient().User.GetAsync(true);
            var result = await GetClient().Torrents.AddMagnetAsync(magnetLink, user.Data?.Settings?.SeedTorrents ?? 3, as_queued: asQueued);
            return result.Data!.Hash!;
        });
    }

    public async Task<String> AddTorrentFile(Byte[] bytes)
    {
        return await HandleAddTorrentErrors(async asQueued =>
        {
            var user = await GetClient().User.GetAsync(true);
            var result = await GetClient().Torrents.AddFileAsync(bytes, user.Data?.Settings?.SeedTorrents ?? 3, as_queued: asQueued);
            return result.Data!.Hash!;
        });
    }

    public async Task<String> AddNzbLink(String nzbLink)
    {
        return await HandleAddUsenetErrors(async asQueued =>
        {
            var result = await GetClient().Usenet.AddLinkAsync(nzbLink, as_queued: asQueued);
            return result.Data!.Hash!;
        });
    }

    public virtual async Task<String> AddNzbFile(Byte[] bytes, String? name)
    {
        return await HandleAddUsenetErrors(async asQueued =>
        {
            var result = await GetClient().Usenet.AddFileAsync(bytes, name: name, as_queued: asQueued);
            return result.Data!.Hash!;
        });
    }

    public async Task<IList<DebridClientAvailableFile>> GetAvailableFiles(String hash)
    {
        var availability = await GetTorrentAvailability(hash);

        if (availability.Data != null && availability.Data.Count > 0)
        {
            return (availability.Data[0]?.Files ?? []).Select(file => new DebridClientAvailableFile
            {
                Filename = file.Name,
                Filesize = file.Size
            }).ToList();
        }

        var usenetAvailability = await GetUsenetAvailability(hash);

        if (usenetAvailability.Data != null && usenetAvailability.Data.Count > 0)
        {
            return (usenetAvailability.Data[0]?.Files ?? []).Select(file => new DebridClientAvailableFile
            {
                Filename = file.Name,
                Filesize = file.Size
            }).ToList();
        }

        return [];
    }

    /// <inheritdoc />
    public Task<Int32?> SelectFiles(Torrent torrent)
    {
        return Task.FromResult<Int32?>(torrent.Files.Count);
    }

    public async Task Delete(Torrent torrent)
    {
        if (torrent.RdId == null)
        {
            return;
        }

        if (torrent.Type == DownloadType.Nzb)
        {
            await GetClient().Usenet.ControlAsync(torrent.RdId, "delete");
        }
        else
        {
            await GetClient().Torrents.ControlAsync(torrent.RdId, "delete");
        }
    }

    public async Task<String> Unrestrict(Torrent torrent, String link)
    {
        if (String.IsNullOrWhiteSpace(link))
        {
            throw new ArgumentException("Link cannot be null or empty", nameof(link));
        }

        var segments = link.Split('/');
        if (segments is not [_, _, _, _, var torrentIdStr, var fileIdStrOrZip])
        {
            throw new ArgumentException($"Invalid link format: {link}", nameof(link));
        }

        var zipped = fileIdStrOrZip == "zip";
        var fileIdStr = zipped ? "0" : fileIdStrOrZip;

        if (!Int32.TryParse(torrentIdStr, out var torrentId))
        {
            throw new ArgumentException($"Invalid torrent ID in link segment 4: {torrentIdStr}", nameof(link));
        }

        if (!Int32.TryParse(fileIdStr, out var fileId))
        {
            throw new ArgumentException($"Invalid file ID in link segment 5: {fileId}", nameof(link));
        }

        Response<String> result;

        if (torrent.Type == DownloadType.Nzb)
        {
            result = await GetClient().Usenet.RequestDownloadAsync(torrentId, fileId, zipped);
        }
        else
        {
            result = await GetClient().Torrents.RequestDownloadAsync(torrentId, fileId, zipped);
        }

        if (result.Error != null)
        {
            throw new($"Unrestrict returned an invalid download: {result.Error}");
        }

        return result.Data!;
    }

    public async Task<Torrent> UpdateData(Torrent torrent, DebridClientTorrent? torrentClientTorrent)
    {
        try
        {
            if (torrent.RdId == null)
            {
                return torrent;
            }

            var rdTorrent = torrentClientTorrent ?? await GetInfo(torrent.RdId, torrent.Type) ?? throw new($"Resource not found");

            if (!String.IsNullOrWhiteSpace(rdTorrent.Filename))
            {
                torrent.RdName = rdTorrent.Filename;
            }

            if (!String.IsNullOrWhiteSpace(rdTorrent.OriginalFilename))
            {
                torrent.RdName = rdTorrent.OriginalFilename;
            }

            if (rdTorrent.Bytes > 0)
            {
                torrent.RdSize = rdTorrent.Bytes;
            }
            else if (rdTorrent.OriginalBytes > 0)
            {
                torrent.RdSize = rdTorrent.OriginalBytes;
            }

            if (rdTorrent.Files != null)
            {
                torrent.RdFiles = JsonConvert.SerializeObject(rdTorrent.Files);
            }

            torrent.ClientKind = Provider.TorBox;
            torrent.RdHost = rdTorrent.Host;
            torrent.RdSplit = rdTorrent.Split;
            torrent.RdProgress = rdTorrent.Progress;
            torrent.RdAdded = rdTorrent.Added;
            torrent.RdEnded = rdTorrent.Ended;
            torrent.RdSpeed = rdTorrent.Speed;
            torrent.RdSeeders = rdTorrent.Seeders;
            torrent.RdStatusRaw = rdTorrent.Status;

            if (rdTorrent.Host == "True")
            {
                torrent.RdStatus = TorrentStatus.Finished;
            }
            else
            {
                logger.LogTrace("Updating status for {TorrentName} from {OldStatus} to {NewStatus}", torrent.RdName, torrent.RdStatus, rdTorrent.Status);
                torrent.RdStatus = rdTorrent.Status switch
                {
                    "allocating" => TorrentStatus.Processing,
                    "metaDL" => TorrentStatus.Processing,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("queued", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Processing,
                    "completed" => TorrentStatus.Downloading,
                    "processing" => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("paused", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("stalled", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("downloading", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("checking", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("waiting", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("direct unpack", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("repair", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("verifying", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Downloading,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("uploading", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Uploading,
                    "cached" => TorrentStatus.Finished,
                    "missing" => TorrentStatus.Error, // NZB missing parts
                    "error" => TorrentStatus.Error,
                    _ when rdTorrent.Status != null && rdTorrent.Status.StartsWith("failed", StringComparison.OrdinalIgnoreCase) => TorrentStatus.Error,
                    _ => LogUnmappedStatus(rdTorrent.Status, torrent)
                };
            }
        }
        catch (Exception ex)
        {
            if (ex.Message == "Resource not found")
            {
                torrent.RdStatusRaw = "deleted";
            }
            else
            {
                throw;
            }
        }

        return torrent;
    }

    public async Task<IList<DownloadInfo>?> GetDownloadInfos(Torrent torrent)
    {
        Int32? id;

        if (torrent.Type == DownloadType.Nzb)
        {
            if (torrent.RdId == null)
            {
                return null;
            }

            var usenets = await GetClient().Usenet.GetCurrentAsync(true);
            var usenet = usenets?.FirstOrDefault(m => m.Hash == torrent.RdId);
            id = (Int32?)usenet?.Id;
        }
        else
        {
            var torrentId = await GetClient().Torrents.GetHashInfoAsync(torrent.Hash, skipCache: true);
            id = torrentId?.Id;
        }

        if (id == null)
        {
            return null;
        }
        var downloadableFiles = torrent.Files.Where(file => fileFilter.IsDownloadable(torrent, file.Path, file.Bytes)).ToList();

        if (downloadableFiles.Count == torrent.Files.Count && torrent.DownloadClient != Data.Enums.DownloadClient.Symlink && Settings.Get.Provider.PreferZippedDownloads)
        {
            logger.LogDebug("Downloading files from TorBox as a zip.");

            return
            [
                new()
                {
                    RestrictedLink = $"https://torbox.app/fakedl/{id}/zip",
                    FileName = $"{torrent.RdName}.zip"
                }
            ];
        }

        logger.LogDebug("Downloading files from TorBox individually.");

        return downloadableFiles.Select(file => new DownloadInfo
                                {
                                    RestrictedLink = $"https://torbox.app/fakedl/{id}/{file.Id}",
                                    FileName = Path.GetFileName(file.Path)
                                })
                                .ToList();
    }

    /// <inheritdoc />
    public Task<String> GetFileName(Download download)
    {
        // FileName is set in GetDownlaadInfos
        Debug.Assert(download.FileName != null);

        return Task.FromResult(download.FileName);
    }


    private DateTimeOffset? ChangeTimeZone(DateTimeOffset? dateTimeOffset)
    {
        if (_offset == null)
        {
            return dateTimeOffset;
        }

        return dateTimeOffset?.Subtract(_offset.Value).ToOffset(_offset.Value);
    }

    private async Task<DebridClientTorrent?> GetInfo(String id, DownloadType type)
    {
        if (type == DownloadType.Nzb)
        {
            var usenet = await GetClient().Usenet.GetHashInfoAsync(id, skipCache: true);
            if (usenet != null)
            {
                return Map(usenet);
            }
        }
        else
        {
            var result = await GetClient().Torrents.GetHashInfoAsync(id, skipCache: true);

            if (result != null)
            {
                return Map(result);
            }
        }

        return null;
    }

    public static void MoveHashDirContents(String extractPath, Torrent torrent)
    {
        var hashDir = Path.Combine(extractPath, torrent.Hash);

        if (Directory.Exists(hashDir))
        {
            var innerFolder = Directory.GetDirectories(hashDir)[0];

            var moveDir = extractPath;
            if (!extractPath.EndsWith(torrent.RdName!))
            {
                moveDir = hashDir;
            }

            foreach (var file in Directory.GetFiles(innerFolder))
            {
                var destFile = Path.Combine(moveDir, Path.GetFileName(file));
                File.Move(file, destFile);
            }

            foreach (var dir in Directory.GetDirectories(innerFolder))
            {
                var destDir = Path.Combine(moveDir, Path.GetFileName(dir));
                Directory.Move(dir, destDir);
            }

            if (!extractPath.Contains(torrent.RdName!))
            {
                Directory.Delete(innerFolder, true);
            }
            else
            {
                Directory.Delete(hashDir, true);
            }
        }
    }

    private TorrentStatus LogUnmappedStatus(String? status, Torrent torrent)
    {
        if (!String.IsNullOrWhiteSpace(status))
        {
            logger.LogInformation("TorBoxDebridClient encountered an unmapped status: {Status} for torrent {TorrentName}", status, torrent.RdName);
        }

        return torrent.RdStatus ?? TorrentStatus.Processing;
    }

    private void Log(String message, Torrent? torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        logger.LogDebug(message);
    }
}

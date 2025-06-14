using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TorBoxNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services.TorrentClients;

public class TorBoxTorrentClient(ILogger<TorBoxTorrentClient> logger, IHttpClientFactory httpClientFactory, IDownloadableFileFilter fileFilter) : ITorrentClient
{
    private TimeSpan? _offset;
    private TorBoxNetClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new("TorBox API Key not set in the settings");
            }

            var httpClient = httpClientFactory.CreateClient(); 
            httpClient.Timeout = TimeSpan.FromSeconds(Settings.Get.Provider.Timeout);

            var torBoxNetClient = new TorBoxNetClient(null, httpClient, 5);
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

    private TorrentClientTorrent Map(TorrentInfoResult torrent)
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
            Added = ChangeTimeZone(torrent.CreatedAt)!.Value,
            Files = (torrent.Files ?? []).Select(m => new TorrentClientFile
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

    public async Task<IList<TorrentClientTorrent>> GetTorrents()
    {
        var torrents = new List<TorrentInfoResult>();

        var currentTorrents = await GetClient().Torrents.GetCurrentAsync(true);
        if (currentTorrents != null)
        {
            torrents.AddRange(currentTorrents);
        }

        var queuedTorrents = await GetClient().Torrents.GetQueuedAsync(true);
        if (queuedTorrents != null)
        {
            torrents.AddRange(queuedTorrents);
        }

        return torrents.Select(Map).ToList();
    }

    public async Task<TorrentClientUser> GetUser()
    {
        var user = await GetClient().User.GetAsync(false);

        return new()
        {
            Username = user.Data!.Email,
            Expiration = user.Data!.Plan != 0 ? user.Data!.PremiumExpiresAt!.Value : null
        };
    }

    public async Task<String> AddMagnet(String magnetLink)
    {
        var user = await GetClient().User.GetAsync(true);

        var result = await GetClient().Torrents.AddMagnetAsync(magnetLink, user.Data?.Settings?.SeedTorrents ?? 3, false);

        if (result.Error == "ACTIVE_LIMIT")
        {
            var magnetLinkInfo = MonoTorrent.MagnetLink.Parse(magnetLink);
            return magnetLinkInfo.InfoHashes.V1!.ToHex().ToLowerInvariant();
        }

        return result.Data!.Hash!;
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        var user = await GetClient().User.GetAsync(true);

        var result = await GetClient().Torrents.AddFileAsync(bytes, user.Data?.Settings?.SeedTorrents ?? 3);
        if (result.Error == "ACTIVE_LIMIT")
        {
            using var stream = new MemoryStream(bytes);

            var torrent = await MonoTorrent.Torrent.LoadAsync(stream);
            return torrent.InfoHashes.V1!.ToHex().ToLowerInvariant();
        }

        return result.Data!.Hash!;
    }

    public async Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        var availability = await GetClient().Torrents.GetAvailabilityAsync(hash, listFiles: true);

        if (availability.Data != null && availability.Data.Count > 0)
        {
            return (availability.Data[0]?.Files ?? []).Select(file => new TorrentClientAvailableFile
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

    public async Task Delete(String torrentId)
    {
        await GetClient().Torrents.ControlAsync(torrentId, "delete");
    }

    public async Task<String> Unrestrict(String link)
    {
        var segments = link.Split('/');

        var zipped = segments[5] == "zip";
        var fileId = zipped ? "0" : segments[5];

        var result = await GetClient().Torrents.RequestDownloadAsync(Convert.ToInt32(segments[4]), Convert.ToInt32(fileId), zipped);

        if (result.Error != null)
        {
            throw new("Unrestrict returned an invalid download");
        }

        return result.Data!;
    }

    public async Task<Torrent> UpdateData(Torrent torrent, TorrentClientTorrent? torrentClientTorrent)
    {
        try
        {
            if (torrent.RdId == null)
            {
                return torrent;
            }

            var rdTorrent = torrentClientTorrent ?? await GetInfo(torrent.Hash) ?? throw new($"Resource not found");

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
                torrent.RdStatus = rdTorrent.Status switch
                {
                    "queued" => TorrentStatus.Processing,
                    "metaDL" => TorrentStatus.Processing,
                    "checking" => TorrentStatus.Processing,
                    "checkingResumeData" => TorrentStatus.Processing,
                    "paused" => TorrentStatus.Downloading,
                    "stalledDL" => TorrentStatus.Downloading,
                    "downloading" => TorrentStatus.Downloading,
                    "completed" => TorrentStatus.Downloading,
                    "uploading" => TorrentStatus.Downloading,
                    "uploading (no peers)" => TorrentStatus.Downloading,
                    "stalled" => TorrentStatus.Downloading,
                    "stalled (no seeds)" => TorrentStatus.Downloading,
                    "processing" => TorrentStatus.Downloading,
                    "cached" => TorrentStatus.Finished,
                    _ => TorrentStatus.Error
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
        var torrentId = await GetClient().Torrents.GetHashInfoAsync(torrent.Hash, skipCache: true);
        var downloadableFiles = torrent.Files.Where(file => fileFilter.IsDownloadable(torrent, file.Path, file.Bytes)).ToList();

        if (downloadableFiles.Count == torrent.Files.Count && torrent.DownloadClient != Data.Enums.DownloadClient.Symlink && Settings.Get.Provider.PreferZippedDownloads)
        {
            logger.LogDebug("Downloading files from TorBox as a zip.");

            return
            [
                new()
                {
                    RestrictedLink = $"https://torbox.app/fakedl/{torrentId?.Id}/zip",
                    FileName = $"{torrent.RdName}.zip"
                }
            ];
        }

        logger.LogDebug("Downloading files from TorBox individually.");

        return downloadableFiles.Select(file => new DownloadInfo
                                {
                                    RestrictedLink = $"https://torbox.app/fakedl/{torrentId?.Id}/{file.Id}",
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

    private async Task<TorrentClientTorrent> GetInfo(String torrentHash)
    {
        var result = await GetClient().Torrents.GetHashInfoAsync(torrentHash, skipCache: true);

        return Map(result!);
    }

    public static void MoveHashDirContents(String extractPath, Torrent _torrent)
    {
        var hashDir = Path.Combine(extractPath, _torrent.Hash);

        if (Directory.Exists(hashDir))
        {
            var innerFolder = Directory.GetDirectories(hashDir)[0];

            var moveDir = extractPath;
            if (!extractPath.EndsWith(_torrent.RdName!))
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

            if (!extractPath.Contains(_torrent.RdName!))
            {
                Directory.Delete(innerFolder, true);
            }
            else
            {
                Directory.Delete(hashDir, true);
            }
        }
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

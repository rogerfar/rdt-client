using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TorBoxNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using System.Web;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Services.TorrentClients;

public class TorBoxTorrentClient(ILogger<TorBoxTorrentClient> logger, IHttpClientFactory httpClientFactory) : ITorrentClient
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
        else
        {
            return result.Data!.Hash!;
        }
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

    public Task SelectFiles(Torrent torrent)
    {
        return Task.CompletedTask;
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

            var rdTorrent = await GetInfo(torrent.Hash) ?? throw new($"Resource not found");

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

    public async Task<IList<String>?> GetDownloadLinks(Torrent torrent)
    {
        var files = new List<String>();

        var torrentId = await GetClient().Torrents.GetHashInfoAsync(torrent.Hash, skipCache: true);

        foreach (var file in torrent.Files)
        {
            var newFile = $"https://torbox.app/fakedl/{torrentId?.Id}/{file.Id}";
            files.Add(newFile);
        }

        return files;
    }

    public async Task<String> GetFileName(String downloadUrl)
    {
        if (String.IsNullOrWhiteSpace(downloadUrl))
        {
            return "";
        }

        var uri = new Uri(downloadUrl);

        using (HttpClient client = new())
        {
            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = await client.SendAsync(request);
            if (response.Content.Headers.ContentDisposition != null)
            {
                var fileName = response.Content.Headers.ContentDisposition.FileName;
                if (!String.IsNullOrWhiteSpace(fileName))
                {
                    return fileName.Trim('"');
                }
            }
        }

        return HttpUtility.UrlDecode(uri.Segments.Last());
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
}

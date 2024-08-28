using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TorBoxNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using Microsoft.AspNetCore.Routing.Constraints;

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

            var rdtNetClient = new TorBoxNetClient(null, httpClient, 5);
            rdtNetClient.UseApiAuthentication(apiKey);

            // Get the server time to fix up the timezones on results
            if (_offset == null)
            {
                var serverTime = rdtNetClient.Api.GetIsoTimeAsync();
                _offset = serverTime.Offset;
            }

            return rdtNetClient;
        }
        catch (AggregateException ae)
        {
            foreach (var inner in ae.InnerExceptions)
            {
                logger.LogError(inner, $"The connection to RealDebrid has failed: {inner.Message}");
            }

            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogError(ex, $"The connection to RealDebrid has timed out: {ex.Message}");

            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"The connection to RealDebrid has timed out: {ex.Message}");

            throw;
        }
    }

    private TorrentClientTorrent Map(Torrent torrent)
    {
        return new()
        {
            Id = torrent.Id.ToString(),
            Filename = torrent.Name,
            OriginalFilename = torrent.Name,
            Hash = torrent.Hash,
            Bytes = torrent.Size,
            OriginalBytes = torrent.Size,
            Host = torrent.DownloadPresent.ToString(),
            Split = 0,
            Progress = (Int64)((torrent.Progress) * 100.0),
            Status = torrent.DownloadState,
            Added = ChangeTimeZone(torrent.CreatedAt)!.Value,
            Files = (torrent.Files ?? []).Select(m => new TorrentClientFile
            {
                Path = string.Join("/", m.Name.Split('/').Skip(1)),
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
        var torrents = new List<Torrent>();

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

        return torrents!.Select(Map).ToList();
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
        var result = await GetClient().Torrents.AddMagnetAsync(magnetLink, seeding: 3);

        return result.Data?.Hash?.ToString()!;
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        var result = await GetClient().Torrents.AddFileAsync(bytes, seeding: 3);

        return result.Data?.Hash?.ToString()!;
    }

    public Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        var result = new List<TorrentClientAvailableFile>();
        return Task.FromResult<IList<TorrentClientAvailableFile>>(result);
    }

    public Task SelectFiles(Data.Models.Data.Torrent torrent)
    {
        return Task.CompletedTask;
    }

    public async Task Delete(String torrentId)
    {
        await GetClient().Torrents.ControlAsync(Convert.ToInt32(torrentId), "delete");
    }

    public async Task<String> Unrestrict(String link)
    {
        var torrentFile = new List<string>(link.Split('/'));
        var torrentID = torrentFile[4];
        var fileID = torrentFile[5];
        var result = await GetClient().Unrestrict.LinkAsync(torrentID, fileID);

        if (result.Error != null)
        {
            throw new($"Unrestrict returned an invalid download");
        }

        return result.Data!;
    }

    public async Task<Data.Models.Data.Torrent> UpdateData(Data.Models.Data.Torrent torrent, TorrentClientTorrent? torrentClientTorrent)
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

            torrent.RdHost = rdTorrent.Host;
            torrent.RdSplit = rdTorrent.Split;
            torrent.RdProgress = rdTorrent.Progress;
            torrent.RdAdded = rdTorrent.Added;
            torrent.RdEnded = rdTorrent.Ended;
            torrent.RdSpeed = rdTorrent.Speed;
            torrent.RdSeeders = rdTorrent.Seeders;
            torrent.RdStatusRaw = rdTorrent.Status;

            if (rdTorrent.Host == "true")
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
                    "downloading" => TorrentStatus.Downloading,
                    "completed" => TorrentStatus.Downloading,
                    "uploading" => TorrentStatus.Finished,
                    "uploading (no peers)" => TorrentStatus.Finished,
                    "stalled" => TorrentStatus.Downloading,
                    "stalled (no seeds)" => TorrentStatus.Downloading,
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

    public async Task<IList<String>?> GetDownloadLinks(Data.Models.Data.Torrent torrent)
    {
        if (torrent.Hash == null)
        {
            return null;
        }

        var rdTorrent = await GetInfo(torrent.Hash);

        var files = new List<String>();

        if (rdTorrent.Files?.Count != 0)
        {
            foreach (var file in rdTorrent.Files!)
            {
                var newFile = $"https://torbox.app/fakedl/{rdTorrent.Id}/{file.Id}";
                files.Add(newFile);
            }
        }

        return files;
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
        var result = await GetClient().Torrents.GetInfoAsync(torrentHash, skipCache: true);

        return Map(result!);
    }

    private void Log(String message, Data.Models.Data.Torrent? torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        logger.LogDebug(message);
    }
}
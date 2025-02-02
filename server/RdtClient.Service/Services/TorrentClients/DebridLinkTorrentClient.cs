using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DebridLinkFrNET; 
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using DebridLinkFrNET.Models;
using System.Web;
using RdtClient.Data.Models.Data;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services.TorrentClients;

public class DebridLinkClient : ITorrentClient
{
    private readonly ILogger<DebridLinkClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DebridLinkClient(ILogger<DebridLinkClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private DebridLinkFrNETClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("Real-Debrid API Key not set in the settings");
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(Settings.Get.Provider.Timeout);

            var DebridLinkClient = new DebridLinkFrNETClient(apiKey, httpClient);

            return DebridLinkClient;
        }
        catch (AggregateException ae)
        {
            foreach (var inner in ae.InnerExceptions)
            {
                _logger.LogError(inner, $"The connection to DebridLink has failed: {inner.Message}");
            }

            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, $"The connection to DebridLink has timed out: {ex.Message}");

            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, $"The connection to DebridLink has timed out: {ex.Message}");

            throw; 
        }
    }

    private TorrentClientTorrent Map(Torrent torrent)
    {
        return new TorrentClientTorrent
        {
            Id = torrent.Id ?? "",
            Filename = torrent.Name ?? "",
            OriginalFilename = torrent.Name ?? "",
            Hash = torrent.HashString ?? "",
            Bytes = torrent.TotalSize,
            OriginalBytes = 0,
            Host = torrent.ServerId ?? "",
            Split = 0,
            Progress = torrent.DownloadPercent,
            Status = torrent.Status.ToString(),
            Added = DateTimeOffset.FromUnixTimeSeconds(torrent.Created),
            Files = (torrent.Files ?? new List<TorrentFile>()).Select((m, i) => new TorrentClientFile
            {
                Path = m.Name,
                Bytes = m.Size,
                Id = i,
                Selected = true
            }).ToList(),
            Links = torrent.Files?.Select(m => m.DownloadUrl.ToString()).ToList(),
            Ended = null,
            Speed = torrent.UploadSpeed,
            Seeders = torrent.PeersConnected,
        };
    }

    public async Task<IList<TorrentClientTorrent>> GetTorrents()
    {
        var page = 0;
        var results = new List<Torrent>();

        while (true)
        {
            var pagedResults = await GetClient().Seedbox.ListAsync(null,page, 50);

            results.AddRange(pagedResults);

            if (pagedResults.Count == 0)
            {
                break;
            }

            page += 1;
        }

        return results.Select(Map).ToList();
    }

    public async Task<TorrentClientUser> GetUser()
    {
        var user = await GetClient().Account.Infos();
            
        return new TorrentClientUser
        {
            Username = user.Username,
            Expiration = user.PremiumLeft > 0 ? DateTimeOffset.Now.AddSeconds(user.PremiumLeft) : null
        };
    }

    public async Task<String> AddMagnet(String magnetLink)
    {
        var result = await GetClient().Seedbox.AddTorrentAsync(magnetLink);

        return result.Id ?? "";
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        var result = await GetClient().Seedbox.AddTorrentByFileAsync(bytes);

        return result.Id ?? "";
    }

    public Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        return Task.FromResult<IList<TorrentClientAvailableFile>>([]);
    }

    public Task SelectFiles(Data.Models.Data.Torrent torrent)
    {
        return Task.CompletedTask;
    }

    public async Task Delete(String torrentId)
    {
        await GetClient().Seedbox.DeleteAsync(torrentId);
    }

    public async Task<String> Unrestrict(String link)
    {
        return link;
    }

    public async Task<Data.Models.Data.Torrent> UpdateData(Data.Models.Data.Torrent torrent, TorrentClientTorrent? torrentClientTorrent)
    {
        try
        {
            if (torrent.RdId == null)
            {
                return torrent;
            }

            var rdTorrent = await GetInfo(torrent.RdId);

            if (rdTorrent == null)
            {
                throw new Exception($"Resource not found");
            }

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

            torrent.ClientKind = Data.Models.Data.Torrent.TorrentClientKind.DebridLink;
            torrent.RdHost = rdTorrent.Host;
            torrent.RdSplit = rdTorrent.Split;
            torrent.RdProgress = rdTorrent.Progress;
            torrent.RdAdded = rdTorrent.Added;
            torrent.RdEnded = rdTorrent.Ended;
            torrent.RdSpeed = rdTorrent.Speed;
            torrent.RdSeeders = rdTorrent.Seeders;
            torrent.RdStatusRaw = rdTorrent.Status;
            /**
             * 
             *  0   Torrent is stopped
             *  1   Torrent is queued to verify local data
             *  2   Torrent is verifying local data
             *  3   Torrent is queued to download
             *  4   Torrent is downloading
             *  5   Torrent is queued to seed
             *  6   Torrent is seeding
             *  100  Torrent is stored
             */
            torrent.RdStatus = rdTorrent.Status switch
            {
                "100" => TorrentStatus.Finished,
                "1" => TorrentStatus.Processing,
                "2" => TorrentStatus.Processing,
                "3" => TorrentStatus.Processing,
                "4" => TorrentStatus.Downloading,
                "5" => TorrentStatus.Finished,
                "6" => TorrentStatus.Finished,
                _ => TorrentStatus.Error
            };
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
        if (torrent.RdId == null)
        {
            return null;
        }

        var rdTorrent = await GetInfo(torrent.RdId);

        if (rdTorrent.Links == null)
        {
            return null;
        }

        var downloadLinks = rdTorrent.Links.Where(m => !String.IsNullOrWhiteSpace(m)).ToList();

        Log($"Found {downloadLinks.Count} links", torrent);

        foreach (var link in downloadLinks)
        {
            Log($"{link}", torrent);
        }

        // Check if all the links are set that have been selected
        if (torrent.Files.Count(m => m.Selected) == downloadLinks.Count)
        {
            return downloadLinks;
        }

        // Check if all all the links are set for manual selection
        if (torrent.ManualFiles.Count == downloadLinks.Count)
        {
            return downloadLinks;
        }

        // If there is only 1 link, delay for 1 minute to see if more links pop up.
        if (downloadLinks.Count == 1 && torrent.RdEnded.HasValue && DateTime.UtcNow > torrent.RdEnded.Value.ToUniversalTime().AddMinutes(1))
        {
            return downloadLinks;
        }
            
        return null;
    }

    private async Task<TorrentClientTorrent> GetInfo(String torrentId)
    {
        var result = await GetClient().Seedbox.ListAsync(torrentId);

        return Map(result.First());
    }

    private void Log(String message, Data.Models.Data.Torrent? torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        _logger.LogDebug(message);
    }

    public Task<string> GetFileName(string downloadUrl)
    {
        if (String.IsNullOrWhiteSpace(downloadUrl))
        {
            return Task.FromResult("");
        }

        var uri = new Uri(downloadUrl);

        return Task.FromResult(HttpUtility.UrlDecode(uri.Segments.Last()));
    }

    public static String? GetSymlinkPath(Torrent torrent, Download download)
    {
        if (torrent.RdName == null || download.FileName == null)
        {
            return null;
        }

        // Single file torrents always have that file at `/mnt-root/Seedbox/filename.ext`
        if (torrent.Files?.Count == 1)
        {
            return download.FileName;
        }

        return Path.Combine(torrent.RdName, download.FileName);
    }
}
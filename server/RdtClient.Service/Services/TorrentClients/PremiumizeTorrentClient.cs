using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PremiumizeNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services.TorrentClients;

public class PremiumizeTorrentClient : ITorrentClient
{
    private readonly ILogger<PremiumizeTorrentClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public PremiumizeTorrentClient(ILogger<PremiumizeTorrentClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private PremiumizeNETClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("Premiumize API Key not set in the settings");
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var premiumizeNetClient = new PremiumizeNETClient(apiKey, httpClient);

            return premiumizeNetClient;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, $"The connection to Premiumize has timed out: {ex.Message}");

            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, $"The connection to Premiumize has timed out: {ex.Message}");

            throw; 
        }
    }

    private static TorrentClientTorrent Map(Transfer transfer)
    {
        return new TorrentClientTorrent
        {
            Id = transfer.Id,
            Filename = transfer.Name,
            OriginalFilename = transfer.Name,
            Hash = transfer.Src,
            Bytes = 0,
            OriginalBytes = 0,
            Host = null,
            Split = 0,
            Progress = (Int64) ((transfer.Progress ?? 1.0) * 100.0),
            Status = transfer.Status,
            Message = transfer.Message,
            StatusCode = 0,
            Added = null,
            Files = new List<TorrentClientFile>(),
            Links = new List<String>
            {
                transfer.FolderId
            },
            Ended = null,
            Speed = 0,
            Seeders = 0
        };
    }

    public async Task<IList<TorrentClientTorrent>> GetTorrents()
    {
        var results = await GetClient().Transfers.ListAsync();
        return results.Select(Map).ToList();
    }

    public async Task<TorrentClientUser> GetUser()
    {
        var user = await GetClient().Account.InfoAsync();

        if (user == null)
        {
            throw new Exception("Unable to get user");
        }

        return new TorrentClientUser
        {
            Username = user.CustomerId.ToString(),
            Expiration = user.PremiumUntil > 0 ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(user.PremiumUntil.Value) : null
        };
    }

    public async Task<String> AddMagnet(String magnetLink)
    {
        var result = await GetClient().Transfers.CreateAsync(magnetLink, "");

        if (result?.Id == null)
        {
            throw new Exception("Unable to add magnet link");
        }

        var resultId = result.Id;

        if (resultId == null)
        {
            throw new Exception($"Invalid responseID {result.Id}");
        }

        return resultId;
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        var result = await GetClient().Transfers.CreateAsync(bytes, "");

        if (result?.Id == null)
        {
            throw new Exception("Unable to add torrent file");
        }

        var resultId = result.Id;

        if (resultId == null)
        {
            throw new Exception($"Invalid responseID {result.Id}");
        }

        return resultId;
    }

    public Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        var result = new List<TorrentClientAvailableFile>();
        return Task.FromResult<IList<TorrentClientAvailableFile>>(result);
    }

    public Task SelectFiles(Torrent torrent)
    {
        return Task.CompletedTask;
    }

    public async Task Delete(String id)
    {
        await GetClient().Transfers.DeleteAsync(id);
    }

    public Task<String> Unrestrict(String link)
    {
        return Task.FromResult(link);
    }

    public async Task<Torrent> UpdateData(Torrent torrent, TorrentClientTorrent? torrentClientTorrent)
    {
        try
        {
            if (torrent.RdId == null)
            {
                return torrent;
            }

            torrentClientTorrent ??= await GetInfo(torrent.RdId);

            if (!String.IsNullOrWhiteSpace(torrentClientTorrent.Filename))
            {
                torrent.RdName = torrentClientTorrent.Filename;
            }

            if (torrentClientTorrent.Bytes > 0)
            {
                torrent.RdSize = torrentClientTorrent.Bytes;
            }

            if (torrentClientTorrent.Files != null)
            {
                torrent.RdFiles = JsonConvert.SerializeObject(torrentClientTorrent.Files);
            }

            torrent.RdHost = torrentClientTorrent.Host;
            torrent.RdSplit = torrentClientTorrent.Split;
            torrent.RdProgress = torrentClientTorrent.Progress;
            torrent.RdAdded = torrentClientTorrent.Added;
            torrent.RdEnded = torrentClientTorrent.Ended;
            torrent.RdSpeed = torrentClientTorrent.Speed;
            torrent.RdSeeders = torrentClientTorrent.Seeders;
            torrent.RdStatusRaw = torrentClientTorrent.Status;

            torrent.RdStatus = torrentClientTorrent.Status switch
            {
                "waiting" => TorrentStatus.Processing,
                "queued" => TorrentStatus.Processing,
                "running" => TorrentStatus.Downloading,
                "seeding" => TorrentStatus.Uploading,
                "finished" => TorrentStatus.Finished,
                _ => TorrentStatus.Error
            };
        }
        catch (PremiumizeException ex)
        {
            if (ex.Message == "MAGNET_INVALID_ID")
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
        if (torrent.RdId == null)
        {
            return null;
        }

        var transfers = await GetClient().Transfers.ListAsync();

        Log($"Found {transfers.Count} transfers", torrent);

        var transfer = transfers.FirstOrDefault(m => m.Id == torrent.RdId);

        if (transfer == null)
        {
            Log($"Transfer {torrent.RdId} not found!", torrent);
            return null;
        }

        if (String.IsNullOrWhiteSpace(transfer.FolderId))
        {
            Log($"Transfer {torrent.RdId} has no folderId!", torrent);
            return null;
        }

        var files = await GetClient().Folder.ListAsync(transfer.FolderId);

        var downloadLinks = files.Content.Where(m => !String.IsNullOrWhiteSpace(m.Link)).Select(m => m.Link).ToList();

        Log($"Found {downloadLinks.Count} links", torrent);

        foreach (var link in downloadLinks)
        {
            Log($"{link}", torrent);
        }

        return downloadLinks;
    }

    private async Task<TorrentClientTorrent> GetInfo(String id)
    {
        var results = await GetClient().Transfers.ListAsync();
        var result = results.FirstOrDefault(m => m.Id == id);

        if (result == null)
        {
            throw new Exception($"Unable to find transfer with ID {id}");
        }

        return Map(result);
    }

    private void Log(String message, Torrent? torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        _logger.LogDebug(message);
    }
}
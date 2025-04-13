using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PremiumizeNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using RdtClient.Data.Models.Data;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services.TorrentClients;

public class PremiumizeTorrentClient(ILogger<PremiumizeTorrentClient> logger, IHttpClientFactory httpClientFactory, IDownloadableFileFilter fileFilter) : ITorrentClient
{
    private PremiumizeNETClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new("Premiumize API Key not set in the settings");
            }

            var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var premiumizeNetClient = new PremiumizeNETClient(apiKey, httpClient);

            return premiumizeNetClient;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogError(ex, $"The connection to Premiumize has timed out: {ex.Message}");

            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"The connection to Premiumize has timed out: {ex.Message}");

            throw; 
        }
    }

    private static TorrentClientTorrent Map(Transfer transfer)
    {
        return new()
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
            Files = [],
            Links =
            [
                transfer.FolderId
            ],
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
        var user = await GetClient().Account.InfoAsync() ?? throw new("Unable to get user");

        return new()
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
            throw new("Unable to add magnet link");
        }

        var resultId = result.Id ?? throw new($"Invalid responseID {result.Id}");

        return resultId;
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        var result = await GetClient().Transfers.CreateAsync(bytes, "");

        if (result?.Id == null)
        {
            throw new("Unable to add torrent file");
        }

        var resultId = result.Id ?? throw new($"Invalid responseID {result.Id}");

        return resultId;
    }

    public Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        return Task.FromResult<IList<TorrentClientAvailableFile>>([]);
    }

    /// <inheritdoc />
    public Task<Int32?> SelectFiles(Torrent torrent)
    {
        // torrent.Files is not populated when this function is called
        // by returning 1, we ensure no logic for "all files excluded" is followed
        return Task.FromResult<Int32?>(1);
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

            torrent.ClientKind = Provider.Premiumize;
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
                "seeding" => TorrentStatus.Finished,
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

    public async Task<IList<DownloadInfo>?> GetDownloadInfos(Torrent torrent)
    {
        if (torrent.RdId == null)
        {
            return null;
        }

        var transfers = await GetClient().Transfers.ListAsync();

        Log($"Found {transfers.Count} transfers", torrent);

        var transfer = transfers.FirstOrDefault(m => m.Id == torrent.RdId) ?? throw new($"Transfer {torrent.RdId} not found!");

        Log($"Found transfer {transfer.Name} ({transfer.Id})", torrent);

        var downloadInfos = await GetAllDownloadInfos(torrent, transfer.FolderId);
        
        if (!String.IsNullOrWhiteSpace(transfer.FileId))
        {
            var file = await GetClient().Items.DetailsAsync(transfer.FileId);

            Log($"Found {transfer.FileId}", torrent);

            if (String.IsNullOrWhiteSpace(file.Link))
            {
                Log($"File {file.Name} ({file.Id}) does not contain a link", torrent);
            }

            downloadInfos.Add(new () {RestrictedLink = file.Link, FileName = file.Name });
        }

        foreach (var info in downloadInfos)
        {
            Log($"Found {info.RestrictedLink}", torrent);
        }

        return downloadInfos;
    }

    /// <inheritdoc />
    public Task<String> GetFileName(Download download)
    {
        // FileName is set in GetDownlaadInfos
        Debug.Assert(download.FileName != null);
        
        return Task.FromResult(download.FileName);
    }

    private async Task<TorrentClientTorrent> GetInfo(String id)
    {
        var results = await GetClient().Transfers.ListAsync();
        var result = results.FirstOrDefault(m => m.Id == id) ?? throw new($"Unable to find transfer with ID {id}");

        return Map(result);
    }

    private async Task<List<DownloadInfo>> GetAllDownloadInfos(Torrent torrent, String folderId)
    {

        if (String.IsNullOrWhiteSpace(folderId))
        {
            return [];
        }

        var folder = await GetClient().Folder.ListAsync(folderId);

        if (folder.Content == null)
        {
            Log($"Found no items in folder {folder.Name} ({folderId})", torrent);
            return [];
        }

        Log($"Found {folder.Content.Count} items in folder {folder.Name} ({folderId})", torrent);

        var downloadInfos = new List<DownloadInfo>();

        foreach (var item in folder.Content)
        {
            if (item.Type == "file")
            {
                if (String.IsNullOrWhiteSpace(item.Link))
                {
                    Log($"Found item {item.Name} in folder {folder.Name} ({folderId}), but has no link", torrent);

                    continue;
                }

                if (!fileFilter.IsDownloadable(torrent, item.Name, item.Size))
                {
                    continue;
                }

                Log($"Found item {item.Name} in folder {folder.Name} ({folderId})", torrent);

                downloadInfos.Add(new () { RestrictedLink = item.Link, FileName = item.Name});
            }
            else if (item.Type == "folder")
            {
                // Folders don't have Size, use maximum Int64 so it always passes min size check
                if (!fileFilter.IsDownloadable(torrent, item.Name, Int64.MaxValue))
                {
                    continue;
                }

                Log($"Found subfolder {item.Name} in {folder.Name} ({folderId}), searching subfolder for files", torrent);
                var subDownloadLinks = await GetAllDownloadInfos(torrent, item.Id);
                downloadInfos.AddRange(subDownloadLinks);
            }
            else
            {
                Log($"Found item {item.Name} with unknown type {item.Type} in folder {folder.Name} ({folderId})", torrent);
            }
        }

        return downloadInfos;
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
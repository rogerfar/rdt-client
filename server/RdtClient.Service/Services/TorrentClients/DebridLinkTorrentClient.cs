using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DebridLinkFrNET; 
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using RdtClient.Data.Models.Data;
using Download = RdtClient.Data.Models.Data.Download;
using Torrent = DebridLinkFrNET.Models.Torrent;

namespace RdtClient.Service.Services.TorrentClients;

public class DebridLinkClient(ILogger<DebridLinkClient> logger, IHttpClientFactory httpClientFactory, IDownloadableFileFilter fileFilter) : ITorrentClient
{
    private DebridLinkFrNETClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new("DebridLink API Key not set in the settings");
            }

            var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(Settings.Get.Provider.Timeout);

            var debridLinkClient = new DebridLinkFrNETClient(apiKey, httpClient);

            return debridLinkClient;
        }
        catch (AggregateException ae)
        {
            foreach (var inner in ae.InnerExceptions)
            {
                logger.LogError(inner, $"The connection to DebridLink has failed: {inner.Message}");
            }

            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogError(ex, $"The connection to DebridLink has timed out: {ex.Message}");

            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"The connection to DebridLink has timed out: {ex.Message}");

            throw; 
        }
    }

    private TorrentClientTorrent Map(Torrent torrent)
    {
        return new()
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
            Files = (torrent.Files ?? []).Select((m, i) => new TorrentClientFile
                                         {
                                             Path = m.Name ?? "",
                                             Bytes = m.Size,
                                             Id = i,
                                             Selected = true,
                                             DownloadLink = m.DownloadUrl
                                         })
                                         .ToList(),
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
            
        return new()
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

    /// <inheritdoc />
    public Task<Int32?> SelectFiles(Data.Models.Data.Torrent torrent)
    {
        return Task.FromResult<Int32?>(torrent.Files.Count);
    }

    public async Task Delete(String torrentId)
    {
        await GetClient().Seedbox.DeleteAsync(torrentId);
    }

    public Task<String> Unrestrict(String link)
    {
        return Task.FromResult(link);
    }

    public async Task<Data.Models.Data.Torrent> UpdateData(Data.Models.Data.Torrent torrent, TorrentClientTorrent? torrentClientTorrent)
    {
        try
        {
            if (torrent.RdId == null)
            {
                return torrent;
            }

            var rdTorrent = await GetInfo(torrent.RdId) ?? throw new($"Resource not found");

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

            torrent.ClientKind = Provider.DebridLink;
            torrent.RdHost = rdTorrent.Host;
            torrent.RdSplit = rdTorrent.Split;
            torrent.RdProgress = rdTorrent.Progress;
            torrent.RdAdded = rdTorrent.Added;
            torrent.RdEnded = rdTorrent.Ended;
            torrent.RdSpeed = rdTorrent.Speed;
            torrent.RdSeeders = rdTorrent.Seeders;
            torrent.RdStatusRaw = rdTorrent.Status;

             /*
              0   Torrent is stopped
              1   Torrent is queued to verify local data
              2   Torrent is verifying local data
              3   Torrent is queued to download
              4   Torrent is downloading
              5   Torrent is queued to seed
              6   Torrent is seeding
              100  Torrent is stored
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

    public async Task<IList<DownloadInfo>?> GetDownloadInfos(Data.Models.Data.Torrent torrent)
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

        return rdTorrent.Files?
                        .Where(m => fileFilter.IsDownloadable(torrent, m.Path, m.Bytes) && m.DownloadLink != null)
                        .Select(m => new DownloadInfo
                        {
                            RestrictedLink = m.DownloadLink!,
                            FileName = Path.GetFileName(m.Path)
                        })
                        .ToList();
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

        logger.LogDebug(message);
    }

    /// <inheritdoc />
    public Task<String> GetFileName(Download download)
    {
        // FileName is set in GetDownlaadInfos
        Debug.Assert(download.FileName != null);
        
        return Task.FromResult(download.FileName);
    }

    public static String? GetSymlinkPath(Data.Models.Data.Torrent torrent, Download download)
    {
        if (torrent.RdName == null || download.FileName == null)
        {
            return null;
        }

        // Single file torrents always have that file at `/mnt-root/Seedbox/filename.ext`
        if (torrent.Files.Count == 1)
        {
            return download.FileName;
        }

        return Path.Combine(torrent.RdName, download.FileName);
    }
}
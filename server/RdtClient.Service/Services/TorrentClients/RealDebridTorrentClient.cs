using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RDNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using Download = RdtClient.Data.Models.Data.Download;
using Torrent = RDNET.Torrent;

namespace RdtClient.Service.Services.TorrentClients;

public class RealDebridTorrentClient(ILogger<RealDebridTorrentClient> logger, IHttpClientFactory httpClientFactory, IDownloadableFileFilter fileFilter) : ITorrentClient
{
    private TimeSpan? _offset;

    private RdNetClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new("Real-Debrid API Key not set in the settings");
            }

            var httpClient = httpClientFactory.CreateClient(DiConfig.RD_CLIENT);
            httpClient.Timeout = TimeSpan.FromSeconds(Settings.Get.Provider.Timeout);

            var rdtNetClient = new RdNetClient(null, httpClient, 5, Settings.Get.Provider.ApiHostname);
            rdtNetClient.UseApiAuthentication(apiKey);

            // Get the server time to fix up the timezones on results
            if (_offset == null)
            {
                var serverTime = rdtNetClient.Api.GetIsoTimeAsync().Result;
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
            Id = torrent.Id,
            Filename = torrent.Filename,
            OriginalFilename = torrent.OriginalFilename,
            Hash = torrent.Hash,
            Bytes = torrent.Bytes,
            OriginalBytes = torrent.OriginalBytes,
            Host = torrent.Host,
            Split = torrent.Split,
            Progress = torrent.Progress,
            Status = torrent.Status,
            Added = ChangeTimeZone(torrent.Added)!.Value,
            Files = (torrent.Files ?? []).Select(m => new TorrentClientFile
            {
                Path = m.Path,
                Bytes = m.Bytes,
                Id = m.Id,
                Selected = m.Selected
            }).ToList(),
            Links = torrent.Links,
            Ended = ChangeTimeZone(torrent.Ended),
            Speed = torrent.Speed,
            Seeders = torrent.Seeders,
        };
    }

    public async Task<IList<TorrentClientTorrent>> GetTorrents()
    {
        var offset = 0;
        var results = new List<Torrent>();

        while (true)
        {
            var pagedResults = await GetClient().Torrents.GetAsync(offset, 5000);

            results.AddRange(pagedResults);

            if (pagedResults.Count == 0)
            {
                break;
            }

            offset += 5000;
        }

        return results.Select(Map).ToList();
    }

    public async Task<TorrentClientUser> GetUser()
    {
        var user = await GetClient().User.GetAsync();
            
        return new()
        {
            Username = user.Username,
            Expiration = user.Premium > 0 ? user.Expiration : null
        };
    }

    public async Task<String> AddMagnet(String magnetLink)
    {
        var timeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(Settings.Get.Provider.Timeout));

        var result = await GetClient().Torrents.AddMagnetAsync(magnetLink, timeoutCancellationToken.Token);

        return result.Id;
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        var timeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(Settings.Get.Provider.Timeout));

        var result = await GetClient().Torrents.AddFileAsync(bytes, timeoutCancellationToken.Token);

        return result.Id;
    }

    public Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        return Task.FromResult<IList<TorrentClientAvailableFile>>([]);
    }

    /// <inheritdoc />
    public async Task<Int32?> SelectFiles(Data.Models.Data.Torrent torrent)
    {
        List<TorrentClientFile> files;

        Log("Seleting files", torrent);

        if (torrent.DownloadAction == TorrentDownloadAction.DownloadManual)
        {
            Log("Selecting manual selected files", torrent);
            files = torrent.Files.Where(m => torrent.ManualFiles.Any(f => m.Path.EndsWith(f))).ToList();
        }
        else
        {
            Log("Selecting files", torrent);
            files = [.. torrent.Files];
        }


        files = files.Where(f => fileFilter.IsDownloadable(torrent, f.Path, f.Bytes)).ToList();

        Log($"Selecting {files.Count}/{torrent.Files.Count} files", torrent);

        var fileIds = files.Select(m => m.Id.ToString()).ToArray();

        if (fileIds.Length == 0)
        {
            return 0;
        }

        await GetClient().Torrents.SelectFilesAsync(torrent.RdId!, [.. fileIds]);

        return fileIds.Length;
    }

    public async Task Delete(String torrentId)
    {
        await GetClient().Torrents.DeleteAsync(torrentId);
    }

    public async Task<String> Unrestrict(String link)
    {
        var result = await GetClient().Unrestrict.LinkAsync(link);

        if (result.Download == null)
        {
            throw new($"Unrestrict returned an invalid download");
        }

        return result.Download;
    }

    public async Task<Data.Models.Data.Torrent> UpdateData(Data.Models.Data.Torrent torrent, TorrentClientTorrent? torrentClientTorrent)
    {
        try
        {
            if (torrent.RdId == null)
            {
                return torrent;
            }

            if (torrentClientTorrent == null || torrentClientTorrent.Ended == null || String.IsNullOrEmpty(torrentClientTorrent.Filename))
            {
                torrentClientTorrent = await GetInfo(torrent.RdId) ?? throw new($"Resource not found");
            }

            if (!String.IsNullOrWhiteSpace(torrentClientTorrent.Filename))
            {
                torrent.RdName = torrentClientTorrent.Filename;
            }

            if (!String.IsNullOrWhiteSpace(torrentClientTorrent.OriginalFilename))
            {
                torrent.RdName = torrentClientTorrent.OriginalFilename;
            }

            if (torrentClientTorrent.Bytes > 0)
            {
                torrent.RdSize = torrentClientTorrent.Bytes;
            }
            else if (torrentClientTorrent.OriginalBytes > 0)
            {
                torrent.RdSize = torrentClientTorrent.OriginalBytes;
            }

            if (torrentClientTorrent.Files != null && torrentClientTorrent.Files.Count > 0)
            {
                torrent.RdFiles = JsonConvert.SerializeObject(torrentClientTorrent.Files);
            }

            torrent.ClientKind = Provider.RealDebrid;
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
                "magnet_error" => TorrentStatus.Error,
                "magnet_conversion" => TorrentStatus.Processing,
                "waiting_files_selection" => TorrentStatus.WaitingForFileSelection,
                "queued" => TorrentStatus.Downloading,
                "downloading" => TorrentStatus.Downloading,
                "downloaded" => TorrentStatus.Finished,
                "error" => TorrentStatus.Error,
                "virus" => TorrentStatus.Error,
                "compressing" => TorrentStatus.Downloading,
                "uploading" => TorrentStatus.Uploading,
                "dead" => TorrentStatus.Error,
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

        var downloadLinks = rdTorrent.Links
                                     .Where(m => !String.IsNullOrWhiteSpace(m))
                                     .Select(l => new DownloadInfo
                                     {
                                         RestrictedLink = l,
                                         FileName = null
                                     })
                                     .ToList();

        Log($"Found {downloadLinks.Count} links", torrent);

        foreach (var link in downloadLinks)
        {
            Log($"{link}", torrent);
        }

        Log($"Torrent has {torrent.Files.Count(m => m.Selected)} selected files out of {torrent.Files.Count} files, found {downloadLinks.Count} links, torrent ended: {torrent.RdEnded}", torrent);
        
        // Check if all the links are set that have been selected
        if (torrent.Files.Count(m => m.Selected) == downloadLinks.Count)
        {
            Log($"Matched {torrent.Files.Count(m => m.Selected)} selected files expected files to {downloadLinks.Count} found files", torrent);

            return downloadLinks;
        }

        // Check if all all the links are set for manual selection
        if (torrent.ManualFiles.Count == downloadLinks.Count)
        {
            Log($"Matched {torrent.ManualFiles.Count} manual files expected files to {downloadLinks.Count} found files", torrent);

            return downloadLinks;
        }

        // If there is only 1 link, delay for 1 minute to see if more links pop up.
        if (downloadLinks.Count == 1 && torrent.RdEnded.HasValue)
        {
            var expired = DateTime.UtcNow - torrent.RdEnded.Value.ToUniversalTime();

            Log($"Waiting to see if more links appear, checked for {expired.TotalSeconds} seconds", torrent);

            if (expired.TotalSeconds > 60.0)
            {
                Log($"Waited long enough", torrent);

                return downloadLinks;
            }
        }

        Log($"Did not find any suiteable download links", torrent);
            
        return null;
    }

    /// <inheritdoc />
    public Task<String> GetFileName(Download download)
    {
        if (String.IsNullOrWhiteSpace(download.Link))
        {
            return Task.FromResult("");
        }

        var uri = new Uri(download.Link);

        return Task.FromResult(HttpUtility.UrlDecode(uri.Segments.Last()));
    }

    private DateTimeOffset? ChangeTimeZone(DateTimeOffset? dateTimeOffset)
    {
        if (_offset == null)
        {
            return dateTimeOffset;
        }

        return dateTimeOffset?.Subtract(_offset.Value).ToOffset(_offset.Value);
    }

    private async Task<TorrentClientTorrent> GetInfo(String torrentId)
    {
        var result = await GetClient().Torrents.GetInfoAsync(torrentId);

        return Map(result);
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
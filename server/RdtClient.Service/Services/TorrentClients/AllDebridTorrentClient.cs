using System.Text.RegularExpressions;
using AllDebridNET;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using System.Web;
using RdtClient.Data.Models.Data;
using File = AllDebridNET.File;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services.TorrentClients;

public class AllDebridTorrentClient(ILogger<AllDebridTorrentClient> logger, IHttpClientFactory httpClientFactory) : ITorrentClient
{
    private AllDebridNETClient GetClient()
    {
        try
        {
            var apiKey = Settings.Get.Provider.ApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new("All-Debrid API Key not set in the settings");
            }

            var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var allDebridNetClient = new AllDebridNETClient("RealDebridClient", apiKey, httpClient);

            return allDebridNetClient;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogError(ex, $"The connection to AllDebrid has timed out: {ex.Message}");

            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"The connection to AllDebrid has timed out: {ex.Message}");

            throw;
        }
    }

    private static TorrentClientTorrent Map(Magnet torrent)
    {
        return new()
        {
            Id = torrent.Id.ToString(),
            Filename = torrent.Filename,
            OriginalFilename = torrent.Filename,
            Hash = torrent.Hash,
            Bytes = torrent.Size,
            OriginalBytes = torrent.Size,
            Host = null,
            Split = 0,
            Progress = (Int64)Math.Round(torrent.Downloaded * 100.0 / torrent.Size),
            Status = torrent.Status,
            StatusCode = torrent.StatusCode,
            Added = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(torrent.UploadDate),
            Files = torrent.Links.Select((m, i) => new TorrentClientFile
                           {
                               Path = GetFiles(m.Files),
                               Bytes = m.Size,
                               Id = i,
                               Selected = true,
                           })
                           .ToList(),
            Links = torrent.Links.Select(m => m.LinkUrl.ToString()).ToList(),
            Ended = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(torrent.CompletionDate),
            Speed = torrent.DownloadSpeed,
            Seeders = torrent.Seeders
        };
    }

    public async Task<IList<TorrentClientTorrent>> GetTorrents()
    {
        var results = await GetClient().Magnet.StatusAllAsync();

        return results.Select(Map).ToList();
    }

    public async Task<TorrentClientUser> GetUser()
    {
        var user = await GetClient().User.GetAsync() ?? throw new("Unable to get user");

        return new()
        {
            Username = user.Username,
            Expiration = user.PremiumUntil > 0 ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(user.PremiumUntil) : null
        };
    }

    public async Task<String> AddMagnet(String magnetLink)
    {
        var result = await GetClient().Magnet.UploadMagnetAsync(magnetLink);

        if (result?.Id == null)
        {
            throw new("Unable to add magnet link");
        }

        var resultId = result.Id.ToString() ?? throw new($"Invalid responseID {result.Id}");

        return resultId;
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        var result = await GetClient().Magnet.UploadFileAsync(bytes);

        if (result?.Id == null)
        {
            throw new("Unable to add torrent file");
        }

        var resultId = result.Id.ToString() ?? throw new($"Invalid responseID {result.Id}");

        return resultId;
    }

    public Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        return Task.FromResult<IList<TorrentClientAvailableFile>>([]);
    }

    public Task SelectFiles(Torrent torrent)
    {
        return Task.CompletedTask;
    }

    public async Task Delete(String torrentId)
    {
        await GetClient().Magnet.DeleteAsync(torrentId);
    }

    public async Task<String> Unrestrict(String link)
    {
        var result = await GetClient().Links.DownloadLinkAsync(link);

        if (result.Link == null)
        {
            throw new("Invalid result link");
        }

        return result.Link;
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

            torrent.ClientKind = Torrent.TorrentClientKind.AllDebrid;
            torrent.RdHost = torrentClientTorrent.Host;
            torrent.RdSplit = torrentClientTorrent.Split;
            torrent.RdProgress = torrentClientTorrent.Progress;
            torrent.RdAdded = torrentClientTorrent.Added;
            torrent.RdEnded = torrentClientTorrent.Ended;
            torrent.RdSpeed = torrentClientTorrent.Speed;
            torrent.RdSeeders = torrentClientTorrent.Seeders;
            torrent.RdStatusRaw = torrentClientTorrent.Status;

            torrent.RdStatus = torrentClientTorrent.StatusCode switch
            {
                0 => TorrentStatus.Processing, // Processing	In Queue.
                1 => TorrentStatus.Downloading, // Processing	Downloading.
                2 => TorrentStatus.Downloading, // Processing	Compressing / Moving.
                3 => TorrentStatus.Uploading, // Processing	Uploading.
                4 => TorrentStatus.Finished, // Finished	Ready.
                5 => TorrentStatus.Error, // Error	Upload fail.
                6 => TorrentStatus.Error, // Error	Internal error on unpacking.
                7 => TorrentStatus.Error, // Error	Not downloaded in 20 min.
                8 => TorrentStatus.Error, // Error	File too big.
                9 => TorrentStatus.Error, // 	Error	Internal error.
                10 => TorrentStatus.Error, // Error	Download took more than 72h.
                11 => TorrentStatus.Error, // Error	Deleted on the hoster website
                _ => TorrentStatus.Error
            };
        }
        catch (AllDebridException ex)
        {
            if (ex.ErrorCode == "MAGNET_INVALID_ID")
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

        var magnet = await GetClient().Magnet.StatusAsync(torrent.RdId);

        if (magnet == null)
        {
            return null;
        }

        var links = magnet.Links;

        Log($"Getting download links", torrent);

        if (torrent.DownloadMinSize > 0)
        {
            var minFileSize = torrent.DownloadMinSize * 1024 * 1024;

            Log($"Determining which files are over {minFileSize} bytes", torrent);

            links = links.Where(m => m.Size > minFileSize)
                         .ToList();

            Log($"Found {links.Count} files that match the minimum file size criterea", torrent);
        }

        if (!String.IsNullOrWhiteSpace(torrent.IncludeRegex))
        {
            Log($"Using regular expression {torrent.IncludeRegex} to include only files matching this regex", torrent);

            var newLinks = new List<Link>();

            foreach (var link in links)
            {
                var path = GetFiles(link.Files);

                if (Regex.IsMatch(path, torrent.IncludeRegex))
                {
                    Log($"* Including {path}", torrent);
                    newLinks.Add(link);
                }
                else
                {
                    Log($"* Excluding {path}", torrent);
                }
            }

            links = newLinks;

            Log($"Found {newLinks.Count} files that match the regex", torrent);
        }
        else if (!String.IsNullOrWhiteSpace(torrent.ExcludeRegex))
        {
            Log($"Using regular expression {torrent.IncludeRegex} to ignore files matching this regex", torrent);

            var newLinks = new List<Link>();

            foreach (var link in links)
            {
                var path = GetFiles(link.Files);

                if (!Regex.IsMatch(path, torrent.ExcludeRegex))
                {
                    Log($"* Including {path}", torrent);
                    newLinks.Add(link);
                }
                else
                {
                    Log($"* Excluding {path}", torrent);
                }
            }

            links = newLinks;

            Log($"Found {newLinks.Count} files that match the regex", torrent);
        }

        if (links.Count == 0)
        {
            Log($"Filtered all files out! Downloading ALL files instead!", torrent);

            links = magnet.Links;
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            Log($"Selecting links:");

            foreach (var link in links)
            {
                Log($"{GetFiles(link.Files)} ({link.Size}b) {link.LinkUrl}");
            }
        }

        Log("", torrent);

        return links.Select(m => m.LinkUrl.ToString()).ToList();
    }

    public Task<String> GetFileName(String downloadUrl)
    {
        if (String.IsNullOrWhiteSpace(downloadUrl))
        {
            return Task.FromResult("");
        }

        var uri = new Uri(downloadUrl);

        return Task.FromResult(HttpUtility.UrlDecode(uri.Segments.Last()));
    }

    private async Task<TorrentClientTorrent> GetInfo(String torrentId)
    {
        var result = await GetClient().Magnet.StatusAsync(torrentId) ?? throw new($"Unable to find magnet with ID {torrentId}");

        return Map(result);
    }

    private static String GetFiles(IList<File> files)
    {
        var result = new List<String>();

        foreach (var file in files)
        {
            if (!String.IsNullOrWhiteSpace(file.N))
            {
                result.Add(file.N);
            }

            if (file.E != null && file.E.Value.PurpleEArray != null && file.E.Value.PurpleEArray.Count > 0)
            {
                if (file.E.Value.PurpleEArray.Count != 1)
                {
                    throw new("Unexpected number of nested files");
                }

                result.AddRange(GetFiles(file.E.Value.PurpleEArray));
            }
        }

        return String.Join("/", result);
    }

    private static List<String> GetFiles(IList<FileE1> files)
    {
        var result = new List<String>();

        foreach (var file in files)
        {
            if (!String.IsNullOrWhiteSpace(file.N))
            {
                result.Add(file.N);
            }

            if (file.E != null && file.E.Count > 0)
            {
                result.AddRange(GetFiles(file.E));
            }
        }

        return result;
    }

    private static List<String> GetFiles(IList<FileE2> files)
    {
        var result = new List<String>();

        foreach (var file in files)
        {
            if (!String.IsNullOrWhiteSpace(file.N))
            {
                result.Add(file.N);
            }
        }

        return result;
    }

    private void Log(String message, Torrent? torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        logger.LogDebug(message);
    }

    public static String? GetSymlinkPath(Torrent torrent, Download download)
    {
        var fileName = DownloadHelper.GetFileName(download);

        if (fileName == null || torrent.RdName == null)
        {
            return null;
        }
        
        var directory = DownloadHelper.RemoveInvalidPathChars(torrent.RdName);

        var matchingTorrentFiles = torrent.Files.Where(m => m.Path.EndsWith(fileName)).Where(m => !String.IsNullOrWhiteSpace(m.Path)).ToList();

        if (matchingTorrentFiles.Count == 0)
        {
            throw new Exception($"Could not find file {fileName} in {torrent.RdName}");
        }

        // Torrents with a single file in them don't need to have the `RdName` added
        if (torrent.Files.Count == 1)
        {
            return matchingTorrentFiles[0].Path;
        }

        return Path.Combine(directory, matchingTorrentFiles[0].Path);
    }
}

using AllDebridNET;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RDNET;
using RDNET.Exceptions;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using File = AllDebridNET.File;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services.TorrentClients;

public class AllDebridTorrentClient : ITorrentClient
{
    private readonly ILogger<AllDebridTorrentClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public AllDebridTorrentClient(ILogger<AllDebridTorrentClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private AllDebridNETClient GetClient()
    {
        var apiKey = Settings.Get.RealDebridApiKey;

        if (String.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("All-Debrid API Key not set in the settings");
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        var allDebridNetClient = new AllDebridNETClient("RealDebridClient", apiKey);

        return allDebridNetClient;
    }

    private static TorrentClientTorrent Map(Magnet torrent)
    {
        return new TorrentClientTorrent
        {
            Id = torrent.Id.ToString(),
            Filename = torrent.Filename,
            OriginalFilename = torrent.Filename,
            Hash = torrent.Hash,
            Bytes = torrent.Size,
            OriginalBytes = torrent.Size,
            Host = null,
            Split = 0,
            Progress = (Int64) Math.Round(torrent.Downloaded * 100.0 / torrent.Size),
            Status = torrent.Status,
            StatusCode = torrent.StatusCode,
            Added = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(torrent.UploadDate),
            Files = (torrent.Links ?? new List<Link>()).Select((m, i) => new TorrentClientFile
            {
                Path = m.Filename,
                Bytes = m.Size,
                Id = i,
                Selected = true,
            }).ToList(),
            Links = (torrent.Links ?? new List<Link>()).Select(m => m.LinkUrl.ToString()).ToList(),
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
        var user = await GetClient().User.GetAsync();

        return new TorrentClientUser
        {
            Username = user.Username,
            Expiration = user.PremiumUntil > 0 ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(user.PremiumUntil) : null
        };
    }

    public async Task<String> AddMagnet(String magnetLink)
    {
        var result = await GetClient().Magnet.UploadMagnetAsync(magnetLink);

        return result.Id.ToString();
    }

    public async Task<String> AddFile(Byte[] bytes)
    {
        var result = await GetClient().Magnet.UploadFileAsync(bytes);

        return result.Id.ToString();
    }

    public async Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        var isAvailable = await GetClient().Magnet.InstantAvailabilityAsync(hash);

        if (isAvailable)
        {
            return new List<TorrentClientAvailableFile>
            {
                new TorrentClientAvailableFile
                {
                    Filename = "All files",
                    Filesize = 0
                }
            };
        }

        return new List<TorrentClientAvailableFile>();
    }

    public Task SelectFiles(Torrent torrent)
    {
        return Task.CompletedTask;
    }

    public async Task<TorrentClientTorrent> GetInfo(String torrentId)
    {
        var result = await GetClient().Magnet.StatusAsync(torrentId);

        return Map(result);
    }

    public async Task Delete(String torrentId)
    {
        await GetClient().Magnet.DeleteAsync(torrentId);
    }

    public async Task<String> Unrestrict(String link)
    {
        var result = await GetClient().Links.DownloadLinkAsync(link);

        return result.Link;
    }

    public async Task<Torrent> UpdateData(Torrent torrent, TorrentClientTorrent torrentClientTorrent)
    {
        try
        {
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
        catch (RealDebridException ex)
        {
            if (ex.ErrorCode == 7)
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

    public async Task<IList<String>> GetDownloadLinks(Torrent torrent)
    {
        var magnet = await GetClient().Magnet.StatusAsync(torrent.RdId);

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

        if (links.Count == 0)
        {
            Log($"Filtered all files out! Downloading ALL files instead!", torrent);

            links = magnet.Links;
        }

        Log($"Selecting links:");

        foreach (var link in links)
        {
            if (link.Files == null)
            {
                continue;
            }

            var fileList = GetFiles(link.Files, "");

            Log($"{link.Filename} ({link.Size}b) {link.LinkUrl}, contains files:{Environment.NewLine}{String.Join(Environment.NewLine, fileList)}");
        }

        Log("", torrent);

        return links.Select(m => m.LinkUrl.ToString()).ToList();
    }

    private static IEnumerable<String> GetFiles(IList<File> files, String parent)
    {
        var result = new List<String>();

        foreach (var file in files)
        {
            if (!String.IsNullOrWhiteSpace(file.N))
            {
                result.Add($"{parent}/{file.N}");
            }

            if (file.E != null && file.E.Value.PurpleEArray != null && file.E.Value.PurpleEArray.Count > 0)
            {
                result.AddRange(GetFiles(file.E.Value.PurpleEArray, file.N));
            }
        }

        return result;
    }

    private static IEnumerable<String> GetFiles(IList<FileE1> files, String parent)
    {
        var result = new List<String>();

        foreach (var file in files)
        {
            if (!String.IsNullOrWhiteSpace(file.N))
            {
                result.Add($"{parent}/{file.N}");
            }

            if (file.E != null && file.E.Count > 0)
            {
                result.AddRange(GetFiles(file.E, file.N));
            }
        }

        return result;
    }

    private static IEnumerable<String> GetFiles(IList<FileE2> files, String parent)
    {
        var result = new List<String>();

        foreach (var file in files)
        {
            if (!String.IsNullOrWhiteSpace(file.N))
            {
                result.Add($"{parent}/{file.N}");
            }
        }

        return result;
    }

    private void Log(String message, Torrent torrent = null)
    {
        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        _logger.LogDebug(message);
    }
}
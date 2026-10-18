using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.DebridClient;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
using Download = RdtClient.Data.Models.Data.Download;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services.DebridClients;

/// <summary>
///     Client for HIGHWAY (<c>high-way.me</c>), a self-hosted "cloud download center" that downloads
///     torrents and usenet server-side.
/// </summary>
public class HighwayDebridClient(
    ILogger<HighwayDebridClient> logger,
    IHttpClientFactory httpClientFactory,
    IDownloadableFileFilter fileFilter,
    ISettings settings)
    : IDebridClient
{
    /// <summary>Sentinel link scheme used between <see cref="GetDownloadInfos" /> and <see cref="Unrestrict" />.</summary>
    private const String LinkScheme = "highway://";

    /// <summary>HIGHWAY has a single public API host; not user-configurable to keep the settings page small.</summary>
    private const String ApiBaseUrl = "https://cloud.high-way.me";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IList<DebridClientTorrent>> GetDownloads()
    {
        var transfers = await ListTransfers();

        return transfers.Select(Map).ToList();
    }

    public async Task<DebridClientUser> GetUser()
    {
        HighwayStats? stats = null;

        try
        {
            stats = await GetStats();
        }
        catch (JsonException)
        {
            // older server builds ignore ?stats=1 and return the transfer list instead
        }

        var username = stats?.Username;

        if (String.IsNullOrWhiteSpace(username))
        {
            username = stats?.Plan;
        }

        return new()
        {
            Username = String.IsNullOrWhiteSpace(username) ? "HIGHWAY" : username,

            // Dated plans (subscription/legacy) get an expiry; unlimited/no-expiry plans don't, but are
            // still premium - Premium below is what tells the UI that (see DebridClientUser.Premium).
            Expiration = stats?.PlanExpiresAt is { } expiresAt ? DateTimeOffset.FromUnixTimeSeconds(expiresAt) : null,
            Premium = stats?.Premium ?? false
        };
    }

    public async Task<String> AddTorrentMagnet(String magnetLink)
    {
        using var content = new MultipartFormDataContent
        {
            {
                new StringContent(magnetLink), "magnet"
            }
        };

        var result = await PostTransfer(content);

        return result?.Id ?? throw new InvalidOperationException("HIGHWAY API did not return a transfer id.");
    }

    public async Task<String> AddTorrentFile(Byte[] bytes)
    {
        using var content = BuildFileContent(bytes, "file.torrent");

        var result = await PostTransfer(content);

        return result?.Id ?? throw new InvalidOperationException("HIGHWAY API did not return a transfer id.");
    }

    public async Task<String> AddNzbFile(Byte[] bytes, String? name)
    {
        var fileName = String.IsNullOrWhiteSpace(name) ? "file.nzb" : name;

        if (!fileName.EndsWith(".nzb", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".nzb";
        }

        using var content = BuildFileContent(bytes, fileName);

        var result = await PostTransfer(content);

        return result?.Id ?? throw new InvalidOperationException("HIGHWAY API did not return a transfer id.");
    }

    public async Task<String> AddNzbLink(String nzbLink)
    {
        // transfers.php only accepts an uploaded .nzb, so fetch it ourselves first.
        var bytes = await DownloadNzb(nzbLink);

        String? name = null;

        try
        {
            name = Path.GetFileName(new Uri(nzbLink).AbsolutePath);
        }
        catch (UriFormatException)
        {
            // fall back to the default name
        }

        return await AddNzbFile(bytes, name);
    }

    public async Task<IList<DebridClientAvailableFile>> GetAvailableFiles(String hash)
    {
        // rdt-client only calls this with a torrent infohash (40- or 64-hex).
        if (String.IsNullOrWhiteSpace(hash) || !hash.All(Uri.IsHexDigit) || hash.Length is not (40 or 64))
        {
            return [];
        }

        HighwayCheckResponse? check = null;

        try
        {
            check = await CheckCache(hash, "torrent");
        }
        catch (JsonException)
        {
            // older server builds ignore ?check= and return the transfer list instead
        }

        if (check?.Results == null || !check.Results.TryGetValue(hash, out var entry) || !entry.Ready)
        {
            return [];
        }

        // The cache lookup itself has no file breakdown; if this user already linked the transfer, read it.
        if (String.IsNullOrWhiteSpace(entry.Id))
        {
            return [];
        }

        var transfer = await GetTransfer(entry.Id, "torrent");

        return (transfer?.Files ?? [])
               .Select(file => new DebridClientAvailableFile
               {
                   Filename = file.Path ?? "",
                   Filesize = file.Size
               })
               .Where(file => !String.IsNullOrWhiteSpace(file.Filename))
               .ToList();
    }

    public Task<Int32?> SelectFiles(Torrent torrent)
    {
        // Symlink mode needs named files[] entries to symlink out of the rclone mount, so wait/retry while
        // it's empty. Every other download client always fetches the whole transfer as one ZIP
        // (GetDownloadInfos below) regardless of files[], so don't block those on it.
        if (torrent.Files.Count == 0 && torrent.DownloadClient == Data.Enums.DownloadClient.Symlink)
        {
            return Task.FromResult<Int32?>(null);
        }

        return Task.FromResult<Int32?>(Math.Max(torrent.Files.Count, 1));
    }

    public async Task Delete(Torrent torrent)
    {
        if (String.IsNullOrWhiteSpace(torrent.RdId))
        {
            return;
        }

        var service = IsSpoolId(torrent.RdId) ? null : Service(torrent.Type);

        await DeleteTransfer(torrent.RdId, service);
    }

    public async Task<String> Unrestrict(Torrent torrent, String link)
    {
        if (String.IsNullOrWhiteSpace(link))
        {
            throw new ArgumentException("Link cannot be null or empty", nameof(link));
        }

        // Per-file downloads (non-symlink mode) already embed the real, signed download_url straight from
        // GetDownloadInfos - nothing to resolve (mirrors DebridLinkClient).
        if (!link.StartsWith(LinkScheme, StringComparison.Ordinal))
        {
            return link;
        }

        var (id, service, fileRef) = ParseLink(link);

        // Symlink mode resolves files straight from the rclone mount and never needs a real URL,
        // so leave the per-file sentinel untouched. Only the ZIP sentinel needs a signed URL.
        if (fileRef != "zip")
        {
            return link;
        }

        var transfer = await GetTransfer(id, service);

        if (transfer == null)
        {
            throw new($"HIGHWAY transfer {id} was not found. It was likely deleted or unlinked on HIGHWAY " +
                      "since this torrent was added here - remove it in rdt-client and re-add it.");
        }

        if (String.IsNullOrWhiteSpace(transfer.DownloadUrl))
        {
            throw new($"HIGHWAY did not return a download URL for transfer {id}. Signed URLs may be disabled on the server; " +
                      "use the Symlink download client with an rclone WebDAV mount instead.");
        }

        return transfer.DownloadUrl;
    }

    public async Task<Torrent> UpdateData(Torrent torrent, DebridClientTorrent? torrentClientTorrent)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(torrent.RdId))
            {
                return torrent;
            }

            var rdTorrent = torrentClientTorrent;

            if (rdTorrent == null)
            {
                var service = IsSpoolId(torrent.RdId) ? null : Service(torrent.Type);
                var transfer = await GetTransfer(torrent.RdId, service);

                if (transfer == null)
                {
                    torrent.RdStatusRaw = "deleted";

                    return torrent;
                }

                rdTorrent = Map(transfer);
            }

            if (!String.IsNullOrWhiteSpace(rdTorrent.Filename))
            {
                torrent.RdName = rdTorrent.Filename;
            }

            if (rdTorrent.Bytes > 0)
            {
                torrent.RdSize = rdTorrent.Bytes;
            }

            if (rdTorrent.Files != null)
            {
                torrent.RdFiles = JsonSerializer.Serialize(rdTorrent.Files);
            }

            torrent.ClientKind = Provider.HIGHWAY;
            torrent.RdHost = rdTorrent.Host; // content_path, needed by the Symlink downloader
            torrent.RdProgress = rdTorrent.Progress;
            torrent.RdAdded ??= DateTimeOffset.UtcNow;
            torrent.RdSpeed = rdTorrent.Speed;
            torrent.RdSeeders = rdTorrent.Seeders;
            torrent.RdStatusRaw = rdTorrent.Status;

            if (rdTorrent.Ended != null && torrent.RdEnded == null)
            {
                torrent.RdEnded = rdTorrent.Ended;
            }

            logger.LogTrace("Updating status for {TorrentName} from {OldStatus} to {NewStatus}", torrent.RdName, torrent.RdStatus, rdTorrent.Status);

            torrent.RdStatus = rdTorrent.Status switch
            {
                "queued" => TorrentStatus.Processing,
                "downloading" => TorrentStatus.Downloading,
                "completed" => TorrentStatus.Finished,
                "error" => TorrentStatus.Error,
                _ => torrent.RdStatus is null or TorrentStatus.Queued ? TorrentStatus.Processing : torrent.RdStatus.Value
            };

            if (rdTorrent.Status == "error" && !String.IsNullOrWhiteSpace(rdTorrent.Message))
            {
                torrent.Error = rdTorrent.Message;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update HIGHWAY transfer {RdId}: {Message}", torrent.RdId, ex.Message);

            throw;
        }

        return torrent;
    }

    public async Task<IList<DownloadInfo>?> GetDownloadInfos(Torrent torrent)
    {
        if (String.IsNullOrWhiteSpace(torrent.RdId) || IsSpoolId(torrent.RdId))
        {
            return null;
        }

        if (torrent.Files.Count == 0)
        {
            // Not indexed/visible on HIGHWAY yet - null (not []) so rdt-client retries next cycle
            // instead of marking the torrent as "all files excluded".
            return null;
        }

        var service = Service(torrent.Type);

        if (torrent.DownloadClient == Data.Enums.DownloadClient.Symlink)
        {
            logger.LogDebug("Resolving HIGHWAY transfer {RdName} via the rclone mount (symlink).", torrent.RdName);

            return torrent.Files
                          .Where(file => file.Selected && fileFilter.IsDownloadable(torrent, file.Path, file.Bytes))
                          .Select(file => new DownloadInfo
                          {
                              RestrictedLink = $"{LinkScheme}{torrent.RdId}/{service}/{file.Id}",
                              FileName = Path.GetFileName(file.Path)
                          })
                          .ToList();
        }

        var downloadableFiles = torrent.Files
                                       .Where(file => file.Selected && fileFilter.IsDownloadable(torrent, file.Path, file.Bytes))
                                       .ToList();

        if (downloadableFiles.Count == 0)
        {
            return [];
        }

        // Each file's own signed download_url lets us fetch it directly, respecting the usual
        // filters, with no ZIP/unzip step. Only fall back to the whole-folder ZIP when signed per-file URLs
        // aren't available server-side (same condition that leaves the top-level download_url null too).
        if (downloadableFiles.All(file => !String.IsNullOrWhiteSpace(file.DownloadLink)))
        {
            logger.LogDebug("Downloading HIGHWAY transfer {RdName} as individual files.", torrent.RdName);

            return downloadableFiles.Select(file => new DownloadInfo
                                    {
                                        RestrictedLink = file.DownloadLink!,
                                        FileName = Path.GetFileName(file.Path)
                                    })
                                    .ToList();
        }

        logger.LogDebug("Downloading HIGHWAY transfer {RdName} as a folder ZIP (no per-file signed URLs).", torrent.RdName);

        return
        [
            new()
            {
                RestrictedLink = $"{LinkScheme}{torrent.RdId}/{service}/zip",
                FileName = $"{torrent.RdName}.zip"
            }
        ];
    }

    public Task<String> GetFileName(Download download)
    {
        // FileName is always set in GetDownloadInfos.
        Debug.Assert(download.FileName != null);

        return Task.FromResult(download.FileName);
    }

    /// <summary>
    ///     Resolves the path of a file inside the rclone WebDAV mount. The mount root maps to HIGHWAY's DAV root,
    ///     <c>content_path</c> is <c>TORRENT/&lt;name&gt;</c> or <c>USENET/&lt;name&gt;</c> and each
    ///     <c>files[].path</c> already starts with <c>&lt;name&gt;/</c>, so the mount-relative path is
    ///     <c>&lt;service dir&gt;/&lt;files[].path&gt;</c>.
    /// </summary>
    public static String? GetSymlinkPath(Torrent torrent, Download download)
    {
        var fileName = DownloadHelper.GetFileName(download);

        if (String.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var match = torrent.Files.FirstOrDefault(f => !String.IsNullOrWhiteSpace(f.Path) && f.Path.EndsWith(fileName, StringComparison.Ordinal));
        var relativePath = match?.Path ?? fileName;

        var serviceDir = torrent.RdHost?.Split('/', '\\').FirstOrDefault(segment => !String.IsNullOrWhiteSpace(segment))
                         ?? (torrent.Type == DownloadType.Nzb ? "USENET" : "TORRENT");

        // If the stored file path already starts with the service dir, it is already mount-relative.
        if (relativePath.StartsWith(serviceDir + "/", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith(serviceDir + "\\", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        return Path.Combine(serviceDir, relativePath);
    }

    // ---- HTTP seams (overridable for tests) -------------------------------------------------------

    protected virtual Task<HighwayTransfer?> PostTransfer(HttpContent content)
    {
        return SendAsync<HighwayTransfer>(HttpMethod.Post, null, content);
    }

    protected virtual async Task<IList<HighwayTransfer>> ListTransfers()
    {
        var result = await SendAsync<HighwayTransferList>(HttpMethod.Get, null);

        return result?.Transfers ?? [];
    }

    protected virtual Task<HighwayTransfer?> GetTransfer(String id, String? service)
    {
        return SendAsync<HighwayTransfer>(HttpMethod.Get, BuildIdQuery(id, service));
    }

    protected virtual async Task<Boolean> DeleteTransfer(String id, String? service)
    {
        var result = await SendAsync<HighwayDeleteResult>(HttpMethod.Delete, BuildIdQuery(id, service));

        return result?.Removed ?? false;
    }

    protected virtual Task<HighwayStats?> GetStats()
    {
        return SendAsync<HighwayStats>(HttpMethod.Get, "stats=1");
    }

    protected virtual Task<HighwayCheckResponse?> CheckCache(String hashes, String service)
    {
        return SendAsync<HighwayCheckResponse>(HttpMethod.Get, $"check={Uri.EscapeDataString(hashes)}&service={service}");
    }

    protected virtual async Task<Byte[]> DownloadNzb(String url)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(settings.Current.Provider.Timeout);

        return await httpClient.GetByteArrayAsync(url);
    }

    // ---- internals ------------------------------------------------------------------------------

    private async Task<T?> SendAsync<T>(HttpMethod method, String? query, HttpContent? content = null)
    {
        var token = settings.Current.Provider.ApiKey;

        if (String.IsNullOrWhiteSpace(token))
        {
            throw new("HIGHWAY API token is not set (provider API Key).");
        }

        var url = $"{ApiBaseUrl}/center-api/transfers.php";

        if (!String.IsNullOrWhiteSpace(query))
        {
            url += "?" + query;
        }

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new("Bearer", token);
        request.Content = content;

        var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(settings.Current.Provider.Timeout);

        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(request);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "The connection to HIGHWAY timed out: {Message}", ex.Message);

            throw;
        }

        var body = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            var (error, message) = ParseError(body);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new RateLimitException(message ?? "HIGHWAY rate limit reached", TimeSpan.FromMinutes(2));
            }

            if (error is "invalid_token" or "authentication_required")
            {
                throw new($"HIGHWAY authentication failed ({error}). Check the API token in the provider settings. {message}");
            }

            if (error == "system_feature_unavailable")
            {
                throw new($"The HIGHWAY token does not belong to a high-way.me account ({error}). {message}");
            }

            throw new($"HIGHWAY request failed ({(Int32) response.StatusCode} {error}): {message ?? body}");
        }

        if (String.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private static (String? Error, String? Message) ParseError(String body)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(body);
            var error = element.TryGetProperty("error", out var e) ? e.GetString() : null;
            var message = element.TryGetProperty("message", out var m) ? m.GetString() : null;

            return (error, message);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static String BuildIdQuery(String id, String? service)
    {
        var query = $"id={Uri.EscapeDataString(id)}";

        if (!String.IsNullOrWhiteSpace(service))
        {
            query += $"&service={service}";
        }

        return query;
    }

    private static MultipartFormDataContent BuildFileContent(Byte[] bytes, String fileName)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new("application/octet-stream");

        return new()
        {
            {
                fileContent, "file", fileName
            }
        };
    }

    private static (String Id, String? Service, String FileRef) ParseLink(String link)
    {
        var rest = link.StartsWith(LinkScheme, StringComparison.Ordinal) ? link[LinkScheme.Length..] : link;
        var parts = rest.Split('/');

        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid HIGHWAY link format: {link}", nameof(link));
        }

        return (parts[0], String.IsNullOrWhiteSpace(parts[1]) ? null : parts[1], parts[2]);
    }

    private static String Service(DownloadType type)
    {
        return type == DownloadType.Nzb ? "usenet" : "torrent";
    }

    private static Boolean IsSpoolId(String id)
    {
        return id.Length == 32 && id.All(Uri.IsHexDigit);
    }

    private static DebridClientTorrent Map(HighwayTransfer transfer)
    {
        var type = transfer.Service == "usenet" ? DownloadType.Nzb : DownloadType.Torrent;

        Int64? speed = null;

        if (transfer.Eta is > 0 && transfer.Size > transfer.Downloaded && transfer.Downloaded >= 0)
        {
            speed = (transfer.Size - transfer.Downloaded) / transfer.Eta.Value;
        }

        return new()
        {
            Id = transfer.Id ?? "",
            Filename = transfer.Name ?? "",
            OriginalFilename = transfer.Name,
            Hash = transfer.Id ?? "",
            Bytes = transfer.Size,
            OriginalBytes = transfer.Size,
            Host = transfer.ContentPath,
            Split = 0,
            Progress = transfer.Progress,
            Status = transfer.State,
            Message = transfer.StateDetail,
            Type = type,
            Added = DateTimeOffset.UtcNow,
            Files = (transfer.Files ?? [])
                    .Select((file, index) => new DebridClientFile
                    {
                        Id = index,
                        Path = file.Path ?? "",
                        Bytes = file.Size,
                        Selected = !file.Partial,
                        DownloadLink = file.DownloadUrl
                    })
                    .ToList(),
            Links = [],
            Ended = transfer.State == "completed" ? DateTimeOffset.UtcNow : null,
            Speed = speed,
            Seeders = transfer.Seeder
        };
    }

    // ---- API DTOs (public so tests can stub the seams) -----------------------------------------

    public sealed class HighwayTransfer
    {
        [JsonPropertyName("id")]
        public String? Id { get; set; }

        [JsonPropertyName("service")]
        public String? Service { get; set; }

        [JsonPropertyName("name")]
        public String? Name { get; set; }

        [JsonPropertyName("state")]
        public String? State { get; set; }

        [JsonPropertyName("state_detail")]
        public String? StateDetail { get; set; }

        [JsonPropertyName("progress")]
        public Int64 Progress { get; set; }

        [JsonPropertyName("size")]
        public Int64 Size { get; set; }

        [JsonPropertyName("downloaded")]
        public Int64 Downloaded { get; set; }

        [JsonPropertyName("eta")]
        public Int64? Eta { get; set; }

        [JsonPropertyName("seeder")]
        public Int64 Seeder { get; set; }

        [JsonPropertyName("content_path")]
        public String? ContentPath { get; set; }

        [JsonPropertyName("download_url")]
        public String? DownloadUrl { get; set; }

        [JsonPropertyName("files")]
        public List<HighwayFile>? Files { get; set; }
    }

    public sealed class HighwayFile
    {
        [JsonPropertyName("path")]
        public String? Path { get; set; }

        [JsonPropertyName("size")]
        public Int64 Size { get; set; }

        [JsonPropertyName("partial")]
        public Boolean Partial { get; set; }

        /// <summary>Signed, credential-free direct link to this one file. Null if signed DAV URLs aren't configured server-side.</summary>
        [JsonPropertyName("download_url")]
        public String? DownloadUrl { get; set; }
    }

    public sealed class HighwayTransferList
    {
        [JsonPropertyName("transfers")]
        public List<HighwayTransfer>? Transfers { get; set; }
    }

    public sealed class HighwayDeleteResult
    {
        [JsonPropertyName("removed")]
        public Boolean Removed { get; set; }
    }

    /// <summary>Response of <c>?stats=1</c>. Only the bits rdt-client surfaces are modelled.</summary>
    public sealed class HighwayStats
    {
        [JsonPropertyName("username")]
        public String? Username { get; set; }

        [JsonPropertyName("plan")]
        public String? Plan { get; set; }

        [JsonPropertyName("plan_expires_at")]
        public Int64? PlanExpiresAt { get; set; }

        [JsonPropertyName("premium")]
        public Boolean Premium { get; set; }

        [JsonPropertyName("fair_use")]
        public Boolean FairUse { get; set; }

        [JsonPropertyName("storage")]
        public HighwayStorage? Storage { get; set; }
    }

    public sealed class HighwayStorage
    {
        [JsonPropertyName("limit")]
        public Int64 Limit { get; set; }

        [JsonPropertyName("used")]
        public Int64 Used { get; set; }

        [JsonPropertyName("available")]
        public Int64 Available { get; set; }
    }

    /// <summary>Response of <c>?check=&lt;hash&gt;</c>.</summary>
    public sealed class HighwayCheckResponse
    {
        [JsonPropertyName("service")]
        public String? Service { get; set; }

        [JsonPropertyName("results")]
        public Dictionary<String, HighwayCheckEntry>? Results { get; set; }
    }

    public sealed class HighwayCheckEntry
    {
        [JsonPropertyName("known")]
        public Boolean Known { get; set; }

        [JsonPropertyName("ready")]
        public Boolean Ready { get; set; }

        [JsonPropertyName("id")]
        public String? Id { get; set; }

        [JsonPropertyName("name")]
        public String? Name { get; set; }

        [JsonPropertyName("size")]
        public Int64 Size { get; set; }

        [JsonPropertyName("progress")]
        public Int64 Progress { get; set; }
    }
}

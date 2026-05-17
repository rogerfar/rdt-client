using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PremiumizeNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.DebridClient;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services.DebridClients;

public class PremiumizeDebridClient(ILogger<PremiumizeDebridClient> logger, IHttpClientFactory httpClientFactory, IDownloadableFileFilter fileFilter) : IDebridClient
{
    private const String TransferCreateUrl = "https://www.premiumize.me/api/transfer/create";

    public async Task<IList<DebridClientTorrent>> GetDownloads()
    {
        var results = await GetClient().Transfers.ListAsync();

        return results.Select(Map).ToList();
    }

    public async Task<DebridClientUser> GetUser()
    {
        var user = await GetClient().Account.InfoAsync() ?? throw new("Unable to get user");

        return new()
        {
            Username = user.CustomerId.ToString(),
            Expiration = user.PremiumUntil > 0 ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(user.PremiumUntil.Value) : null
        };
    }

    public async Task<String> AddTorrentMagnet(String magnetLink)
    {
        return await CreatePremiumizeNetTransfer(() => GetClient().Transfers.CreateAsync(magnetLink, ""), "magnet link");
    }

    public async Task<String> AddTorrentFile(Byte[] bytes)
    {
        return await CreatePremiumizeNetTransfer(() => GetClient().Transfers.CreateAsync(bytes, ""), "torrent file");
    }

    public async Task<String> AddNzbLink(String nzbLink)
    {
        return await CreatePremiumizeNetTransfer(() => GetClient().Transfers.CreateAsync(nzbLink, ""), "NZB link");
    }

    public async Task<String> AddNzbFile(Byte[] bytes, String? name)
    {
        return await CreateTransferFromNzbFile(bytes, GetNzbFileName(name));
    }

    public Task<IList<DebridClientAvailableFile>> GetAvailableFiles(String hash)
    {
        return Task.FromResult<IList<DebridClientAvailableFile>>([]);
    }

    /// <inheritdoc />
    public Task<Int32?> SelectFiles(Torrent torrent)
    {
        // torrent.Files is not populated when this function is called
        // by returning 1, we ensure no logic for "all files excluded" is followed
        return Task.FromResult<Int32?>(1);
    }

    public async Task Delete(Torrent torrent)
    {
        await GetClient().Transfers.DeleteAsync(torrent.RdId);
    }

    public Task<String> Unrestrict(Torrent torrent, String link)
    {
        return Task.FromResult(link);
    }

    public async Task<Torrent> UpdateData(Torrent torrent, DebridClientTorrent? torrentClientTorrent)
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

            downloadInfos.Add(new()
            {
                RestrictedLink = file.Link,
                FileName = file.Name
            });
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

    private PremiumizeNETClient GetClient()
    {
        try
        {
            var apiKey = GetApiKey();
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

    private static String GetApiKey()
    {
        var apiKey = Settings.Get.Provider.ApiKey;

        if (String.IsNullOrWhiteSpace(apiKey))
        {
            throw new("Premiumize API Key not set in the settings");
        }

        return apiKey;
    }

    private async Task<String> CreatePremiumizeNetTransfer(Func<Task<PremiumizeNET.TransferCreateResponse>> createTransfer, String description)
    {
        try
        {
            var result = await createTransfer();

            if (String.IsNullOrWhiteSpace(result?.Id))
            {
                throw new($"Unable to add {description}");
            }

            return result.Id;
        }
        catch (Exception ex) when (IsRateLimitMessage(ex.Message))
        {
            throw new RateLimitException(ex.Message, TimeSpan.FromMinutes(2));
        }
    }

    private async Task<String> CreateTransferFromNzbFile(Byte[] bytes, String fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        content.Add(fileContent, "src", fileName);

        return await CreateTransfer(content, "NZB file");
    }

    private static String GetNzbFileName(String? name)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            return "upload.nzb";
        }

        return name.EndsWith(".nzb", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.nzb";
    }

    private async Task<String> CreateTransfer(HttpContent content, String description)
    {
        try
        {
            using (content)
            {
                var httpClient = httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                using var request = new HttpRequestMessage(HttpMethod.Post, TransferCreateUrl)
                {
                    Content = content
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiKey());

                using var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(2);

                    throw new RateLimitException($"Unable to add {description}: Premiumize rate limit exceeded", retryAfter);
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new($"Unable to add {description}: Premiumize returned {(Int32)response.StatusCode} {response.ReasonPhrase}. {responseBody}");
                }

                var result = JsonConvert.DeserializeObject<RawTransferCreateResponse>(responseBody) ?? throw new($"Unable to add {description}: invalid Premiumize response");

                if (!String.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    var error = FormatPremiumizeError(result);

                    if (IsRateLimitMessage(error))
                    {
                        throw new RateLimitException(error, TimeSpan.FromMinutes(2));
                    }

                    throw new($"Unable to add {description}: {error}");
                }

                if (String.IsNullOrWhiteSpace(result.Id))
                {
                    throw new($"Unable to add {description}: Premiumize did not return a transfer id");
                }

                return result.Id;
            }
        }
        catch (RateLimitException)
        {
            throw;
        }
        catch (Exception ex) when (IsRateLimitMessage(ex.Message))
        {
            throw new RateLimitException(ex.Message, TimeSpan.FromMinutes(2));
        }
    }

    private static String FormatPremiumizeError(RawTransferCreateResponse result)
    {
        return String.Join(": ", new[]
        {
            result.Code,
            result.Message
        }.Where(m => !String.IsNullOrWhiteSpace(m)));
    }

    private static Boolean IsRateLimitMessage(String message)
    {
        return message.Contains("slow_down", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("rate limit exceeded", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("rate_limit_reached", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("account_limit_reached", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("service_limit_reached", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("service_down", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("semi_permanent_error", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("too many API requests", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("fair-use points", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("booster points", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("active-job count", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("usage limit for this service", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("target service is unreachable", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("retry after a delay", StringComparison.OrdinalIgnoreCase);
    }

    private static DebridClientTorrent Map(Transfer transfer)
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
            Progress = (Int64)((transfer.Progress ?? 1.0) * 100.0),
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

    private async Task<DebridClientTorrent> GetInfo(String id)
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

                downloadInfos.Add(new()
                {
                    RestrictedLink = item.Link,
                    FileName = item.Name
                });
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

    private class RawTransferCreateResponse
    {
        [JsonProperty("status")]
        public String? Status { get; set; }

        [JsonProperty("id")]
        public String? Id { get; set; }

        [JsonProperty("message")]
        public String? Message { get; set; }

        [JsonProperty("code")]
        public String? Code { get; set; }
    }
}

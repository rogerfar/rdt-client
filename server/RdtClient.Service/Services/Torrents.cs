using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MonoTorrent;
using System.Text.Json.Serialization;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Internal;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.TorrentClients;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Service.Services;

public class Torrents
{
    private static readonly SemaphoreSlim RealDebridUpdateLock = new(1, 1);

    private readonly ILogger<Torrents> _logger;
    private readonly TorrentData _torrentData;
    private readonly Downloads _downloads;

    private readonly AllDebridTorrentClient _allDebridTorrentClient;
    private readonly PremiumizeTorrentClient _premiumizeTorrentClient;
    private readonly RealDebridTorrentClient _realDebridTorrentClient;

    private ITorrentClient TorrentClient
    {
        get
        {
            return Settings.Get.Provider.Provider switch
            {
                Provider.Premiumize => _premiumizeTorrentClient,
                Provider.RealDebrid => _realDebridTorrentClient,
                Provider.AllDebrid => _allDebridTorrentClient,
                _ => throw new Exception("Invalid Provider")
            };
        }
    }

    private static readonly SemaphoreSlim TorrentResetLock = new(1, 1);

    public Torrents(ILogger<Torrents> logger,
                    TorrentData torrentData, 
                    Downloads downloads,
                    AllDebridTorrentClient allDebridTorrentClient,
                    PremiumizeTorrentClient premiumizeTorrentClient,
                    RealDebridTorrentClient realDebridTorrentClient)
    {
        _logger = logger;
        _torrentData = torrentData;
        _downloads = downloads;
        _allDebridTorrentClient = allDebridTorrentClient;
        _premiumizeTorrentClient = premiumizeTorrentClient;
        _realDebridTorrentClient = realDebridTorrentClient;
    }

    public async Task<IList<Torrent>> Get()
    {
        var torrents = await _torrentData.Get();

        foreach (var torrent in torrents)
        {
            foreach (var download in torrent.Downloads)
            {
                if (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
                {
                    download.Speed = downloadClient.Speed;
                    download.BytesTotal = downloadClient.BytesTotal;
                    download.BytesDone = downloadClient.BytesDone;
                }

                if (TorrentRunner.ActiveUnpackClients.TryGetValue(download.DownloadId, out var unpackClient))
                {
                    download.BytesTotal = 100;
                    download.BytesDone = unpackClient.Progess;
                }
            }
        }

        return torrents;
    }

    public async Task<Torrent?> GetByHash(String hash)
    {
        var torrent = await _torrentData.GetByHash(hash);

        if (torrent != null)
        {
            await UpdateTorrentClientData(torrent);
        }

        return torrent;
    }

    public async Task UpdateCategory(String hash, String? category)
    {
        var torrent = await _torrentData.GetByHash(hash);

        if (torrent == null)
        {
            return;
        }

        Log($"Update category to {category}", torrent);

        await _torrentData.UpdateCategory(torrent.TorrentId, category);
    }

    public async Task<Torrent> UploadMagnet(String magnetLink, Torrent torrent)
    {
        MagnetLink magnet;

        try
        {
            magnet = MagnetLink.Parse(magnetLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ex.Message}, trying to parse {magnetLink}", ex.Message, magnetLink);
            throw new Exception($"{ex.Message}, trying to parse {magnetLink}");
        }

        var id = await TorrentClient.AddMagnet(magnetLink);

        var hash = magnet.InfoHash.ToHex();

        var newTorrent = await Add(id, hash, magnetLink, false, torrent);

        Log($"Adding {hash} magnet link {magnetLink}", newTorrent);

        return newTorrent;
    }

    public async Task<Torrent> UploadFile(Byte[] bytes, Torrent torrent)
    {
        MonoTorrent.Torrent monoTorrent;

        var fileAsBase64 = Convert.ToBase64String(bytes);

        try
        {
            monoTorrent = await MonoTorrent.Torrent.LoadAsync(bytes);
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message}, trying to parse {fileAsBase64}");
        }

        var id = await TorrentClient.AddFile(bytes);

        var hash = monoTorrent.InfoHash.ToHex();

        var newTorrent = await Add(id, hash, fileAsBase64, true, torrent);

        Log($"Adding {hash} torrent file {fileAsBase64}", newTorrent);

        return newTorrent;
    }

    public async Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
    {
        var result = await TorrentClient.GetAvailableFiles(hash);

        return result;
    }

    public async Task SelectFiles(Guid torrentId)
    {
        var torrent = await GetById(torrentId);

        if (torrent == null)
        {
            return;
        }

        await TorrentClient.SelectFiles(torrent);
    }

    public async Task CreateDownloads(Guid torrentId)
    {
        var torrent = await GetById(torrentId);

        if (torrent == null)
        {
            return;
        }

        var downloadLinks = await TorrentClient.GetDownloadLinks(torrent);

        if (downloadLinks == null)
        {
            return;
        }

        foreach (var downloadLink in downloadLinks)
        {
            // Make sure downloads don't get added multiple times
            var downloadExists = await _downloads.Get(torrent.TorrentId, downloadLink);

            if (downloadExists == null && !String.IsNullOrWhiteSpace(downloadLink))
            {
                await _downloads.Add(torrent.TorrentId, downloadLink);
            }
        }
    }

    public async Task Delete(Guid torrentId, Boolean deleteData, Boolean deleteRdTorrent, Boolean deleteLocalFiles)
    {
        var torrent = await GetById(torrentId);

        if (torrent == null)
        {
            return;
        }

        Log($"Deleting", torrent);

        await UpdateComplete(torrentId, "Torrent deleted", DateTimeOffset.UtcNow, false);

        foreach (var download in torrent.Downloads)
        {
            var retry = 10;

            while (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
            {
                Log($"Cancelling download", download, torrent);

                await downloadClient.Cancel();

                await Task.Delay(500);

                retry++;

                if (retry > 5)
                {
                    break;
                }
            }

            retry = 10;

            while (TorrentRunner.ActiveUnpackClients.TryGetValue(download.DownloadId, out var unpackClient))
            {
                Log($"Cancelling unpack", download, torrent);

                unpackClient.Cancel();

                await Task.Delay(500);

                retry++;

                if (retry > 10)
                {
                    break;
                }
            }
        }

        if (deleteData)
        {
            Log($"Deleting RdtClient data", torrent);

            await _downloads.DeleteForTorrent(torrent.TorrentId);
            await _torrentData.Delete(torrentId);
        }

        if (deleteRdTorrent && torrent.RdId != null)
        {
            Log($"Deleting RealDebrid Torrent", torrent);

            try
            {
                await TorrentClient.Delete(torrent.RdId);
            }
            catch
            {
                // ignored
            }
        }

        if (deleteLocalFiles && !String.IsNullOrWhiteSpace(torrent.RdName))
        {
            var downloadPath = DownloadPath(torrent);
            downloadPath = Path.Combine(downloadPath, torrent.RdName);

            Log($"Deleting local files in {downloadPath}", torrent);

            if (Directory.Exists(downloadPath))
            {
                var retry = 0;

                while (true)
                {
                    try
                    {
                        Directory.Delete(downloadPath, true);

                        break;
                    }
                    catch
                    {
                        retry++;

                        if (retry >= 3)
                        {
                            throw;
                        }

                        await Task.Delay(1000);
                    }
                }
            }
        }
    }

    public async Task<String> UnrestrictLink(Guid downloadId)
    {
        var download = await _downloads.GetById(downloadId);

        if (download == null)
        {
            throw new Exception($"Download with ID {downloadId} not found");
        }

        Log($"Unrestricting link", download, download.Torrent);

        var unrestrictedLink = await TorrentClient.Unrestrict(download.Path);

        await _downloads.UpdateUnrestrictedLink(downloadId, unrestrictedLink);

        return unrestrictedLink;
    }

    public async Task<Profile> GetProfile()
    {
        var user = await TorrentClient.GetUser();

        var profile = new Profile
        {
            Provider = Enum.GetName(Settings.Get.Provider.Provider),
            UserName = user.Username,
            Expiration = user.Expiration,
            CurrentVersion = UpdateChecker.CurrentVersion,
            LatestVersion = UpdateChecker.LatestVersion
        };

        return profile;
    }

    public async Task UpdateRdData()
    {
        await RealDebridUpdateLock.WaitAsync();

        var torrents = await Get();

        try
        {
            var rdTorrents = await TorrentClient.GetTorrents();

            foreach (var rdTorrent in rdTorrents)
            {
                var torrent = torrents.FirstOrDefault(m => m.RdId == rdTorrent.Id);

                // Auto import torrents only torrents that have their files selected
                if (torrent == null && Settings.Get.Provider.AutoImport)
                {
                    var newTorrent = new Torrent
                    {
                        Category = Settings.Get.Provider.Default.Category,
                        DownloadAction = Settings.Get.Provider.Default.OnlyDownloadAvailableFiles ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll,
                        FinishedAction = Settings.Get.Provider.Default.FinishedAction,
                        DownloadMinSize = Settings.Get.Provider.Default.MinFileSize,
                        TorrentRetryAttempts = Settings.Get.Provider.Default.TorrentRetryAttempts,
                        DownloadRetryAttempts = Settings.Get.Provider.Default.DownloadRetryAttempts,
                        DeleteOnError = Settings.Get.Provider.Default.DeleteOnError,
                        Lifetime = Settings.Get.Provider.Default.TorrentLifetime,
                        Priority = Settings.Get.Provider.Default.Priority > 0 ? Settings.Get.Provider.Default.Priority : null,
                        RdId = rdTorrent.Id
                    };

                    if (newTorrent.RdStatus == TorrentStatus.WaitingForFileSelection)
                    {
                        continue;
                    }

                    torrent = await _torrentData.Add(rdTorrent.Id, rdTorrent.Hash, null, false, newTorrent);

                    await UpdateTorrentClientData(torrent, rdTorrent);
                }
                else if (torrent != null)
                {
                    await UpdateTorrentClientData(torrent, rdTorrent);
                }
            }

            foreach (var torrent in torrents)
            {
                var rdTorrent = rdTorrents.FirstOrDefault(m => m.Id == torrent.RdId);

                if (rdTorrent == null && Settings.Get.Provider.AutoDelete)
                {
                    await Delete(torrent.TorrentId, true, false, true);
                }
            }
        }
        finally
        {
            RealDebridUpdateLock.Release();
        }
    }

    public async Task RetryTorrent(Guid torrentId, Int32 retryCount)
    {
        await TorrentResetLock.WaitAsync();

        try
        {
            var torrent = await _torrentData.GetById(torrentId);

            if (torrent?.Retry == null)
            {
                return;
            }

            Log($"Retrying Torrent", torrent);

            await UpdateComplete(torrent.TorrentId, "Retrying Torrent", DateTimeOffset.UtcNow, false);
            await UpdateRetry(torrent.TorrentId, null, 0);

            foreach (var download in torrent.Downloads)
            {
                await _downloads.UpdateError(download.DownloadId, null);
                await _downloads.UpdateCompleted(download.DownloadId, DateTimeOffset.UtcNow);
            }

            foreach (var download in torrent.Downloads)
            {
                while (TorrentRunner.ActiveDownloadClients.TryRemove(download.DownloadId, out var downloadClient))
                {
                    await downloadClient.Cancel();

                    await Task.Delay(100);
                }

                while (TorrentRunner.ActiveUnpackClients.TryRemove(download.DownloadId, out var unpackClient))
                {
                    unpackClient.Cancel();

                    await Task.Delay(100);
                }
            }

            await Delete(torrentId, true, true, true);

            if (String.IsNullOrWhiteSpace(torrent.FileOrMagnet))
            {
                throw new Exception($"Cannot re-add this torrent, original magnet or file not found");
            }

            Torrent newTorrent;

            if (torrent.IsFile)
            {
                var bytes = Convert.FromBase64String(torrent.FileOrMagnet);

                newTorrent = await UploadFile(bytes, torrent);
            }
            else
            {
                newTorrent = await UploadMagnet(torrent.FileOrMagnet, torrent);
            }

            await _torrentData.UpdateRetry(newTorrent.TorrentId, null, retryCount);
        }
        finally
        {
            TorrentResetLock.Release();
        }
    }

    public async Task RetryDownload(Guid downloadId)
    {
        var download = await _downloads.GetById(downloadId);

        if (download == null)
        {
            return;
        }

        Log($"Retrying Download", download, download.Torrent);

        while (TorrentRunner.ActiveDownloadClients.TryRemove(download.DownloadId, out var downloadClient))
        {
            await downloadClient.Cancel();

            await Task.Delay(100);
        }

        while (TorrentRunner.ActiveUnpackClients.TryRemove(download.DownloadId, out var unpackClient))
        {
            unpackClient.Cancel();

            await Task.Delay(100);
        }

        var downloadPath = DownloadPath(download.Torrent!);
            
        var filePath = DownloadHelper.GetDownloadPath(downloadPath, download.Torrent!, download);

        if (filePath != null)
        {
            Log($"Deleting {filePath}", download, download.Torrent);

            await FileHelper.Delete(filePath);
        }

        Log($"Resetting", download, download.Torrent);

        await _downloads.Reset(downloadId);

        await _torrentData.UpdateComplete(download.TorrentId, null, null, false);
    }
        
    public async Task UpdateComplete(Guid torrentId, String? error, DateTimeOffset datetime, Boolean retry)
    {
        await _torrentData.UpdateComplete(torrentId, error, datetime, retry);
    }

    public async Task UpdateFilesSelected(Guid torrentId, DateTimeOffset datetime)
    {
        await _torrentData.UpdateFilesSelected(torrentId, datetime);
    }

    public async Task UpdatePriority(String hash, Int32 priority)
    {
        var torrent = await _torrentData.GetByHash(hash);

        if (torrent == null)
        {
            return;
        }

        await _torrentData.UpdatePriority(torrent.TorrentId, priority);
    }

    public async Task UpdateRetry(Guid torrentId, DateTimeOffset? datetime, Int32 retry)
    {
        await _torrentData.UpdateRetry(torrentId, datetime, retry);
    }

    public async Task UpdateError(Guid torrentId, String error)
    {
        await _torrentData.UpdateError(torrentId, error);
    }

    public async Task<Torrent?> GetById(Guid torrentId)
    {
        var torrent = await _torrentData.GetById(torrentId);

        if (torrent == null)
        {
            return null;
        }

        await UpdateTorrentClientData(torrent);

        foreach (var download in torrent.Downloads)
        {
            if (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
            {
                download.Speed = downloadClient.Speed;
                download.BytesTotal = downloadClient.BytesTotal;
                download.BytesDone = downloadClient.BytesDone;
            }

            if (TorrentRunner.ActiveUnpackClients.TryGetValue(download.DownloadId, out var unpackClient))
            {
                download.BytesTotal = 100;
                download.BytesDone = unpackClient.Progess;
            }
        }

        return torrent;
    }

    private static String DownloadPath(Torrent torrent)
    {
        var settingDownloadPath = Settings.Get.DownloadClient.DownloadPath;

        if (!String.IsNullOrWhiteSpace(torrent.Category))
        {
            settingDownloadPath = Path.Combine(settingDownloadPath, torrent.Category);
        }

        return settingDownloadPath;
    }

    private async Task<Torrent> Add(String rdTorrentId,
                                    String infoHash,
                                    String fileOrMagnetContents,
                                    Boolean isFile,
                                    Torrent torrent)
    {
        await RealDebridUpdateLock.WaitAsync();
            
        try
        {
            var existingTorrent = await _torrentData.GetByHash(infoHash);

            if (existingTorrent != null)
            {
                return existingTorrent;
            }

            var newTorrent = await _torrentData.Add(rdTorrentId,
                                                    infoHash,
                                                    fileOrMagnetContents,
                                                    isFile,
                                                    torrent);

            await UpdateTorrentClientData(newTorrent);

            return newTorrent;
        }
        finally
        {
            RealDebridUpdateLock.Release();
        }
    }

    public async Task Update(Torrent torrent)
    {
        await _torrentData.Update(torrent);
    }

    public async Task RunTorrentComplete(Guid torrentId)
    {
        if (String.IsNullOrWhiteSpace(Settings.Get.General.RunOnTorrentCompleteFileName))
        {
            return;
        }

        var torrent = await _torrentData.GetById(torrentId);

        if (torrent == null)
        {
            throw new Exception($"Cannot find Torrent with ID {torrentId}");
        }

        var downloads = await _downloads.GetForTorrent(torrentId);

        var fileName = Settings.Get.General.RunOnTorrentCompleteFileName;
        var arguments = Settings.Get.General.RunOnTorrentCompleteArguments ?? "";

        Log($"Parsing external program {fileName} with arguments {arguments}", torrent);

        var downloadPath = DownloadPath(torrent);
        var torrentPath = Path.Combine(downloadPath, torrent.RdName ?? "Unknown");

        arguments = arguments.Replace("%N", $"\"{torrent.RdName}\"");
        arguments = arguments.Replace("%L", $"\"{torrent.Category}\"");
        arguments = arguments.Replace("%F", $"\"{torrentPath}\"");
        arguments = arguments.Replace("%R", $"\"{downloadPath}\"");
        arguments = arguments.Replace("%D", $"\"{torrentPath}\"");
        arguments = arguments.Replace("%C", downloads.Count.ToString(CultureInfo.InvariantCulture).Replace(",", "").Replace(".", ""));
        arguments = arguments.Replace("%Z", torrent.RdSize?.ToString(CultureInfo.InvariantCulture).Replace(",", "").Replace(".", ""));
        arguments = arguments.Replace("%I", torrent.Hash);

        Log($"Executing external program {fileName} with arguments {arguments}", torrent);

        var errorSb = new StringBuilder();
        var outputSb = new StringBuilder();

        using var process = new Process();
            
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += (_, data) =>
        {
            if (data.Data == null)
            {
                return;
            }

            outputSb.AppendLine(data.Data.Trim());
        };
        process.ErrorDataReceived += (_, data) =>
        {
            if (data.Data == null)
            {
                return;
            }

            errorSb.AppendLine(data.Data.Trim());
        };
            
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var exited = process.WaitForExit(60000 * 10);

        var errors = errorSb.ToString();
        var output = outputSb.ToString();

        if (errors.Length > 0)
        {
            Log($"External application exited with errors: {errors}", torrent);
        }

        if (output.Length > 0)
        {
            Log($"External application exited with output: {output}", torrent);
        }

        if (!exited)
        {
            Log("External application after a 60 second timeout", torrent);
        }
    }

    private async Task UpdateTorrentClientData(Torrent torrent, TorrentClientTorrent? torrentClientTorrent = null)
    {
        try
        {
            var originalTorrent = JsonSerializer.Serialize(torrent,
                                                           new JsonSerializerOptions
                                                           {
                                                               ReferenceHandler = ReferenceHandler.IgnoreCycles
                                                           });

            await TorrentClient.UpdateData(torrent, torrentClientTorrent);

            var newTorrent = JsonSerializer.Serialize(torrent,
                                                      new JsonSerializerOptions
                                                      {
                                                          ReferenceHandler = ReferenceHandler.IgnoreCycles
                                                      });

            if (originalTorrent != newTorrent)
            {
                await _torrentData.UpdateRdData(torrent);
            }
        }
        catch
        {
            // ignored
        }
    }

    private void Log(String message, Data.Models.Data.Download? download, Torrent? torrent)
    {
        if (download != null)
        {
            message = $"{message} {download.ToLog()}";
        }

        if (torrent != null)
        {
            message = $"{message} {torrent.ToLog()}";
        }

        _logger.LogDebug(message);
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
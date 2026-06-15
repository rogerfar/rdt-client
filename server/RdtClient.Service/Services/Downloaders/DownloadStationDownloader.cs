using System.IO.Abstractions;
using RdtClient.Service.Helpers;
using Serilog;
using Synology.Api.Client;
using Synology.Api.Client.Apis.DownloadStation.Info.Models;
using Synology.Api.Client.Apis.DownloadStation.Task.Models;
using Synology.Api.Client.Exceptions;

namespace RdtClient.Service.Services.Downloaders;

internal class DelayProvider : IDelayProvider
{
    public Task Delay(Int32 delay)
    {
        return Task.Delay(delay);
    }
}

public class DownloadStationDownloader : IDownloader
{
    private const Int32 RetryCount = 5;

    private readonly IDelayProvider _delayProvider;
    private readonly IFileSystem _fileSystem;
    private readonly String _filePath;

    private readonly ILogger _logger;
    private readonly String? _remotePath;

    private readonly ISynologyClient _synologyClient;
    private readonly String _uri;

    private String? _gid;

    public DownloadStationDownloader(String? gid,
                                     String uri,
                                     String? remotePath,
                                     String filePath,
                                     String downloadPath,
                                     ISynologyClient synologyClient,
                                     IDelayProvider? delayProvider = null,
                                     IFileSystem? fileSystem = null)
    {
        _logger = Log.ForContext<DownloadStationDownloader>();
        _logger.Debug($"Instantiated new DownloadStation Downloader for URI {uri} to filePath {filePath} (remote {remotePath}) and downloadPath {downloadPath} and GID {gid}");

        _gid = gid;
        _filePath = filePath;
        _uri = uri;
        _remotePath = remotePath;
        _synologyClient = synologyClient;
        _delayProvider = delayProvider ?? new DelayProvider();
        _fileSystem = fileSystem ?? new FileSystem();
    }

    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    public async Task Cancel()
    {
        // Resolve the gid from the task list in case a task was created but its id was never persisted (orphan cleanup).
        _gid ??= await GetGidFromUri();

        if (_gid == null)
        {
            return;
        }

        _logger.Debug($"Remove download {_uri} {_gid} from Synology DownloadStation");

        try
        {
            await _synologyClient.DownloadStationApi()
                                 .TaskEndpoint()
                                 .DeleteAsync(new()
                                 {
                                     Ids =
                                     [
                                         _gid
                                     ],
                                     ForceComplete = false
                                 });
        }
        catch (Exception ex)
        {
            // The Synology DELETE response can itself fail to deserialize (upstream #792); don't let cleanup throw.
            _logger.Debug($"Failed to remove DownloadStation task {_gid}: {ex.Message}");
        }
    }

    public async Task<String> Download()
    {
        var path = Path.GetDirectoryName(_remotePath)?.Replace('\\', '/') ?? throw new($"Invalid file path {_filePath}");

        if (!path.StartsWith('/'))
        {
            path = '/' + path;
        }

        _logger.Debug($"Starting download of {_uri}, writing to path: {path}");

        if (_gid != null)
        {
            var task = await GetTask();

            if (task != null)
            {
                // A task for this download already exists in DownloadStation; adopt it instead of throwing.
                // Throwing here permanently bricks every retry that reuses the gid: the caller Cancel()s first,
                // but the DownloadStation delete response fails to deserialize (upstream #792) so the task is not
                // actually removed, and the next Download() then errors with "already added" forever.
                _logger.Debug($"Download with ID {_gid} already exists in DownloadStation; reusing it");

                return _gid;
            }
        }

        var retryCount = 0;

        while (retryCount < RetryCount)
        {
            _gid = await GetGidFromUri();

            if (_gid != null)
            {
                _logger.Debug($"Download with ID {_gid} found in DownloadStation");

                return _gid;
            }

            try
            {
                // DownloadStation will not create the destination folder for a direct-file task, so create it first.
                // `path` is the share-relative destination folder with a leading slash, e.g. /Media/Downloads/Torrents/<name>.
                await EnsureRemoteFolderExists(path);

                var createResult = await _synologyClient
                                         .DownloadStationApi()
                                         .TaskEndpoint()
                                         .CreateAsync(new(_uri, path[1..]));

                _gid = createResult.TaskId?.FirstOrDefault();
                _logger.Debug($"Added download to DownloadStation, received ID {_gid}");

                // Only fall back to a list lookup when create returned no id; GetGidFromUri is guarded so it can't erase a known id.
                _gid ??= await GetGidFromUri();

                if (_gid != null)
                {
                    _logger.Debug($"Download with ID {_gid} found in DownloadStation");

                    return _gid;
                }

                retryCount++;
                _logger.Error($"Task not found in DownloadStation after a successful create. Retrying {retryCount}/{RetryCount}");
                await _delayProvider.Delay(retryCount * 1000);
            }
            catch (Exception e)
            {
                retryCount++;
                _logger.Error($"Error starting download: {e.Message}. Retrying {retryCount}/{RetryCount}");
                await _delayProvider.Delay(retryCount * 1000);
            }
        }

        throw new($"Unable to download file");
    }

    public async Task Pause()
    {
        _logger.Debug($"Pausing download {_uri} {_gid}");

        if (_gid == null)
        {
            return;
        }

        try
        {
            await _synologyClient.DownloadStationApi().TaskEndpoint().PauseAsync(_gid);
        }
        catch (Exception ex)
        {
            _logger.Debug($"Failed to pause DownloadStation task {_gid}: {ex.Message}");
        }
    }

    public async Task Resume()
    {
        _logger.Debug($"Resuming download {_uri} {_gid}");

        if (_gid == null)
        {
            return;
        }

        try
        {
            await _synologyClient.DownloadStationApi().TaskEndpoint().ResumeAsync(_gid);
        }
        catch (Exception ex)
        {
            _logger.Debug($"Failed to resume DownloadStation task {_gid}: {ex.Message}");
        }
    }

    public static async Task<DownloadStationDownloader> Init(String? gid, String uri, String filePath, String downloadPath, String? category)
    {
        if (Settings.Get.DownloadClient.DownloadStationUrl == null)
        {
            throw new("No URL specified for Synology download station");
        }

        if (Settings.Get.DownloadClient.DownloadStationUsername == null || Settings.Get.DownloadClient.DownloadStationPassword == null)
        {
            throw new("No username/password specified for Synology download station");
        }

        var synologyClient = new SynologyClient(Settings.Get.DownloadClient.DownloadStationUrl);

        try
        {
            await synologyClient.LoginAsync(Settings.Get.DownloadClient.DownloadStationUsername, Settings.Get.DownloadClient.DownloadStationPassword);
        }
        catch (Exception ex)
        {
            throw new($"DownloadStation login failed for user '{Settings.Get.DownloadClient.DownloadStationUsername}': {ex.Message}. " +
                      $"If 2-step verification is enabled on this Synology account, use a dedicated service account without 2FA.");
        }

        String? remotePath = null;
        String? rootPath;

        if (!String.IsNullOrWhiteSpace(Settings.Get.DownloadClient.DownloadStationDownloadPath))
        {
            rootPath = Settings.Get.DownloadClient.DownloadStationDownloadPath;

            // DownloadStation resolves a per-account default destination and leaves every task in "Waiting" if the
            // account has none. Make sure this account has one (set to the configured root) so downloads can start.
            await EnsureDefaultDestination(synologyClient, rootPath);
        }
        else
        {
            var config = await synologyClient.DownloadStationApi().InfoEndpoint().GetConfigAsync();
            rootPath = config.DefaultDestination;
        }

        if (rootPath != null)
        {
            if (String.IsNullOrWhiteSpace(category))
            {
                remotePath = Path.Combine(rootPath, downloadPath).Replace('\\', '/');
            }
            else
            {
                remotePath = Path.Combine(rootPath, category, downloadPath).Replace('\\', '/');
            }
        }

        return new(gid, uri, remotePath, filePath, downloadPath, synologyClient);
    }

    /// <summary>
    ///     DownloadStation resolves a per-account "default destination"; without one it leaves every task in "Waiting"
    ///     (DSM logs "Failed to get default download destination of user"). Set it for this account when it has none,
    ///     best-effort and preserving the other server settings, so downloads start without a manual setup step.
    /// </summary>
    private static async Task EnsureDefaultDestination(ISynologyClient synologyClient, String destination)
    {
        var logger = Log.ForContext<DownloadStationDownloader>();

        try
        {
            var config = await synologyClient.DownloadStationApi().InfoEndpoint().GetConfigAsync();

            if (!String.IsNullOrWhiteSpace(config.DefaultDestination))
            {
                return;
            }

            // Re-send the existing config unchanged except for the default destination, so nothing else is reset.
            var updated = new DownloadStationServerConfig(config.BtMaxDownloadSpeed,
                                                          config.BtMaxUploadSpeed,
                                                          config.EmulEnable,
                                                          config.EmulMaxDownloadSpeed,
                                                          config.EmulMaxUploadSpeed,
                                                          config.FtpMaxDownloadSpeed,
                                                          config.HttpMaxDownloadSpeed,
                                                          config.NzbMaxDownloadSpeed,
                                                          config.UnzipServiceEnable,
                                                          destination,
                                                          config.EmulDefaultDestination);

            await synologyClient.DownloadStationApi().InfoEndpoint().SetServerConfigAsync(updated);

            logger.Information($"Set the DownloadStation default destination to '{destination}' for this account (it had none).");
        }
        catch (Exception ex)
        {
            logger.Warning($"Could not set the DownloadStation default destination automatically ({ex.Message}). " +
                           $"If downloads stay in 'Waiting', sign into Download Station as this account and set a Default destination " +
                           $"under Settings -> BT/HTTP/FTP/NZB -> Location.");
        }
    }

    /// <summary>
    ///     Ensures the destination folder exists on the Synology before a task is created. DownloadStation does not create
    ///     the destination for a direct-file download task, so a missing folder yields "Destination does not exist".
    /// </summary>
    private async Task EnsureRemoteFolderExists(String folderPath)
    {
        try
        {
            await _synologyClient.FileStationApi()
                                 .CreateFolderEndpoint()
                                 .CreateAsync([folderPath], true);

            _logger.Debug($"Ensured DownloadStation destination folder exists: {folderPath}");
        }
        catch (SynologyApiException ex)
        {
            // With force_parent (createParentFolders), creating a folder that already exists returns success, so an
            // exception here means the folder could NOT be created — most commonly because the Synology account lacks
            // File Station permission or write access to the destination. Surface it clearly: the task create below
            // will then fail with "Destination does not exist", and this warning explains why.
            _logger.Warning($"Could not create the DownloadStation destination folder '{folderPath}' " +
                            $"(File Station error {ex.ErrorCode}: {ex.ErrorDescription}). The Synology account must have " +
                            $"File Station permission with read/write access to that path.");
        }
    }

    private async Task<String?> GetGidFromUri()
    {
        try
        {
            var tasks = await _synologyClient.DownloadStationApi().TaskEndpoint().ListAsync();

            return tasks.Task?.FirstOrDefault(t => t.Additional?.Detail?.Uri == _uri)?.Id;
        }
        catch (Exception ex)
        {
            // The task-list response can fail to deserialize when a task carries a non-string error_detail (upstream #723);
            // treat that as "not found" instead of aborting the whole download.
            _logger.Debug($"Failed to list DownloadStation tasks for {_uri}: {ex.Message}");

            return null;
        }
    }

    public async Task Update()
    {
        if (_gid == null)
        {
            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = "Task not found"
                                     });

            return;
        }

        var task = await GetTask();

        if (task == null)
        {
            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = "Task not found"
                                     });

            return;
        }

        // DownloadStation reports failures via status_extra.error_detail (the status enum has no error member),
        // and stalls awaiting input on CaptchaNeeded. Surface both instead of polling progress forever.
        if (!String.IsNullOrWhiteSpace(task.StatusExtra?.ErrorDetail) || task.Status == DownloadStationTaskStatus.CaptchaNeeded)
        {
            var detail = !String.IsNullOrWhiteSpace(task.StatusExtra?.ErrorDetail) ? task.StatusExtra!.ErrorDetail : task.Status.ToString();

            await Cancel();

            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = $"DownloadStation error: {detail}"
                                     });

            return;
        }

        if (IsComplete(task))
        {
            await CompleteWhenFileExists();

            return;
        }

        DownloadProgress?.Invoke(this,
                                 new()
                                 {
                                     BytesDone = task.Additional?.Transfer?.SizeDownloaded ?? 0,
                                     BytesTotal = task.Size,
                                     Speed = task.Additional?.Transfer?.SpeedDownload ?? 0
                                 });
    }

    /// <summary>
    ///     A DownloadStation task is done downloading in several terminal states (it is not always <c>Finished</c> — a
    ///     completed torrent settles into <c>Seeding</c>/<c>Downloaded</c>). Also treat fully-transferred tasks as complete.
    /// </summary>
    private static Boolean IsComplete(DownloadStationTask task)
    {
        if (task.Status is DownloadStationTaskStatus.Finished
                        or DownloadStationTaskStatus.Downloaded
                        or DownloadStationTaskStatus.Seeding
                        or DownloadStationTaskStatus.PreSeeding)
        {
            return true;
        }

        return task.Size > 0 && (task.Additional?.Transfer?.SizeDownloaded ?? 0) >= task.Size;
    }

    /// <summary>
    ///     DownloadStation finishing only means the file is on the Synology; verify it is visible at the container path
    ///     (the bind-mounted location rdt-client unpacks/imports from) before signalling success, mirroring the Aria2c
    ///     downloader. A missing file means the path mapping is wrong, which is otherwise a silent import failure.
    /// </summary>
    private async Task CompleteWhenFileExists()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (_fileSystem.File.Exists(_filePath))
            {
                DownloadComplete?.Invoke(this, new());

                return;
            }

            await _delayProvider.Delay(attempt * 1000);
        }

        DownloadComplete?.Invoke(this,
                                 new()
                                 {
                                     Error = $"DownloadStation reported the download finished, but no file was found at {_filePath} " +
                                             $"(DownloadStation saved to {_remotePath}). Check that the Download path, the DownloadStation Download Path, " +
                                             $"and the container bind mount all point at the same folder."
                                 });
    }

    private async Task<DownloadStationTask?> GetTask()
    {
        try
        {
            if (_gid == null)
            {
                return null;
            }

            return await _synologyClient.DownloadStationApi().TaskEndpoint().GetInfoAsync(_gid);
        }
        catch (Exception ex)
        {
            // GetInfo can throw on an empty/absent task or on the #723 error_detail deserialization; treat as "no task".
            _logger.Debug($"Failed to get DownloadStation task {_gid}: {ex.Message}");

            return null;
        }
    }
}

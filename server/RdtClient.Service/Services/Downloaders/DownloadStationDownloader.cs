using RdtClient.Service.Helpers;
using Serilog;
using Synology.Api.Client;
using Synology.Api.Client.Apis.DownloadStation.Task.Models;

namespace RdtClient.Service.Services.Downloaders;

class DelayProvider : IDelayProvider
{
    public Task Delay(Int32 delay)
    {
        return Task.Delay(delay);
    }
}

public class DownloadStationDownloader : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private const Int32 RetryCount = 5;

    private readonly ISynologyClient _synologyClient;

    private readonly ILogger _logger;
    private readonly String _filePath;
    private readonly String _uri;
    private readonly String? _remotePath;
    private readonly IDelayProvider _delayProvider;

    private String? _gid;

    public DownloadStationDownloader(String? gid, String uri, String? remotePath, String filePath, String downloadPath, ISynologyClient synologyClient, IDelayProvider? delayProvider = null)
    {
        _logger = Log.ForContext<DownloadStationDownloader>();
        _logger.Debug($"Instantiated new DownloadStation Downloader for URI {uri} to filePath {filePath} and downloadPath {downloadPath} and GID {gid}");

        _gid = gid;
        _filePath = filePath;
        _uri = uri;
        _remotePath = remotePath;
        _synologyClient = synologyClient;
        _delayProvider = delayProvider ?? new DelayProvider();
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
        await synologyClient.LoginAsync(Settings.Get.DownloadClient.DownloadStationUsername, Settings.Get.DownloadClient.DownloadStationPassword);

        String? remotePath = null;
        String? rootPath;

        if (!String.IsNullOrWhiteSpace(Settings.Get.DownloadClient.DownloadStationDownloadPath))
        {
            rootPath = Settings.Get.DownloadClient.DownloadStationDownloadPath;
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

    public async Task Cancel()
    {
        if (_gid != null)
        {
            _logger.Debug($"Remove download {_uri} {_gid} from Synology DownloadStation");

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
                throw new($"The download link {_uri} has already been added to DownloadStation");
            }
        }

        var retryCount = 0;

        while (retryCount < 5)
        {
            _gid = await GetGidFromUri();

            if (_gid != null)
            {
                _logger.Debug($"Download with ID {_gid} found in DownloadStation");

                return _gid;
            }

            try
            {
                var createResult = await _synologyClient
                                         .DownloadStationApi()
                                         .TaskEndpoint()
                                         .CreateAsync(new(_uri, path[1..]));

                _gid = createResult.TaskId?.FirstOrDefault();
                _logger.Debug($"Added download to DownloadStation, received ID {_gid}");

                _gid ??= await GetGidFromUri();

                if (_gid != null)
                {
                    _logger.Debug($"Download with ID {_gid} found in DownloadStation");

                    return _gid;
                }

                retryCount++;
                _logger.Error($"Task not found in DownloadStation after creat Sucess. Retrying {retryCount}/{RetryCount}");
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

    private async Task<String?> GetGidFromUri()
    {
        var tasks = await _synologyClient.DownloadStationApi().TaskEndpoint().ListAsync();

        return tasks.Task?.FirstOrDefault(t => t.Additional?.Detail?.Uri == _uri)?.Id;
    }

    public async Task Pause()
    {
        _logger.Debug($"Pausing download {_uri} {_gid}");

        if (_gid != null)
        {
            await _synologyClient.DownloadStationApi().TaskEndpoint().PauseAsync(_gid);
        }
    }

    public async Task Resume()
    {
        _logger.Debug($"Resuming download {_uri} {_gid}");

        if (_gid != null)
        {
            await _synologyClient.DownloadStationApi().TaskEndpoint().ResumeAsync(_gid);
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

        if (task.Status == DownloadStationTaskStatus.Finished)
        {
            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = null
                                     });

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
        catch
        {
            return null;
        }
    }
}

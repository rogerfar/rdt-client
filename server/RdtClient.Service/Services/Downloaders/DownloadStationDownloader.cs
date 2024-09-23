
using Serilog;
using Synology.Api.Client;
using Synology.Api.Client.Apis.DownloadStation.Task.Models;

namespace RdtClient.Service.Services.Downloaders;

public class DownloadStationDownloader : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private const Int32 RetryCount = 5;

    private readonly SynologyClient _synologyClient;

    private readonly ILogger _logger;
    private readonly String _uri;
    private readonly String _filePath;
    private readonly String? _remotePath;

    private String? _gid;
    private DownloadStationDownloader(String? gid, String uri, String filePath, String downloadPath, String? category)
    {
        _logger = Log.ForContext<DownloadStationDownloader>();
        _logger.Debug($"Instantiated new DownloadStation Downloader for URI {uri} to filePath {filePath} and downloadPath {downloadPath} and GID {gid}");

        _gid = gid;
        _uri = uri;
        _filePath = filePath;

        _remotePath = !String.IsNullOrWhiteSpace(Settings.Get.DownloadClient.DownloadStationDownloadPath)
            ? String.IsNullOrWhiteSpace(category)
                ? Path.Combine(ToCurrentPath(Settings.Get.DownloadClient.DownloadStationDownloadPath))
                : Path.Combine(ToCurrentPath(Settings.Get.DownloadClient.DownloadStationDownloadPath), category)
            : null;

        _synologyClient = new SynologyClient(Settings.Get.DownloadClient.DownloadStationUrl);
    }

    public static async Task<DownloadStationDownloader> Init(String? gid, String uri, String filePath, String downloadPath, String? category)
    {
        var result = new DownloadStationDownloader(gid, uri, filePath, downloadPath, category);
        if (Settings.Get.DownloadClient.DownloadStationUsername != null && Settings.Get.DownloadClient.DownloadStationPassword != null)
            await result._synologyClient.LoginAsync(Settings.Get.DownloadClient.DownloadStationUsername, Settings.Get.DownloadClient.DownloadStationPassword);

        return result;
    }

    public async Task Cancel()
    {
        if (_gid != null)
        {
            _logger.Debug($"Remove download {_uri} {_gid} from Synology DownloadStation");

            await _synologyClient.DownloadStationApi().TaskEndpoint().DeleteAsync(new DownloadStationTaskDeleteRequest
            {
                Ids = new List<string> { _gid },
                ForceComplete = false
            });
        }
    }

    public async Task<string> Download()
    {
        var path = _remotePath != null ? ToUnixPath(_remotePath) : null;
        _logger.Debug($"Starting download of {_uri}, writing to path: {path}");

        if (_gid != null)
        {
            if (GetTask() != null)
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

            var createResult = await _synologyClient
                .DownloadStationApi()
                .TaskEndpoint()
                .CreateAsync(new DownloadStationTaskCreateRequest(_uri, path));

            _logger.Debug($"Added download to DownloadStation, received ID {_gid}");

            if (createResult.Success)
            {
                _gid = await GetGidFromUri();
                if (_gid != null)
                {
                    _logger.Debug($"Download with ID {_gid} found in DownloadStation");
                    return _gid;
                }
                else
                {
                    retryCount++;
                    _logger.Debug($"Task not found in DownloadStation after creat Sucess. Retrying {retryCount}/{RetryCount}");
                    await Task.Delay(retryCount * 1000);
                }
            }
            else
            {
                retryCount++;
                _logger.Debug($"Error starting download: {createResult.Error.Code}. Retrying {retryCount}/{RetryCount}");
                await Task.Delay(retryCount * 1000);
            }
        }

        throw new Exception($"Unable to download file");
    }

    private async Task<String?> GetGidFromUri()
    {
        var tasks = await _synologyClient.DownloadStationApi().TaskEndpoint().ListAsync();
        return tasks.Tasks.FirstOrDefault(t => t.Additional?.Detail?.Uri == _uri)?.Id;
    }

    public async Task Pause()
    {
        _logger.Debug($"Pausing download {_uri} {_gid}");
        if (_gid != null)
            await _synologyClient.DownloadStationApi().TaskEndpoint().PauseAsync(_gid);
    }

    public async Task Resume()
    {
        _logger.Debug($"Resuming download {_uri} {_gid}");
        if (_gid != null)
            await _synologyClient.DownloadStationApi().TaskEndpoint().ResumeAsync(_gid);
    }

    public async Task Update()
    {
        if (_gid == null)
            return;

        var task = await GetTask();

        if (task == null)
        {
            DownloadComplete?.Invoke(this, new() { Error = "Task not found" });
            return;
        }

        if (task.Status == "finished")
        {
            DownloadComplete?.Invoke(this, new() { Error = null });
            return;
        }

        DownloadProgress?.Invoke(this, new()
        {
            BytesDone = task.Additional.Transfer.SizeDownloaded,
            BytesTotal = task.Size,
            Speed = task.Additional.Transfer.SpeedDownload
        });
    }

    private static string ToUnixPath(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string ToCurrentPath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar);
    }

    private async Task<DownloadStationTask?> GetTask()
    {
        try
        {
            return await _synologyClient.DownloadStationApi().TaskEndpoint().GetInfoAsync(_gid);
        }
        catch
        {
            return null;
        }
    }
}

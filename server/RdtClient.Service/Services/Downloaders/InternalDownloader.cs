using DownloaderNET;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class InternalDownloader : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private readonly Downloader _downloadService;
    private readonly DownloaderNET.Settings _downloadConfiguration;

    private readonly String _filePath;
    private readonly String _uri;

    private readonly ILogger _logger;

    private readonly CancellationTokenSource _cancellationToken = new();

    private Boolean _finished;

    public InternalDownloader(String uri, String filePath)
    {
        _logger = Log.ForContext<InternalDownloader>();

        _uri = uri;
        _filePath = filePath;
        
        _downloadConfiguration = new DownloaderNET.Settings();

        SetSettings();

        _downloadService = new Downloader(_uri, _filePath, _downloadConfiguration);

        //_downloadService.OnLog += message => Debug.WriteLine(message.Message);

        _downloadService.OnProgress += (chunks, _) =>
        {
            if (DownloadProgress == null)
            {
                return;
            }

            DownloadProgress.Invoke(this,
                                     new DownloadProgressEventArgs
                                     {
                                         Speed = (Int64)chunks.Where(m => m.IsActive).Sum(m => m.Speed),
                                         BytesDone = chunks.Sum(m => m.DownloadBytes),
                                         BytesTotal = chunks.Sum(m => m.LengthBytes)
                                     });
        };

        _downloadService.OnComplete += (_, error) =>
        {
            DownloadComplete?.Invoke(this,
                                     new DownloadCompleteEventArgs
                                     {
                                         Error = error?.Message
                                     });

            _finished = true;

            return Task.CompletedTask;
        };
    }

    public Task<String?> Download()
    {
        _logger.Debug($"Starting download of {_uri}, writing to path: {_filePath}");

        Task.Run(async () => await _downloadService.Download(_cancellationToken.Token));
        Task.Run(StartTimer);

        return Task.FromResult<String?>(null);
    }

    public Task Cancel()
    {
        _logger.Debug($"Cancelling download {_uri}");

        _cancellationToken.Cancel(false);

        return Task.CompletedTask;
    }

    public Task Pause()
    {
        return Task.CompletedTask;
    }

    public Task Resume()
    {
        return Task.CompletedTask;
    }

    private void SetSettings()
    {
        var settingDownloadParallelCount = Settings.Get.DownloadClient.ParallelCount;

        if (settingDownloadParallelCount <= 0)
        {
            settingDownloadParallelCount = 1;
        }
        
        var settingDownloadMaxSpeed = Settings.Get.DownloadClient.MaxSpeed;

        if (settingDownloadMaxSpeed <= 0)
        {
            settingDownloadMaxSpeed = 0;
        }

        settingDownloadMaxSpeed = settingDownloadMaxSpeed * 1024 * 1024;

        var settingDownloadTimeout = Settings.Get.DownloadClient.Timeout;

        if (settingDownloadTimeout <= 0)
        {
            settingDownloadTimeout = 1000;
        }
        
        _downloadConfiguration.Parallel = settingDownloadParallelCount;
        _downloadConfiguration.MaximumBytesPerSecond = settingDownloadMaxSpeed;
        _downloadConfiguration.Timeout = settingDownloadTimeout;
        _downloadConfiguration.RetryCount = 5;
    }

    private async Task StartTimer()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync())
        {
            if (_finished)
            {
                return;
            }

            SetSettings();
        }
    }
}
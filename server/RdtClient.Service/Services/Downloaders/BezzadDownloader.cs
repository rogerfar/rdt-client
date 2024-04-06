using System.Net;
using Downloader;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class BezzadDownloader : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private readonly DownloadService _downloadService;
    private readonly DownloadConfiguration _downloadConfiguration;

    private readonly String _filePath;
    private readonly String _uri;

    private readonly ILogger _logger;

    private Boolean _finished;

    public BezzadDownloader(String uri, String filePath)
    {
        _logger = Log.ForContext<BezzadDownloader>();
        _logger.Debug($"Instantiated new Bezzad Downloader for URI {uri} to filePath {filePath}");

        _uri = uri;
        _filePath = filePath;

        var settingProxyServer = Settings.Get.DownloadClient.ProxyServer;

        // For all options, see https://github.com/bezzad/Downloader
        _downloadConfiguration = new()
        {
            MaxTryAgainOnFailover = 5,
            RangeDownload = false,
            ClearPackageOnCompletionWithFailure = true,
            ReserveStorageSpaceBeforeStartingDownload = false,
            CheckDiskSizeBeforeDownload = false,
            MaximumMemoryBufferBytes = 1024 * 1024 * 10,
            RequestConfiguration =
            {
                Accept = "*/*",
                UserAgent = $"rdt-client",
                ProtocolVersion = HttpVersion.Version11,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                KeepAlive = true,
                UseDefaultCredentials = false
            }
        };

        SetSettings();

        if (!String.IsNullOrWhiteSpace(settingProxyServer))
        {
            _downloadConfiguration.RequestConfiguration.Proxy = new WebProxy(new Uri(settingProxyServer), false);
        }

        _downloadService = new(_downloadConfiguration);

        _downloadService.DownloadProgressChanged += (_, args) =>
        {
            if (DownloadProgress == null)
            {
                return;
            }

            DownloadProgress.Invoke(this,
                                     new()
                                     {
                                         Speed = (Int64)args.BytesPerSecondSpeed,
                                         BytesDone = args.ReceivedBytesSize,
                                         BytesTotal = args.TotalBytesToReceive
                                     });
        };

        _downloadService.DownloadFileCompleted += (_, args) =>
        {
            String? error = null;

            if (args.Cancelled)
            {
                error = $"The download was cancelled";
            }
            else if (args.Error != null)
            {
                error = args.Error.Message;
            }

            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = error
                                     });

            _finished = true;
        };
    }

    public Task<String> Download()
    {
        _logger.Debug($"Starting download of {_uri}, writing to path: {_filePath}");

        _ = Task.Run(async () =>
        {
            await _downloadService.DownloadFileTaskAsync(_uri, _filePath);
        });

        _ = Task.Run(StartTimer);

        return Task.FromResult(Guid.NewGuid().ToString());
    }

    public Task Cancel()
    {
        _logger.Debug($"Cancelling download {_uri}");

        _downloadService.CancelAsync();

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

        var settingParallelCount = Settings.Get.DownloadClient.ParallelCount;

        if (settingParallelCount <= 0)
        {
            settingParallelCount = 4;
        }

        if (Settings.Get.DownloadClient.ChunkCount <= 0)
        {
            _downloadConfiguration.ChunkCount = 8;
        }
        else
        {
            _downloadConfiguration.ChunkCount = Settings.Get.DownloadClient.ChunkCount;
        }
        
        _downloadConfiguration.MaximumBytesPerSecond = settingDownloadMaxSpeed;
        _downloadConfiguration.ParallelDownload = settingParallelCount > 1;
        _downloadConfiguration.ParallelCount = settingParallelCount;
        _downloadConfiguration.Timeout = settingDownloadTimeout;
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
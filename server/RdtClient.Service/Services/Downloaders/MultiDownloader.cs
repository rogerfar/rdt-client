using System.Net;
using Downloader;
using RdtClient.Data.Models.Internal;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class MultiDownloader : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private readonly DownloadService _downloadService;
    private readonly String _filePath;
    private readonly String _uri;

    private readonly ILogger _logger;

    public MultiDownloader(String uri, String filePath, DbSettings settings)
    {
        _logger = Log.ForContext<MultiDownloader>();

        _uri = uri;
        _filePath = filePath;

        var settingTempPath = settings.DownloadClient.TempPath;

        if (String.IsNullOrWhiteSpace(settingTempPath))
        {
            settingTempPath = Path.GetTempPath();
        }

        var settingDownloadChunkCount = settings.DownloadClient.ChunkCount;

        if (settingDownloadChunkCount <= 0)
        {
            settingDownloadChunkCount = 1;
        }

        var settingDownloadMaxSpeed = settings.DownloadClient.MaxSpeed;

        if (settingDownloadMaxSpeed <= 0)
        {
            settingDownloadMaxSpeed = 0;
        }

        settingDownloadMaxSpeed = settingDownloadMaxSpeed * 1024 * 1024;

        var settingProxyServer = settings.DownloadClient.ProxyServer;

        var downloadOpt = new DownloadConfiguration
        {
            MaxTryAgainOnFailover = Int32.MaxValue,
            ParallelDownload = settingDownloadChunkCount > 1,
            ChunkCount = settingDownloadChunkCount,
            Timeout = 1000,
            OnTheFlyDownload = false,
            BufferBlockSize = 1024 * 8,
            MaximumBytesPerSecond = settingDownloadMaxSpeed,
            TempDirectory = settingTempPath,
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

        if (!String.IsNullOrWhiteSpace(settingProxyServer))
        {
            downloadOpt.RequestConfiguration.Proxy = new WebProxy(new Uri(settingProxyServer), false);
        }

        _downloadService = new DownloadService(downloadOpt);

        _downloadService.DownloadProgressChanged += (_, args) =>
        {
            if (DownloadProgress == null)
            {
                return;
            }

            DownloadProgress.Invoke(this,
                                     new DownloadProgressEventArgs
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
                                     new DownloadCompleteEventArgs
                                     {
                                         Error = error
                                     });
        };
    }

    public async Task<String?> Download()
    {
        _logger.Debug($"Starting download of {_uri}, writing to path: {_filePath}");

        await _downloadService.DownloadFileTaskAsync(_uri, _filePath);

        return null;
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
}
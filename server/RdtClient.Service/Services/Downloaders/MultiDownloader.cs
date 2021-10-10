using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Downloader;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Services.Downloaders
{
    public class MultiDownloader : IDownloader
    {
        public event EventHandler<DownloadCompleteEventArgs> DownloadComplete;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        private readonly DownloadService _downloadService;
        private readonly String _filePath;
        private readonly String _uri;

        public MultiDownloader(String uri, String filePath, DbSettings settings)
        {
            _uri = uri;
            _filePath = filePath;

            var settingTempPath = settings.TempPath;

            if (String.IsNullOrWhiteSpace(settingTempPath))
            {
                settingTempPath = Path.GetTempPath();
            }

            var settingDownloadChunkCount = settings.DownloadChunkCount;

            if (settingDownloadChunkCount <= 0)
            {
                settingDownloadChunkCount = 1;
            }

            var settingDownloadMaxSpeed = settings.DownloadMaxSpeed;

            if (settingDownloadMaxSpeed <= 0)
            {
                settingDownloadMaxSpeed = 0;
            }

            settingDownloadMaxSpeed = settingDownloadMaxSpeed * 1024 * 1024;

            var settingProxyServer = settings.ProxyServer;

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

                DownloadProgress?.Invoke(this,
                                         new DownloadProgressEventArgs
                                         {
                                             Speed = (Int64)args.BytesPerSecondSpeed,
                                             BytesDone = args.ReceivedBytesSize,
                                             BytesTotal = args.TotalBytesToReceive
                                         });
            };

            _downloadService.DownloadFileCompleted += (_, args) =>
            {
                String error = null;

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

        public async Task<String> Download()
        {
            await _downloadService.DownloadFileTaskAsync(_uri, _filePath);

            return null;
        }

        public Task Cancel()
        {
            _downloadService.CancelAsync();

            return Task.CompletedTask;
        }
    }
}

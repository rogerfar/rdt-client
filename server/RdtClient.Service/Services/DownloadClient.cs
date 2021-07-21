using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Downloader;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services
{
    public class DownloadClient
    {
        private readonly String _destinationPath;

        private readonly Download _download;
        private readonly Torrent _torrent;

        private DownloadService _downloader;
        private SimpleDownloader _simpleDownloader;

        public DownloadClient(Download download, Torrent torrent, String destinationPath)
        {
            _download = download;
            _torrent = torrent;
            _destinationPath = destinationPath;
        }

        public Boolean Finished { get; private set; }

        public String Error { get; private set; }

        public Int64 Speed { get; private set; }
        public Int64 BytesTotal { get; private set; }
        public Int64 BytesDone { get; private set; }

        public async Task Start(DbSettings settings)
        {
            BytesDone = 0;
            BytesTotal = 0;
            Speed = 0;

            try
            {
                var filePath = DownloadHelper.GetDownloadPath(_destinationPath, _torrent, _download);

                if (filePath == null)
                {
                    throw new Exception("Invalid download path");
                }
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var uri = new Uri(_download.Link);

                await Task.Run(async delegate
                {
                    switch (settings.DownloadClient)
                    {
                        case "Simple":
                            await DownloadSimple(uri, filePath);
                            break;
                        case "MultiPart":
                            await DownloadMultiPart(filePath, settings);
                            break;
                        default:
                            throw new Exception($"Unknown download client {settings.DownloadClient}");
                    }
                });
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred preparing download {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
                Finished = true;
            }
        }

        public void Cancel()
        {
            _downloader?.CancelAsync();
            _simpleDownloader?.Cancel();
        }

        private async Task DownloadSimple(Uri uri, String filePath)
        {
            try
            {
                _simpleDownloader = new SimpleDownloader();

                _simpleDownloader.DownloadProgressChanged += (_, args) =>
                {
                    Speed = (Int64) args.BytesPerSecondSpeed;
                    BytesDone = args.ReceivedBytesSize;
                    BytesTotal = args.TotalBytesToReceive;
                };
                
                _simpleDownloader.DownloadFileCompleted += (_, args) =>
                {
                    if (args.Cancelled)
                    {
                        Error = $"The download was cancelled";
                    }
                    else if (args.Error != null)
                    {
                        Error = args.Error.Message;
                    }

                    Finished = true;
                };
                
                Speed = 0;
                BytesDone = 0;
                BytesTotal = 0;

                await _simpleDownloader.Download(uri, filePath);
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred downloading {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
                Finished = true;
            }
        }

        private async Task DownloadMultiPart(String filePath, DbSettings settings)
        {
            try
            {
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

                _downloader = new DownloadService(downloadOpt);

                _downloader.DownloadProgressChanged += (_, args) =>
                {
                    Speed = (Int64) args.BytesPerSecondSpeed;
                    BytesDone = args.ReceivedBytesSize;
                    BytesTotal = args.TotalBytesToReceive;
                };
                
                _downloader.DownloadFileCompleted += (_, args) =>
                {
                    if (args.Cancelled)
                    {
                        Error = $"The download was cancelled";
                    }
                    else if (args.Error != null)
                    {
                        Error = args.Error.Message;
                    }

                    Finished = true;
                };

                await _downloader.DownloadFileTaskAsync(_download.Link, filePath);
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred downloading {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
                Finished = true;
            }
        }
    }
}

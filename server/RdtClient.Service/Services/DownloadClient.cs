using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Downloader;
using RdtClient.Data.Models.Data;
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

        public DownloadClient(Download download, String destinationPath)
        {
            _download = download;
            _destinationPath = destinationPath;
            _torrent = download.Torrent;
        }

        public Boolean Finished { get; private set; }

        public String Error { get; private set; }

        public Int64 Speed { get; private set; }
        public Int64 BytesTotal { get; private set; }
        public Int64 BytesDone { get; private set; }

        public async Task Start(IList<Setting> settings)
        {
            BytesDone = 0;
            BytesTotal = 0;
            Speed = 0;

            try
            {
                var downloadClientSetting = settings.GetString("DownloadClient");
                
                var fileUrl = _download.Link;

                if (String.IsNullOrWhiteSpace(fileUrl))
                {
                    throw new Exception("File URL is empty");
                }

                var uri = new Uri(fileUrl);
                var torrentPath = Path.Combine(_destinationPath, _torrent.RdName);

                if (!Directory.Exists(torrentPath))
                {
                    Directory.CreateDirectory(torrentPath);
                }

                var fileName = uri.Segments.Last();

                fileName = HttpUtility.UrlDecode(fileName);

                var filePath = Path.Combine(torrentPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await Task.Factory.StartNew(async delegate
                {
                    switch (downloadClientSetting)
                    {
                        case "Simple":
                            await DownloadSimple(uri, filePath);
                            break;
                        case "MultiPart":
                            await DownloadMultiPart(filePath, settings);
                            break;
                        default:
                            throw new Exception($"Unknown download client {downloadClientSetting}");
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

        private async Task DownloadMultiPart(String filePath, IList<Setting> settings)
        {
            try
            {
                var settingTempPath = settings.GetString("TempPath");
                if (String.IsNullOrWhiteSpace(settingTempPath))
                {
                    settingTempPath = Path.GetTempPath();
                }

                var settingDownloadChunkCount = settings.GetNumber("DownloadChunkCount");
                if (settingDownloadChunkCount <= 0)
                {
                    settingDownloadChunkCount = 1;
                }

                var settingDownloadMaxSpeed = settings.GetNumber("DownloadMaxSpeed");
                if (settingDownloadMaxSpeed <= 0)
                {
                    settingDownloadMaxSpeed = 0;
                }
                settingDownloadMaxSpeed = settingDownloadMaxSpeed * 1024 * 1024;
                
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

                await _downloader.DownloadFileAsync(_download.Link, filePath);
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred downloading {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
                Finished = true;
            }
        }
    }
}

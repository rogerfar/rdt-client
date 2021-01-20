using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Downloader;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Services
{
    public class DownloadClient
    {
        private readonly String _destinationPath;

        private readonly Download _download;
        private readonly Torrent _torrent;

        private DownloadService _downloader;

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

        private static Int64 LastTick { get; set; }
        private static ConcurrentBag<Int64> AverageSpeed { get; } = new ConcurrentBag<Int64>();

        public async Task Start(Boolean writeToMemory)
        {
            BytesDone = 0;
            BytesTotal = 0;
            Speed = 0;

            try
            {
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
                var filePath = Path.Combine(torrentPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await Task.Factory.StartNew(async delegate
                {
                    await Download(filePath, writeToMemory);
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
        }

        private async Task Download(String filePath, Boolean writeToMemory)
        {
            try
            {
                LastTick = 0;

                var downloadOpt = new DownloadConfiguration
                {
                    MaxTryAgainOnFailover = Int32.MaxValue,
                    ParallelDownload = true,
                    ChunkCount = 8,
                    Timeout = 100,
                    OnTheFlyDownload = writeToMemory,
                    BufferBlockSize = 1024 * 8,
                    MaximumBytesPerSecond = 100 * 1024 * 1024,
                    TempDirectory = @"C:\temp",
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
                    var isElapsedTimeMoreThanOneSecond = Environment.TickCount - LastTick >= 1000;

                    if (isElapsedTimeMoreThanOneSecond)
                    {
                        AverageSpeed.Add(args.BytesPerSecondSpeed);
                        LastTick = Environment.TickCount;
                    }

                    Speed = (Int64) AverageSpeed.Average();
                    BytesDone = args.BytesReceived;
                    BytesTotal = args.TotalBytesToReceive;
                };

                _downloader.DownloadStarted += (_, args) =>
                {
                    AverageSpeed?.Clear();
                    Speed = 0;
                    BytesDone = 0;
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

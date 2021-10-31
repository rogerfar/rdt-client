using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RdtClient.Service.Services.Downloaders
{
    public class SimpleDownloader : IDownloader
    {
        private const Int32 BufferSize = 8 * 1024;

        public event EventHandler<DownloadCompleteEventArgs> DownloadComplete;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        private readonly String _uri;
        private readonly String _filePath;

        private Int64 Speed { get; set; }
        private Int64 BytesTotal { get; set; }
        private Int64 BytesDone { get; set; }

        private Boolean _cancelled;

        private Int64 _bytesLastUpdate;
        private DateTime _nextUpdate;

        private readonly ILogger _logger;

        public SimpleDownloader(String uri, String filePath)
        {
            _logger = Log.ForContext<Aria2cDownloader>();

            _uri = uri;
            _filePath = filePath;
        }

        public Task<String> Download()
        {
            _logger.Debug($"Starting download of {_uri}, writing to path: {_filePath}");

            _ = Task.Run(async () =>
            {
                await StartDownloadTask();
            });
            
            return Task.FromResult<String>(null);
        }

        public Task Cancel()
        {
            _logger.Debug($"Cancelling download {_uri}");

            _cancelled = true;

            return Task.CompletedTask;
        }

        private async Task StartDownloadTask()
        {
            try
            {
                _bytesLastUpdate = 0;
                _nextUpdate = DateTime.UtcNow.AddSeconds(1);

                // Determine the file size
                var webRequest = WebRequest.Create(_uri);
                webRequest.Method = "HEAD";
                webRequest.Timeout = 5000;
                Int64 responseLength;

                using (var webResponse = await webRequest.GetResponseAsync())
                {
                    responseLength = Int64.Parse(webResponse.Headers.Get("Content-Length"));
                }

                var timeout = DateTimeOffset.UtcNow.AddHours(1);

                while (timeout > DateTimeOffset.UtcNow && !_cancelled)
                {
                    try
                    {
                        var request = WebRequest.Create(_uri);
                        using var response = await request.GetResponseAsync();

                        await using var responseStream = response.GetResponseStream();

                        if (responseStream == null)
                        {
                            throw new IOException("No stream");
                        }

                        var speedLimit = Settings.Get.DownloadMaxSpeed * BufferSize * 1024L;
                        
                        if (speedLimit == 0)
                        {
                            speedLimit = ThrottledStream.Infinite;
                        }

                        await using var destinationStream = new ThrottledStream(responseStream, speedLimit);

                        await using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Write);
                        
                        var readSize = 1;
                        while (readSize > 0 && !_cancelled)
                        {
                            using var innerCts = new CancellationTokenSource(1000);
                            var buffer = new Byte[BufferSize * 8];
                            readSize = await destinationStream.ReadAsync(buffer, 0, buffer.Length, innerCts.Token).ConfigureAwait(false);

                            await fileStream.WriteAsync(buffer.AsMemory(0, readSize), innerCts.Token);

                            BytesDone = fileStream.Length;
                            BytesTotal = responseLength;

                            if (DateTime.UtcNow > _nextUpdate)
                            {
                                Speed = fileStream.Length - _bytesLastUpdate;

                                _nextUpdate = DateTime.UtcNow.AddSeconds(1);
                                _bytesLastUpdate = fileStream.Length;

                                timeout = DateTimeOffset.UtcNow.AddHours(1);

                                DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
                                {
                                    Speed = Speed,
                                    BytesDone = BytesDone,
                                    BytesTotal = BytesTotal
                                });
                            }
                        }

                        break;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(1000);
                    }
                    catch (WebException)
                    {
                        await Task.Delay(1000);
                    }
                }

                if (_cancelled)
                {
                    throw new Exception("Download cancelled");
                }

                if (timeout <= DateTimeOffset.UtcNow)
                {
                    throw new Exception($"Download timed out");
                }

                DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs());
            }
            catch (Exception ex)
            {
                DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs
                {
                    Error = ex.Message
                });
            }
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
}

using System.Net;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class SimpleDownloader : IDownloader
{
    private const Int32 BufferSize = 8 * 1024;

    public event EventHandler<DownloadCompleteEventArgs> DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

    private readonly String _uri;
    private readonly String _filePath;

    private Int64 _bytesTotal;
    private Int64 _bytesDone;

    private Boolean _cancelled;

    private DateTime _nextUpdate;

    private readonly ILogger _logger;

    public SimpleDownloader(String uri, String filePath)
    {
        _logger = Log.ForContext<SimpleDownloader>();

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
            _nextUpdate = DateTime.UtcNow.AddSeconds(1);

            _bytesTotal = await GetContentSize();
                
            var timeout = DateTimeOffset.UtcNow.AddHours(1);

            var httpClient = new HttpClient();

            while (timeout > DateTimeOffset.UtcNow && !_cancelled)
            {
                try
                {
                    var responseStream = await httpClient.GetStreamAsync(_uri);

                    if (responseStream == null)
                    {
                        throw new IOException("No stream");
                    }

                    var speedLimit = Settings.Get.DownloadMaxSpeed;
                        
                    await using var destinationStream = new ThrottledStream(responseStream, speedLimit * 1000L * 1000L);

                    await using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Write);
                        
                    var readSize = 1;
                    var buffer = new Byte[BufferSize * 8];

                    while (readSize > 0 && !_cancelled)
                    {
                        // ReSharper disable once ConvertToUsingDeclaration
                        using (var innerCts = new CancellationTokenSource(1000))
                        {
                            readSize = await destinationStream.ReadAsync(buffer.AsMemory(0, buffer.Length), innerCts.Token).ConfigureAwait(false);

                            await fileStream.WriteAsync(buffer.AsMemory(0, readSize), innerCts.Token);

                            _bytesDone = fileStream.Length;

                            if (DateTime.UtcNow > _nextUpdate)
                            {
                                _nextUpdate = DateTime.UtcNow.AddSeconds(1);

                                timeout = DateTimeOffset.UtcNow.AddHours(1);

                                DownloadProgress?.Invoke(this,
                                                         new DownloadProgressEventArgs
                                                         {
                                                             Speed = destinationStream.Speed,
                                                             BytesDone = _bytesDone,
                                                             BytesTotal = _bytesTotal
                                                         });

                                if (Settings.Get.DownloadMaxSpeed != speedLimit)
                                {
                                    speedLimit = Settings.Get.DownloadMaxSpeed;
                                    destinationStream.BandwidthLimit = speedLimit * 1000L * 1000L;
                                }
                            }
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

            DownloadComplete.Invoke(this, new DownloadCompleteEventArgs());
        }
        catch (Exception ex)
        {
            DownloadComplete.Invoke(this, new DownloadCompleteEventArgs
            {
                Error = ex.Message
            });
        }
    }

    private async Task<Int64> GetContentSize()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var responseHeaders = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, _uri));

        if (!responseHeaders.IsSuccessStatusCode)
        {
            return -1;
        }

        return responseHeaders.Content.Headers.ContentLength ?? -1;
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
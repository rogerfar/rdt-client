using Aria2NET;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class Aria2cDownloader : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private const Int32 RetryCount = 5;

    private readonly Aria2NetClient _aria2NetClient;

    private readonly ILogger _logger;
    private readonly String _uri;
    private readonly String _filePath;

    private String? _gid;

    public Aria2cDownloader(String? gid, String uri, String filePath)
    {
        _logger = Log.ForContext<Aria2cDownloader>();
        _gid = gid;
        _uri = uri;
        _filePath = filePath;

        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        _aria2NetClient = new Aria2NetClient(Settings.Get.DownloadClient.Aria2cUrl, Settings.Get.DownloadClient.Aria2cSecret, httpClient, 10);
    }
        
    public async Task<String?> Download()
    {
        var path = Path.GetDirectoryName(_filePath);

        if (path == null)
        {
            throw new Exception($"Invalid file path {_filePath}");
        }

        var fileName = Path.GetFileName(_filePath);

        _logger.Debug($"Starting download of {_uri}, writing to path: {path}, fileName: {fileName}");

        if (String.IsNullOrWhiteSpace(_gid))
        {
            var isAlreadyAdded = await CheckIfAdded();

            if (isAlreadyAdded)
            {
                throw new Exception($"The download link {_uri} has already been added to Aria2");
            }
        }

        var retryCount = 0;
        while(true)
        {
            try
            {
                if (_gid != null)
                {
                    try
                    {
                        await _aria2NetClient.TellStatusAsync(_gid);

                        return _gid;
                    }
                    catch
                    {
                        _gid = null;
                    }
                }
                    
                _gid ??= await _aria2NetClient.AddUriAsync(new List<String>
                                                           {
                                                               _uri
                                                           },
                                                           new Dictionary<String, Object>
                                                           {
                                                               {
                                                                   "dir", path
                                                               },
                                                               {
                                                                   "out", fileName
                                                               }
                                                           });

                _logger.Debug($"Added download to Aria2, received ID {_gid}");

                await _aria2NetClient.TellStatusAsync(_gid);

                _logger.Debug($"Download with ID {_gid} found in Aria2");

                return _gid;
            }
            catch (Exception ex)
            {
                if (retryCount >= RetryCount)
                {
                    throw;
                }

                _logger.Debug($"Error starting download: {ex.Message}. Retrying {retryCount}/{RetryCount}");

                await Task.Delay(retryCount * 1000);

                retryCount++;
            }
        }
    }

    public async Task Cancel()
    {
        await Remove();
    }

    public async Task Pause()
    {
        _logger.Debug($"Pausing download {_uri} {_gid}");

        if (String.IsNullOrWhiteSpace(_gid))
        {
            return;
        }

        try
        {
            await _aria2NetClient.PauseAsync(_gid);
        }
        catch
        {
            // ignored
        }
    }

    public async Task Resume()
    {
        _logger.Debug($"Resuming download {_uri} {_gid}");

        if (String.IsNullOrWhiteSpace(_gid))
        {
            return;
        }

        try
        {
            await _aria2NetClient.UnpauseAsync(_gid);
        }
        catch
        {
            // ignored
        }
    }

    public async Task Update(IEnumerable<DownloadStatusResult> allDownloads)
    {
        if (_gid == null)
        {
            return;
        }

        var download = allDownloads.FirstOrDefault(m => m.Gid == _gid);

        if (download == null)
        {
            DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs
            {
                Error = $"Download was not found in Aria2"
            });
            return;
        }

        if (!String.IsNullOrWhiteSpace(download.ErrorMessage) || download.Status == "error")
        {
            await Remove();
            DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs
            {
                Error = $"{download.ErrorCode}: {download.ErrorMessage}"
            });
            return;
        }

        if (download.Status == "complete" || download.Status == "removed")
        {
            _logger.Debug($"Aria2 download found as complete {_gid}");

            await Remove();

            var retryCount = 0;
            while (true)
            {
                if (retryCount >= 10)
                {
                    DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs
                    {
                        Error = $"File not found at {_filePath}"
                    });
                    break;
                }

                if (File.Exists(_filePath))
                {
                    DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs());
                    break;
                }

                await Task.Delay(1000 * retryCount);
                retryCount++;
            }
            return;
        }

        DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
        {
            BytesDone = download.CompletedLength,
            BytesTotal = download.TotalLength,
            Speed = download.DownloadSpeed
        });
    }

    private async Task Remove()
    {
        if (String.IsNullOrWhiteSpace(_gid))
        {
            return;
        }

        _logger.Debug($"Remove download {_uri} {_gid} from Aria2");

        try
        {
            await _aria2NetClient.ForceRemoveAsync(_gid);
        }
        catch
        {
            // ignored
        }

        try
        {
            await _aria2NetClient.RemoveDownloadResultAsync(_gid);
        }
        catch
        {
            // ignored
        }
    }

    private async Task<Boolean> CheckIfAdded()
    {
        var allDownloads = await _aria2NetClient.TellAllAsync();

        foreach (var download in allDownloads)
        {
            foreach (var file in download.Files)
            {
                if (file.Uris.Any(uri => uri.Uri == _uri))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
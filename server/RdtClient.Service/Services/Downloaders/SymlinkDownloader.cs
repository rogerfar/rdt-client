using System.Diagnostics;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class SymlinkDownloader : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private const Int32 RetryCount = 5;

    private readonly String _filePath;
    private readonly String _uri;
    
    private readonly CancellationTokenSource _cancellationToken = new();
    
    private readonly ILogger _logger;
    
    public SymlinkDownloader(String uri, String filePath)
    {
        _logger = Log.ForContext<SymlinkDownloader>();

        _uri = uri;
        _filePath = filePath;
    }

    public async Task<String?> Download()
    {
        _logger.Debug($"Starting download of {_uri}, writing to path: {_filePath}");

        var fileName = Path.GetFileName(_filePath);

        DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
        {
            BytesDone = 0,
            BytesTotal = 0,
            Speed = 0
        });

        var retryCount = 1;

        while (retryCount < RetryCount)
        {
            _logger.Debug($"Searching {Settings.Get.DownloadClient.RcloneMountPath} for {fileName} ({retryCount}/{RetryCount}) ");

            // Recursively search for the fileName in the rclone mount location.
            var foundFiles = Directory.GetFiles(Settings.Get.DownloadClient.RcloneMountPath, fileName, SearchOption.AllDirectories).ToList();

            if (foundFiles.Any())
            {
                if (foundFiles.Count > 1)
                {
                    _logger.Warning($"Found {foundFiles.Count} files named {fileName}");
                }

                // Assume first matching filename is the one we want.
                var actualFilePath = foundFiles.First();

                var result = TryCreateSymbolicLink(actualFilePath, _filePath);

                if (result)
                {
                    DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs());

                    return actualFilePath;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), _cancellationToken.Token);

            retryCount++;
        }

        _logger.Error($"File '{fileName}' not found after {RetryCount} attempts.");

        DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs
        {
            Error = $"File '{fileName}' not found after {RetryCount} attempts."
        });
        
        return null;
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

    private Boolean TryCreateSymbolicLink(String sourcePath, String symlinkPath)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "ln";
            process.StartInfo.Arguments = @$"-s ""{sourcePath}"" ""{symlinkPath}""";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;

            process.Start();

            var errors = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                _logger.Information($"Created symbolic link from {sourcePath} to {symlinkPath}");

                return true;
            }
            
            _logger.Error($"Failed to create symbolic link: {process.ExitCode} - {errors}");

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating symbolic link from {sourcePath} to {symlinkPath}: {ex.Message}");

            return false;
        }
    }
}

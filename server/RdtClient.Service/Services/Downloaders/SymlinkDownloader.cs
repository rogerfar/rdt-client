using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class SymlinkDownloader : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

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

    public Task<String?> Download()
    {
        _logger.Debug($"Starting symlink resolving of {_uri}, writing to path: {_filePath}");

        var fileName = Path.GetFileName(_filePath);

        DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
        {
            BytesDone = 0,
            BytesTotal = 0,
            Speed = 0
        });

        _logger.Debug($"Searching {Settings.Get.DownloadClient.RcloneMountPath} for {fileName}");

        // Recursively search for the fileName in the rclone mount location.
        var foundFiles = Directory.GetFiles(Settings.Get.DownloadClient.RcloneMountPath, fileName, SearchOption.AllDirectories);

        if (foundFiles.Any())
        {
            if (foundFiles.Length > 1)
            {
                _logger.Warning($"Found {foundFiles.Length} files named {fileName}");
            }

            // Assume first matching filename is the one we want.
            var actualFilePath = foundFiles.First();

            var result = TryCreateSymbolicLink(actualFilePath, _filePath);

            if (result)
            {
                DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs());

                _logger.Information($"File {fileName} found on {Settings.Get.DownloadClient.RcloneMountPath} at {actualFilePath}");
                return Task.FromResult<String?>(actualFilePath);
            }
        }

        _logger.Information($"File {fileName} not found on {Settings.Get.DownloadClient.RcloneMountPath}!");

        // Return null and try again next cycle.
        return Task.FromResult<String?>(null);
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
            _logger.Information($"Creating symbolic link from {sourcePath} to {symlinkPath}");

            File.CreateSymbolicLink(symlinkPath, sourcePath);

            if (File.Exists(symlinkPath))
            {
                _logger.Information($"Created symbolic link from {sourcePath} to {symlinkPath}");
                return true;
            }

            _logger.Error($"Failed to create symbolic link from {sourcePath} to {symlinkPath}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating symbolic link from {sourcePath} to {symlinkPath}: {ex.Message}");
            return false;
        }
    }
}

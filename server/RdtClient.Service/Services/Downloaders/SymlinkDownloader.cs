using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class SymlinkDownloader(String uri, String path) : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private readonly CancellationTokenSource _cancellationToken = new();

    private readonly ILogger _logger = Log.ForContext<SymlinkDownloader>();

    public async Task<String> Download()
    {
        _logger.Debug($"Starting symlink resolving of {uri}, writing to path: {path}");

        var filePath = new DirectoryInfo(path);

        var fileName = filePath.Name;
        var fileExtension = filePath.Extension;
        var directoryName = Path.GetDirectoryName(filePath.FullName) ?? throw new($"Cannot get directory name for file {filePath.FullName}");
        var fileDirectory = Path.GetFileName(directoryName) ?? throw new($"Cannot get directory name for file {directoryName}");
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName) ?? throw new($"Cannot get directory name for file {fileName}");
        var fileDirectoryWithoutExtension = Path.GetFileNameWithoutExtension(fileDirectory) ?? throw new($"Cannot get directory name for file {fileDirectory}");

        String[] folders =
        [
            fileNameWithoutExtension, 
            fileDirectoryWithoutExtension, 
            fileName, 
            fileDirectory
        ];

        List<String> unWantedExtensions =
        [
            "zip",
            "rar",
            "tar"
        ];

        if (unWantedExtensions.Any(m => fileExtension == m))
        {
            throw new($"Cant handle compressed files with symlink downloader");
        }

        DownloadProgress?.Invoke(this, new()
                                 {
                                     BytesDone = 0,
                                     BytesTotal = 0,
                                     Speed = 0
                                 });

        FileInfo? file = null;

        var tries = 1;

        while (file == null && tries <= 10)
        {
            _logger.Debug($"Searching {Settings.Get.DownloadClient.RcloneMountPath} for {fileName} (attempt #{tries})...");

            var dirInfo = new DirectoryInfo(Settings.Get.DownloadClient.RcloneMountPath);
            file = dirInfo.EnumerateDirectories().FirstOrDefault(dir => folders.Contains(dir.Name))?.EnumerateFiles().FirstOrDefault(x => x.Name == fileName);

            if (file == null)
            {
                await Task.Delay(1000 * tries);

                tries++;
            }
        }

        if (file == null)
        {
            throw new("Could not find file from rclone mount!");
        }

        _logger.Debug($"Found {file.FullName} after #{tries} attempts");

        var result = TryCreateSymbolicLink(file.FullName, filePath.FullName);

        if (!result)
        {
            throw new("Could not find file from rclone mount!");
        }

        DownloadComplete?.Invoke(this, new());

        return file.FullName;

    }

    public Task Cancel()
    {
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
            File.CreateSymbolicLink(symlinkPath, sourcePath);

            if (File.Exists(symlinkPath)) // Double-check that the link was created
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

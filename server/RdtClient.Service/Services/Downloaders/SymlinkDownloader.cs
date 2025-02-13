using RdtClient.Data.Enums;
using RdtClient.Service.Helpers;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class SymlinkDownloader(String uri, String destinationPath, String path, Provider? clientKind) : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private readonly CancellationTokenSource _cancellationToken = new();

    private readonly ILogger _logger = Log.ForContext<SymlinkDownloader>();

    private const Int32 MaxRetries = 10;

    public async Task<String> Download()
    {
        _logger.Debug($"Starting symlink resolving of {path} (uri = {uri}), writing to path: {destinationPath}");

        try
        {
            var filePath = new FileInfo(path);

            var rcloneMountPath = Settings.Get.DownloadClient.RcloneMountPath.TrimEnd(['\\', '/']);
            var searchSubDirectories = rcloneMountPath.EndsWith('*');
            rcloneMountPath = rcloneMountPath.TrimEnd('*').TrimEnd(['\\', '/']);

            if (!Directory.Exists(rcloneMountPath))
            {
                throw new($"Mount path {rcloneMountPath} does not exist!");
            }

            var fileName = filePath.Name;
            var fileExtension = filePath.Extension;
            var fileNameWithoutExtension = fileName.Replace(fileExtension, "");
            var pathWithoutFileName = path.Replace(fileName, "").TrimEnd(['\\', '/']);
            var searchPath = Path.Combine(rcloneMountPath, pathWithoutFileName);

            List<String> unWantedExtensions =
            [
                ".zip",
                ".rar",
                ".tar"
            ];

            if (unWantedExtensions.Any(m => fileExtension == m))
            {
                throw new($"Cant handle compressed files with symlink downloader");
            }

            DownloadProgress?.Invoke(this,
                                     new()
                                     {
                                         BytesDone = 0,
                                         BytesTotal = 0,
                                         Speed = 0
                                     });
            
            String? file = null;
            var shouldSearch = true;

            // When resolving symlinks for AllDebrid, we know the exact file path, so we can skip the search.
            if (clientKind == Provider.AllDebrid)
            {
                var potentialFilePath = Path.Combine(rcloneMountPath, path);
                
                // Make sure the file exists before making any assumptions.
                // If this somehow fails, fallback to the search below.
                if (File.Exists(potentialFilePath))
                {
                    _logger.Debug($"Found file {path} at {potentialFilePath} using direct search");
                    file = potentialFilePath;
                    shouldSearch = false;
                }
                else
                {
                    // Log if the file wasn't found and continue searching.
                    _logger.Warning($"Expected file {path} to be at {potentialFilePath} but it wasn't found. Continuing search (this will probably fail).");
                }
            }

            if (shouldSearch)
            {
                var potentialFilePaths = new List<String>
                {
                    searchPath
                };

                var directoryInfo = new DirectoryInfo(searchPath);

                while (directoryInfo.Parent != null)
                {
                    potentialFilePaths.Add(directoryInfo.Name);
                    directoryInfo = directoryInfo.Parent;

                    if (directoryInfo.FullName.TrimEnd(['\\', '/']) == rcloneMountPath)
                    {
                        break;
                    }
                }

                potentialFilePaths.Add(fileName);
                potentialFilePaths.Add(fileNameWithoutExtension);

                // add an empty path so we can check for the new file in the base directory
                potentialFilePaths.Add("");

                potentialFilePaths = potentialFilePaths.Distinct().ToList();

                for (var retryCount = 0; retryCount < MaxRetries; retryCount++)
                {
                    DownloadProgress?.Invoke(this,
                                             new()
                                             {
                                                 BytesDone = retryCount,
                                                 BytesTotal = 10,
                                                 Speed = 1
                                             });

                    _logger.Debug($"Searching {rcloneMountPath} for {fileName} (attempt #{retryCount})...");

                    file = FindFile(rcloneMountPath, potentialFilePaths, fileName);

                    if (file == null && searchSubDirectories)
                    {
                        var subDirectories = Directory.GetDirectories(rcloneMountPath, "*.*", SearchOption.TopDirectoryOnly);

                        foreach (var subDirectory in subDirectories)
                        {
                            file = FindFile(Path.Combine(rcloneMountPath, subDirectory), potentialFilePaths, fileName);

                            if (file != null)
                            {
                                break;
                            }
                        }
                    }

                    if (file == null)
                    {
                        await Task.Delay(1000 * retryCount);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (file == null)
            {
                _logger.Debug($"Unable to find file in rclone mount. Folders available in {rcloneMountPath}: ");

                try
                {
                    var allFolders = FileHelper.GetDirectoryContents(rcloneMountPath);

                    _logger.Debug(allFolders);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                }

                throw new("Could not find file from rclone mount!");
            }

            _logger.Debug($"Creating symbolic link from {file} to {destinationPath}");

            var result = TryCreateSymbolicLink(file, destinationPath);

            if (!result)
            {
                throw new("Could not find file from rclone mount!");
            }

            DownloadComplete?.Invoke(this, new());

            return file;
        }
        catch (Exception ex)
        {
            DownloadComplete?.Invoke(this, new()
            {
                Error = ex.Message
            });

            throw;
        }
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

    private String? FindFile(String rootPath, List<String> filePaths, String fileName)
    {
        foreach (var potentialFilePath in filePaths)
        {
            var potentialFilePathWithFileName = Path.Combine(rootPath, potentialFilePath, fileName);

            _logger.Debug($"Searching {potentialFilePathWithFileName}...");

            if (File.Exists(potentialFilePathWithFileName))
            {
                return potentialFilePathWithFileName;
            }
        }

        return null;
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

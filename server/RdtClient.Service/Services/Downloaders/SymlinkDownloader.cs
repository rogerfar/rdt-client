using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace RdtClient.Service.Services.Downloaders
{
    public class SymlinkDownloader : IDownloader
    {
        public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

        private const Int32 RetryCount = 5;
        private readonly string _filePath;
        private readonly string _uri;
        private readonly string _rcloneMountPath;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationToken = new();
        private bool _completed;

        public SymlinkDownloader(string uri, string filePath, string rcloneMountPath)
        {
            _logger = Log.ForContext<SymlinkDownloader>();

            _uri = uri;
            _filePath = filePath;
            _rcloneMountPath = rcloneMountPath;
        }

        public async Task<String?> Download()
        {
            _logger.Debug($"Starting download of {_uri}, writing to path: {_filePath}");

            string fileName = Path.GetFileName(_filePath);
            _completed = false;

            // Sometimes the rclone mount doesn't immediately reflect the new files,
            // so try for up to 5 minutes (10 attempts, 30 seconds between attempts).
            // Would be better to fail properly and have RDT auto retry, but unsure how to do that.

            var retryCount = 0;
            while (retryCount < RetryCount && !_completed)
            {
                _logger.Debug($"(Attempt {retryCount}/{RetryCount}) Searching {_rcloneMountPath} for {fileName}");
                // Recursively search for the fileName in rclone mount location
                string[] foundFiles = Directory.GetFiles(_rcloneMountPath, fileName, SearchOption.AllDirectories);

                if (foundFiles.Length > 0)
                {
                    if (foundFiles.Length > 1)
                    {
                        _logger.Warning($"Found {foundFiles.Length} files named {fileName}");
                    }
                    // Assume first matching filename is the one we want
                    string actualFilePath = foundFiles[0];

                    bool result = TryCreateSymbolicLink(actualFilePath, _filePath).Result;
                    if (result)
                    {
                        _completed = true;
                        DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs{});
                        return actualFilePath;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), _cancellationToken.Token);

                retryCount++;
            }

            _logger.Error($"File '{fileName}' not found after {RetryCount} attempts.");
            return null;
        }

        private Task<bool> TryCreateSymbolicLink(string sourcePath, string symlinkPath)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "ln";
                process.StartInfo.Arguments = $"-s \"{sourcePath}\" \"{symlinkPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    _logger.Information($"Created symbolic link from {sourcePath} to {symlinkPath}");
                    return Task.FromResult<bool>(true);
                }
                else
                {
                    _logger.Error($"Failed to create symbolic link: {process.ExitCode}");
                    return Task.FromResult<bool>(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating symbolic link from {sourcePath} to {symlinkPath}: {ex.Message}");
                return Task.FromResult<bool>(false);
            }
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
    }
}

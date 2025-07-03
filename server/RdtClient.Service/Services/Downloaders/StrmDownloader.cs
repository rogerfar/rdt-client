using System.IO.Abstractions;

namespace RdtClient.Service.Services.Downloaders;

public class StrmDownloader(String downloadLink, String filePath, IFileSystem fileSystem) : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    public async Task<String> Download()
    {
        try
        {
            await fileSystem.File.WriteAllTextAsync(filePath + ".strm", downloadLink);

            DownloadComplete?.Invoke(this, new());
        }
        catch (Exception ex)
        {
            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = ex.Message
                                     });
        }

        return Guid.NewGuid().ToString();
    }

    public Task Cancel()
    {
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

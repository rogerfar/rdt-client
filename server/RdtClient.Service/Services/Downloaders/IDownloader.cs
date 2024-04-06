namespace RdtClient.Service.Services.Downloaders;

public class DownloadCompleteEventArgs
{
    public String? Error { get; set; }
}

public class DownloadProgressEventArgs
{
    public Int64 Speed { get; set; }
    public Int64 BytesDone { get; set; }
    public Int64 BytesTotal { get; set; }
}

public interface IDownloader
{
    event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
    Task<String> Download();
    Task Cancel();
    Task Pause();
    Task Resume();
}
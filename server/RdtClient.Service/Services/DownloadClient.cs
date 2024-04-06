using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.Downloaders;

namespace RdtClient.Service.Services;

public class DownloadClient(Download download, Torrent torrent, String destinationPath)
{
    public IDownloader? Downloader;

    public Data.Enums.DownloadClient Type { get; private set; }

    public Boolean Finished { get; private set; }

    public String? Error { get; private set; }

    public Int64 Speed { get; private set; }
    public Int64 BytesTotal { get; private set; }
    public Int64 BytesDone { get; private set; }

    public async Task<String> Start()
    {
        BytesDone = 0;
        BytesTotal = 0;
        Speed = 0;

        try
        {
            if (download.Link == null)
            {
                throw new($"Invalid download link");
            }

            var filePath = DownloadHelper.GetDownloadPath(destinationPath, torrent, download);
            var downloadPath = DownloadHelper.GetDownloadPath(torrent, download);

            if (filePath == null || downloadPath == null)
            {
                throw new("Invalid download path");
            }

            await FileHelper.Delete(filePath);

            Type = Settings.Get.DownloadClient.Client;

            Downloader = Settings.Get.DownloadClient.Client switch
            {
                Data.Enums.DownloadClient.Internal => new InternalDownloader(download.Link, filePath),
                Data.Enums.DownloadClient.Bezzad => new BezzadDownloader(download.Link, filePath),
                Data.Enums.DownloadClient.Aria2c => new Aria2cDownloader(download.RemoteId, download.Link, filePath, downloadPath),
                Data.Enums.DownloadClient.Symlink => new SymlinkDownloader(download.Link, filePath),
                _ => throw new($"Unknown download client {Settings.Get.DownloadClient}")
            };

            Downloader.DownloadComplete += (_, args) =>
            {
                Finished = true;
                Error ??= args.Error;
            };

            Downloader.DownloadProgress += (_, args) =>
            {
                Speed = args.Speed;
                BytesDone = args.BytesDone;
                BytesTotal = args.BytesTotal;
            };

            var result = await Downloader.Download();
                
            await Task.Delay(1000);

            return result;
        }
        catch (Exception ex)
        {
            if (Downloader != null)
            {
                await Downloader.Cancel();
            }

            Finished = true;

            throw new($"An unexpected error occurred preparing download {download.Link} for torrent {torrent.RdName}: {ex.Message}");
        }
    }

    public async Task Cancel()
    {
        Finished = true;
        Error = null;

        if (Downloader == null)
        {
            return;
        }
        await Downloader.Cancel();
    }

    public async Task Pause()
    {
        if (Downloader == null)
        {
            return;
        }
        await Downloader.Pause();
    }

    public async Task Resume()
    {
        if (Downloader == null)
        {
            return;
        }
        await Downloader.Resume();
    }
}
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.Downloaders;
using RdtClient.Service.Services.TorrentClients;

namespace RdtClient.Service.Services;

public class DownloadClient(Download download, Torrent torrent, String destinationPath, String? category)
{
    private static Int64 _totalBytesDownloadedThisSession;
    private static readonly Lock TotalBytesDownloadedLock = new();

    public IDownloader? Downloader;

    public Data.Enums.DownloadClient Type { get; private set; }

    public Boolean Finished { get; private set; }

    public String? Error { get; private set; }

    public Int64 Speed { get; private set; }
    public Int64 BytesTotal { get; private set; }
    public Int64 BytesDone { get; private set; }

    private Int64 LastBytesDone { get; set; }

    public async Task<String> Start()
    {
        BytesDone = 0;
        BytesTotal = 0;
        Speed = 0;

        try
        {
            Type = torrent.DownloadClient;

            if (download.Link == null)
            {
                throw new($"Invalid download link");
            }

            var filePath = DownloadHelper.GetDownloadPath(destinationPath, torrent, download);
            var downloadPath = DownloadHelper.GetDownloadPath(torrent, download);

            if (torrent.ClientKind == Provider.AllDebrid && Type == Data.Enums.DownloadClient.Symlink)
            {
                downloadPath = AllDebridTorrentClient.GetSymlinkPath(torrent, download);
            }

            if (torrent.ClientKind == Provider.DebridLink && Type == Data.Enums.DownloadClient.Symlink)
            {
                downloadPath = DebridLinkClient.GetSymlinkPath(torrent, download);
            }

            if (filePath == null || downloadPath == null)
            {
                throw new("Invalid download path");
            }

            if (Type != Data.Enums.DownloadClient.Symlink)
            {
                await FileHelper.Delete(filePath);
            }

            Downloader = Type switch
            {
                Data.Enums.DownloadClient.Bezzad => new BezzadDownloader(download.Link, filePath),
                Data.Enums.DownloadClient.Aria2c => new Aria2cDownloader(download.RemoteId, download.Link, filePath, downloadPath, category),
                Data.Enums.DownloadClient.Symlink => new SymlinkDownloader(download.Link, filePath, downloadPath, torrent.ClientKind),
                Data.Enums.DownloadClient.DownloadStation => await DownloadStationDownloader.Init(download.RemoteId, download.Link, filePath, downloadPath, category),
                _ => throw new($"Unknown download client {Type}")
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

                var bytesAdded = BytesDone - LastBytesDone;

                LastBytesDone = BytesDone;

                AddToTotalBytesDownloadedThisSession(bytesAdded);
            };

            var result = await Downloader.Download();

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

    public static Int64 GetTotalBytesDownloadedThisSession()
    {
        lock (TotalBytesDownloadedLock)
        {
            return _totalBytesDownloadedThisSession;
        }
    }

    private static void AddToTotalBytesDownloadedThisSession(Int64 bytes)
    {
        lock (TotalBytesDownloadedLock)
        {
            _totalBytesDownloadedThisSession += bytes;
        }
    }
}
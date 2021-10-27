using System;
using System.Threading.Tasks;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.Downloaders;

namespace RdtClient.Service.Services
{
    public class DownloadClient
    {
        private readonly String _destinationPath;

        private readonly Download _download;
        private readonly Torrent _torrent;

        public IDownloader Downloader;

        public DownloadClient(Download download, Torrent torrent, String destinationPath)
        {
            _download = download;
            _torrent = torrent;
            _destinationPath = destinationPath;
        }

        public String Type { get; set; }

        public Boolean Finished { get; private set; }

        public String Error { get; private set; }

        public Int64 Speed { get; private set; }
        public Int64 BytesTotal { get; private set; }
        public Int64 BytesDone { get; private set; }

        public async Task<String> Start(DbSettings settings)
        {
            BytesDone = 0;
            BytesTotal = 0;
            Speed = 0;

            try
            {
                var filePath = DownloadHelper.GetDownloadPath(_destinationPath, _torrent, _download);

                if (filePath == null)
                {
                    throw new Exception("Invalid download path");
                }
                
                await FileHelper.Delete(filePath);

                Type = settings.DownloadClient;

                Downloader = settings.DownloadClient switch
                {
                    "Simple" => new SimpleDownloader(_download.Link, filePath),
                    "MultiPart" => new MultiDownloader(_download.Link, filePath, settings),
                    "Aria2c" => new Aria2cDownloader(_download.RemoteId, _download.Link, filePath, settings),
                    _ => throw new Exception($"Unknown download client {settings.DownloadClient}")
                };

                Downloader.DownloadComplete += (_, args) =>
                {
                    Finished = true;
                    Error = args.Error;
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
                Error = $"An unexpected error occurred preparing download {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
                Finished = true;

                return null;
            }
        }

        public async Task Cancel()
        {
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
}

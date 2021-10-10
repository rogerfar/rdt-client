using System;
using System.IO;
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

        private IDownloader _downloader;

        public DownloadClient(Download download, Torrent torrent, String destinationPath)
        {
            _download = download;
            _torrent = torrent;
            _destinationPath = destinationPath;
        }

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
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                _downloader = settings.DownloadClient switch
                {
                    "Simple" => new SimpleDownloader(_download.Link, filePath),
                    "MultiPart" => new MultiDownloader(_download.Link, filePath, settings),
                    "Aria2c" => new Aria2cDownloader(_download.RemoteId, _download.Link, filePath, settings),
                    _ => throw new Exception($"Unknown download client {settings.DownloadClient}")
                };

                _downloader.DownloadComplete += (_, args) =>
                {
                    Finished = true;
                    Error = args.Error;
                };

                _downloader.DownloadProgress += (_, args) =>
                {
                    Speed = args.Speed;
                    BytesDone = args.BytesDone;
                    BytesTotal = args.BytesTotal;
                };

                return await _downloader.Download();
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred preparing download {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
                Finished = true;

                return null;
            }
        }

        public void Cancel()
        {
            _downloader?.Cancel();
        }
    }
}

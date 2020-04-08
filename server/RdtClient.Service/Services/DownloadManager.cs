using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace RdtClient.Service.Services
{
    public static class DownloadManager
    {
        public static readonly Dictionary<Guid, Download> ActiveDownloads = new Dictionary<Guid, Download>();

        static DownloadManager()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.MaxServicePointIdleTime = 1000;
        }

        public static async Task Download(Download download, String destinationFolderPath)
        {
            await UpdateStatus(download.DownloadId, DownloadStatus.Downloading, TorrentStatus.Downloading);

            if (!ActiveDownloads.TryAdd(download.DownloadId, download))
            {
                return;
            }

            download.Progress = 0;
            download.BytesLastUpdate = 0;
            download.NextUpdate = DateTime.UtcNow.AddSeconds(1);
            download.Speed = 0;

            var fileUrl = download.Link;

            var uri = new Uri(fileUrl);
            var filePath = Path.Combine(destinationFolderPath, uri.Segments.Last());

            if (!Directory.Exists(destinationFolderPath))
            {
                Directory.CreateDirectory(destinationFolderPath);
            }

            var webRequest = WebRequest.Create(fileUrl);
            webRequest.Method = "HEAD";
            Int64 responseLength;
            using (var webResponse = await webRequest.GetResponseAsync())
            {
                responseLength = Int64.Parse(webResponse.Headers.Get("Content-Length"));
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var request = WebRequest.Create(fileUrl);
            using (var response = await request.GetResponseAsync())
            {
                await using var stream = response.GetResponseStream();
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write);
                var buffer = new Byte[4096];

                while (fileStream.Length < response.ContentLength)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);

                    if (read > 0)
                    {
                        fileStream.Write(buffer, 0, read);

                        ActiveDownloads[download.DownloadId].Progress = (Int32) (fileStream.Length * 100 / responseLength);

                        if (DateTime.UtcNow > ActiveDownloads[download.DownloadId].NextUpdate)
                        {
                            ActiveDownloads[download.DownloadId].Speed = fileStream.Length - ActiveDownloads[download.DownloadId].BytesLastUpdate;
                            ActiveDownloads[download.DownloadId].NextUpdate = DateTime.UtcNow.AddSeconds(1);
                            ActiveDownloads[download.DownloadId].BytesLastUpdate = fileStream.Length;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            ActiveDownloads[download.DownloadId].Speed = 0;

            try
            {
                if (filePath.EndsWith(".rar"))
                {
                    await UpdateStatus(download.DownloadId, DownloadStatus.Unpacking, TorrentStatus.Downloading);

                    await using (Stream stream = File.OpenRead(filePath))
                    {
                        var reader = ReaderFactory.Open(stream);
                        while (reader.MoveToNextEntry())
                        {
                            if (reader.Entry.IsDirectory)
                            {
                                continue;
                            }

                            reader.WriteEntryToDirectory(destinationFolderPath,
                                                         new ExtractionOptions
                                                         {
                                                             ExtractFullPath = true,
                                                             Overwrite = true
                                                         });
                        }
                    }

                    var retryCount = 0;
                    while (File.Exists(filePath) && retryCount < 10)
                    {
                        retryCount++;

                        try
                        {
                            File.Delete(filePath);
                        }
                        catch
                        {
                            await Task.Delay(1000);
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }

            await UpdateStatus(download.DownloadId, DownloadStatus.Finished, TorrentStatus.Finished);

            ActiveDownloads.Remove(download.DownloadId);
        }

        private static async Task UpdateStatus(Guid downloadId, DownloadStatus downloadStatus, TorrentStatus torrentStatus)
        {
            await using var context = new DataContext();

            var download = await context.Downloads.FirstOrDefaultAsync(m => m.DownloadId == downloadId);

            download.Status = downloadStatus;

            await context.SaveChangesAsync();

            var torrent = await context.Torrents.FirstOrDefaultAsync(m => m.TorrentId == download.TorrentId);

            if (torrentStatus == TorrentStatus.Finished)
            {
                var allDownloads = await context.Downloads.Where(m => m.TorrentId == download.TorrentId)
                                                .ToListAsync();

                if (allDownloads.All(m => m.Status == DownloadStatus.Finished))
                {
                    torrent.Status = TorrentStatus.Finished;
                }
                else
                {
                    torrent.Status = TorrentStatus.Downloading;
                }
            }
            else
            {
                torrent.Status = torrentStatus;
            }

            await context.SaveChangesAsync();
        }
    }
}
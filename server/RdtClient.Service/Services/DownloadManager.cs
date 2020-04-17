using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

namespace RdtClient.Service.Services
{
    public class DownloadManager
    {
        private Int64 _bytesLastUpdate;

        private DateTime _nextUpdate;

        private RarArchiveEntry _rarCurrentEntry;
        private Dictionary<String, Int64> _rarfileStatus;

        public DownloadManager()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.MaxServicePointIdleTime = 1000;
        }

        public DownloadStatus? NewStatus { get; set; }
        public Download Download { get; set; }
        public Int64 Speed { get; private set; }
        public Int64 BytesDownloaded { get; private set; }
        public Int64 BytesSize { get; private set; }

        private DownloadManager ActiveDownload => TaskRunner.ActiveDownloads[Download.DownloadId];

        public async Task Start(String destinationFolderPath, String rootfolderPath)
        {
            ActiveDownload.NewStatus = DownloadStatus.Downloading;
            ActiveDownload.BytesDownloaded = 0;
            ActiveDownload.BytesSize = 0;
            ActiveDownload.Speed = 0;

            _bytesLastUpdate = 0;
            _nextUpdate = DateTime.UtcNow.AddSeconds(1);

            var fileUrl = Download.Link;

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

                        ActiveDownload.BytesDownloaded = fileStream.Length;
                        ActiveDownload.BytesSize = responseLength;

                        if (DateTime.UtcNow > _nextUpdate)
                        {
                            ActiveDownload.Speed = fileStream.Length - _bytesLastUpdate;

                            _nextUpdate = DateTime.UtcNow.AddSeconds(1);
                            _bytesLastUpdate = fileStream.Length;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            ActiveDownload.Speed = 0;
            ActiveDownload.BytesDownloaded = ActiveDownload.BytesSize;

            try
            {
                if (filePath.EndsWith(".rar"))
                {
                    ActiveDownload.NewStatus = DownloadStatus.Unpacking;

                    await using (Stream stream = File.OpenRead(filePath))
                    {
                        using var archive = RarArchive.Open(stream);

                        ActiveDownload.BytesSize = archive.TotalSize;

                        var entries = archive.Entries.Where(entry => !entry.IsDirectory)
                                             .ToList();

                        _rarfileStatus = entries.ToDictionary(entry => entry.Key, entry => 0L);
                        _rarCurrentEntry = null;
                        archive.CompressedBytesRead += ArchiveOnCompressedBytesRead;

                        foreach (var entry in entries)
                        {
                            _rarCurrentEntry = entry;

                            entry.WriteToDirectory(rootfolderPath,
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

            ActiveDownload.Speed = 0;
            ActiveDownload.BytesDownloaded = ActiveDownload.BytesSize;
            ActiveDownload.NewStatus = DownloadStatus.Finished;
        }

        private void ArchiveOnCompressedBytesRead(Object sender, CompressedBytesReadEventArgs e)
        {
            if (_rarCurrentEntry == null)
            {
                return;
            }

            _rarfileStatus[_rarCurrentEntry.Key] = e.CompressedBytesRead;

            ActiveDownload.BytesDownloaded = _rarfileStatus.Sum(m => m.Value);
        }
    }
}
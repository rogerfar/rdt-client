using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace RdtClient.Service.Services
{
    public class DownloadManager
    {
        public DownloadStatus? NewStatus { get; set; }
        public Download Download { get; set; }
        public Int64 Speed { get; private set; }
        public Int64 BytesDownloaded { get; private set; }
        public Int64 BytesSize { get; private set; }

        private DateTime _nextUpdate;
        private Int64 _bytesLastUpdate;

        public DownloadManager()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.MaxServicePointIdleTime = 1000;
        }

        private DownloadManager ActiveDownload => TaskRunner.ActiveDownloads[Download.DownloadId];

        public async Task Start(String destinationFolderPath)
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

            ActiveDownload.NewStatus = DownloadStatus.Finished;
        }
    }
}
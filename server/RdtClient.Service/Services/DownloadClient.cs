using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Services
{
    public class DownloadClient
    {
        public Boolean Finished { get; private set; }
        
        public String Error { get; private set; }
        
        public Int64 Speed { get; private set; }
        public Int64 BytesTotal { get; private set; }
        public Int64 BytesDone { get; private set; }

        private readonly Download _download;
        private readonly String _destinationPath;
        private readonly Torrent _torrent;
        
        private Int64 _bytesLastUpdate;
        private DateTime _nextUpdate;

        public DownloadClient(Download download, String destinationPath)
        {
            _download = download;
            _destinationPath = destinationPath;
            _torrent = download.Torrent;
        }

        public async Task Start()
        {
            BytesDone = 0;
            BytesTotal = 0;
            Speed = 0;

            try
            {
                var fileUrl = _download.Link;

                if (String.IsNullOrWhiteSpace(fileUrl))
                {
                    throw new Exception("File URL is empty");
                }

                var uri = new Uri(fileUrl);
                var torrentPath = Path.Combine(_destinationPath, _torrent.RdName);

                if (!Directory.Exists(torrentPath))
                {
                    Directory.CreateDirectory(torrentPath);
                }

                var fileName = uri.Segments.Last();
                var filePath = Path.Combine(torrentPath, fileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await Task.Factory.StartNew(async delegate
                {
                    await Download(uri, filePath);
                });
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred preparing download {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
                Finished = true;
            }
        }

        private async Task Download(Uri uri, String filePath)
        {
            try
            {
                _bytesLastUpdate = 0;
                _nextUpdate = DateTime.UtcNow.AddSeconds(1);

                // Determine the file size
                var webRequest = WebRequest.Create(uri);
                webRequest.Method = "HEAD";
                webRequest.Timeout = 5000;
                Int64 responseLength;

                using (var webResponse = await webRequest.GetResponseAsync())
                {
                    responseLength = Int64.Parse(webResponse.Headers.Get("Content-Length"));
                }

                var timeout = DateTimeOffset.UtcNow.AddHours(1);

                while (timeout > DateTimeOffset.UtcNow)
                {
                    try
                    {
                        var request = WebRequest.Create(uri);
                        using var response = await request.GetResponseAsync();

                        await using var stream = response.GetResponseStream();

                        if (stream == null)
                        {
                            throw new IOException("No stream");
                        }

                        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write);
                        var buffer = new Byte[4096];

                        while (fileStream.Length < response.ContentLength)
                        {
                            var read = await stream.ReadAsync(buffer, 0, buffer.Length);

                            if (read > 0)
                            {
                                fileStream.Write(buffer, 0, read);

                                BytesDone = fileStream.Length;
                                BytesTotal = responseLength;

                                if (DateTime.UtcNow > _nextUpdate)
                                {
                                    Speed = fileStream.Length - _bytesLastUpdate;

                                    _nextUpdate = DateTime.UtcNow.AddSeconds(1);
                                    _bytesLastUpdate = fileStream.Length;
                                    
                                    timeout = DateTimeOffset.UtcNow.AddHours(1);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        break;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(1000);
                    }
                    catch (WebException)
                    {
                        await Task.Delay(1000);
                    }
                }

                if (timeout <= DateTimeOffset.UtcNow)
                {
                    throw new Exception($"Download timed out");
                }

                Speed = 0;
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred downloading {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
            }
            finally
            {
                Finished = true;
            }
        }
    }
}

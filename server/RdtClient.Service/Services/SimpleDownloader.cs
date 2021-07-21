using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;

namespace RdtClient.Service.Services
{
    public class SimpleDownloader
    {
        public Int64 Speed { get; private set; }
        public Int64 BytesTotal { get; private set; }
        public Int64 BytesDone { get; private set; }

        public event EventHandler<AsyncCompletedEventArgs> DownloadFileCompleted;
        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

        private Boolean _cancelled = false;

        private Int64 _bytesLastUpdate;
        private DateTime _nextUpdate;

        public async Task Download(Uri uri, String filePath)
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

                while (timeout > DateTimeOffset.UtcNow && !_cancelled)
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
                        var buffer = new Byte[64 * 1024];

                        while (fileStream.Length < response.ContentLength && !_cancelled)
                        {
                            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

                            if (read > 0)
                            {
                                await fileStream.WriteAsync(buffer.AsMemory(0, read));

                                BytesDone = fileStream.Length;
                                BytesTotal = responseLength;

                                if (DateTime.UtcNow > _nextUpdate)
                                {
                                    Speed = fileStream.Length - _bytesLastUpdate;

                                    _nextUpdate = DateTime.UtcNow.AddSeconds(1);
                                    _bytesLastUpdate = fileStream.Length;

                                    timeout = DateTimeOffset.UtcNow.AddHours(1);

                                    DownloadProgressChanged?.Invoke(this, new DownloadProgressChangedEventArgs(null)
                                    {
                                        BytesPerSecondSpeed = Speed,
                                        ProgressedByteSize = _bytesLastUpdate,
                                        TotalBytesToReceive = BytesTotal,
                                        AverageBytesPerSecondSpeed = Speed,
                                        ReceivedBytesSize = BytesDone,
                                    });
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

                DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, false, null));
            }
            catch (Exception ex)
            {
                DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(ex, false, null));
            }
        }

        public void Cancel()
        {
            _cancelled = true;
        }
    }
}

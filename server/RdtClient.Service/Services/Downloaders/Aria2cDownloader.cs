using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Aria2NET;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Services.Downloaders
{
    public class Aria2cDownloader : IDownloader
    {
        public event EventHandler<DownloadCompleteEventArgs> DownloadComplete;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        private readonly String _uri;
        private readonly String _filePath;

        private readonly Aria2NetClient _aria2NetClient;
        private readonly Timer _timer;

        private String _gid;

        public Aria2cDownloader(String gid, String uri, String filePath, DbSettings settings)
        {
            _gid = gid;
            _uri = uri;
            _filePath = filePath;

            _aria2NetClient = new Aria2NetClient(settings.Aria2cUrl, settings.Aria2cSecret);

            _timer = new Timer();

            _timer.Elapsed += OnTimedEvent;

            _timer.Interval = 1000;
            _timer.Enabled = false;
        }
        
        public async Task<String> Download()
        {
            var path = Path.GetDirectoryName(_filePath);
            var fileName = Path.GetFileName(_filePath);

            if (_gid != null)
            {
                try
                {
                    await _aria2NetClient.TellStatus(_gid);
                }
                catch
                {
                    _gid = null;
                }
            }
            
            _gid ??= await _aria2NetClient.AddUri(new List<String>
                                                  {
                                                      _uri
                                                  },
                                                  new Dictionary<String, Object>
                                                  {
                                                      {
                                                          "dir", path
                                                      },
                                                      {
                                                          "out", fileName
                                                      }
                                                  });

            _timer.Start();

            return _gid;
        }

        public async Task Cancel()
        {
            _timer.Stop();

            if (String.IsNullOrWhiteSpace(_gid))
            {
                return;
            }

            try
            {
                await _aria2NetClient.ForceRemove(_gid);
            }
            catch
            {
                // ignored
            }

            try
            {
                await _aria2NetClient.RemoveDownloadResult(_gid);
            }
            catch
            {
                // ignored
            }
        }

        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (_gid == null)
            {
                return;
            }

            try
            {
                var status = await _aria2NetClient.TellStatus(_gid);

                if (!String.IsNullOrWhiteSpace(status.ErrorMessage) || status.Status == "error")
                {
                    await Cancel();
                    DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs
                    {
                        Error = $"{status.ErrorCode}: {status.ErrorMessage}"
                    });
                    return;
                }

                if (status.Status == "complete" || status.Status == "removed")
                {
                    await Cancel();
                    DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs());
                    return;
                }

                DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
                {
                    BytesDone = status.CompletedLength,
                    BytesTotal = status.TotalLength,
                    Speed = status.DownloadSpeed
                });
            }
            catch
            {
                await Cancel();
                DownloadComplete?.Invoke(this, new DownloadCompleteEventArgs());
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RdtClient.Data.Models.Data;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;

namespace RdtClient.Service.Services
{
    public class UnpackClient
    {
        public Boolean Finished { get; private set; }
        
        public String Error { get; private set; }
        
        public Int64 BytesTotal { get; private set; }
        public Int64 BytesDone { get; private set; }
        
        private readonly Download _download;
        private readonly String _destinationPath;
        private readonly Torrent _torrent;

        private Boolean _cancelled = false;
        
        private RarArchiveEntry _rarCurrentEntry;
        private Dictionary<String, Int64> _rarfileStatus;

        public UnpackClient(Download download, String destinationPath)
        {
            _download = download;
            _destinationPath = destinationPath;
            _torrent = download.Torrent;
        }

        public async Task Start()
        {
            BytesDone = 0;
            BytesTotal = 0;

            try
            {
                var fileUrl = _download.Link;

                if (String.IsNullOrWhiteSpace(fileUrl))
                {
                    throw new Exception("File URL is empty");
                }

                var uri = new Uri(fileUrl);
                var torrentPath = Path.Combine(_destinationPath, _torrent.RdName);

                var fileName = uri.Segments.Last();
                var filePath = Path.Combine(torrentPath, fileName);
                
                if (!File.Exists(filePath))
                {
                    throw new Exception($"File {filePath} could not be extracted because it is missing");
                }

                await Task.Factory.StartNew(async delegate
                {
                    if (!_cancelled)
                    {
                        await Unpack(filePath);
                    }
                });
            }
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred preparing download {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
                Finished = true;
            }
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        private async Task Unpack(String filePath)
        {
            try
            {
                await using (Stream stream = File.OpenRead(filePath))
                {
                    using var archive = RarArchive.Open(stream);

                    BytesTotal = archive.TotalSize;

                    var entries = archive.Entries.Where(entry => !entry.IsDirectory)
                                         .ToList();

                    _rarfileStatus = entries.ToDictionary(entry => entry.Key, entry => 0L);
                    _rarCurrentEntry = null;
                    archive.CompressedBytesRead += ArchiveOnCompressedBytesRead;

                    var extractPath = _destinationPath;

                    if (!entries.Any(m => m.Key.StartsWith(_torrent.RdName + @"\")) && !entries.Any(m => m.Key.StartsWith(_torrent.RdName + @"/")))
                    {
                        extractPath = Path.Combine(_destinationPath, _torrent.RdName);
                    }

                    if (entries.Any(m => m.Key.Contains(".r00")))
                    {
                        extractPath = Path.Combine(extractPath, "Temp");
                    }

                    foreach (var entry in entries)
                    {
                        if (_cancelled)
                        {
                            return;
                        }
                        
                        _rarCurrentEntry = entry;

                        entry.WriteToDirectory(extractPath,
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
            catch (Exception ex)
            {
                Error = $"An unexpected error occurred downloading {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
            }
            finally
            {
                Finished = true;
            }
        }

        private void ArchiveOnCompressedBytesRead(Object sender, CompressedBytesReadEventArgs e)
        {
            if (_rarCurrentEntry == null)
            {
                return;
            }

            _rarfileStatus[_rarCurrentEntry.Key] = e.CompressedBytesRead;

            BytesDone = _rarfileStatus.Sum(m => m.Value);
        }
    }
}

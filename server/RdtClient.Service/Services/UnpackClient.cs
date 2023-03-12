using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace RdtClient.Service.Services;

public class UnpackClient
{
    public Boolean Finished { get; private set; }
        
    public String? Error { get; private set; }
        
    public Int64 BytesTotal { get; private set; }
    public Int64 BytesDone { get; private set; }
        
    private readonly Download _download;
    private readonly String _destinationPath;
    private readonly Torrent _torrent;

    private Boolean _cancelled;
        
    private IArchiveEntry? _rarCurrentEntry;
    private Dictionary<String, Int64>? _rarfileStatus;

    public UnpackClient(Download download, String destinationPath)
    {
        _download = download;
        _destinationPath = destinationPath;
        _torrent = download.Torrent ?? throw new Exception($"Torrent is null");
    }

    public void Start()
    {
        BytesDone = 0;
        BytesTotal = 0;

        try
        {
            var filePath = DownloadHelper.GetDownloadPath(_destinationPath, _torrent, _download);

            if (filePath == null)
            {
                throw new Exception("Invalid download path");
            }

            Task.Run(async delegate
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
            if (!File.Exists(filePath))
            {
                return;
            }

            var extractPath = _destinationPath;
            String? extractPathTemp = null;

            var archiveEntries = await GetArchiveFiles(filePath);

            if (!archiveEntries.Any(m => m.StartsWith(_torrent.RdName + @"\")) && !archiveEntries.Any(m => m.StartsWith(_torrent.RdName + @"/")))
            {
                extractPath = Path.Combine(_destinationPath, _torrent.RdName!);
            }

            if (archiveEntries.Any(m => m.Contains(".r00")))
            {
                extractPathTemp = Path.Combine(extractPath, Guid.NewGuid().ToString());
                
                if (!Directory.Exists(extractPathTemp))
                {
                    Directory.CreateDirectory(extractPathTemp);
                }
            }
            
            if (extractPathTemp != null)
            {
                Extract(filePath, extractPathTemp);

                await FileHelper.Delete(filePath);

                var rarFiles = Directory.GetFiles(extractPathTemp, "*.r00", SearchOption.TopDirectoryOnly);

                foreach (var rarFile in rarFiles)
                {
                    var mainRarFile = Path.ChangeExtension(rarFile, ".rar");

                    if (File.Exists(mainRarFile))
                    {
                        Extract(mainRarFile, extractPath);
                    }

                    await FileHelper.DeleteDirectory(extractPathTemp);
                }
            }
            else
            {
                Extract(filePath, extractPath);

                await FileHelper.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Error = $"An unexpected error occurred unpacking {_download.Link} for torrent {_torrent.RdName}: {ex.Message}";
        }
        finally
        {
            Finished = true;
        }
    }

    private async Task<IList<String>> GetArchiveFiles(String filePath)
    {
        await using Stream stream = File.OpenRead(filePath);

        var extension = Path.GetExtension(filePath);

        IArchive archive;
        if (extension == ".zip")
        {
            archive = ZipArchive.Open(stream);
        }
        else
        {
            archive = RarArchive.Open(stream);
        }

        BytesTotal = archive.TotalSize;

        var entries = archive.Entries
                             .Where(entry => !entry.IsDirectory)
                             .Select(m => m.Key)
                             .ToList();

        archive.Dispose();

        return entries;
    }

    private void Extract(String filePath, String extractPath)
    {
        var parts = ArchiveFactory.GetFileParts(filePath);

        var fi = parts.Select(m => new FileInfo(m));

        var extension = Path.GetExtension(filePath);

        IArchive archive;
        if (extension == ".zip")
        {
            archive = ZipArchive.Open(fi);
        }
        else
        {
            archive = RarArchive.Open(fi);
        }
        
        if (archive.IsComplete)
        {
            BytesTotal = archive.TotalSize;
        }

        var entries = archive.Entries.Where(entry => !entry.IsDirectory)
                             .ToList();

        _rarfileStatus = entries.ToDictionary(entry => entry.Key, _ => 0L);
        _rarCurrentEntry = null;
        archive.CompressedBytesRead += ArchiveOnCompressedBytesRead;

        foreach (var entry in entries)
        {
            if (_cancelled)
            {
                throw new Exception("Task was cancelled");
            }
                        
            _rarCurrentEntry = entry;

            entry.WriteToDirectory(extractPath,
                                   new ExtractionOptions
                                   {
                                       ExtractFullPath = true,
                                       Overwrite = true
                                   });
        }

        archive.Dispose();
    }

    private void ArchiveOnCompressedBytesRead(Object? sender, CompressedBytesReadEventArgs e)
    {
        if (_rarCurrentEntry == null)
        {
            return;
        }

        _rarfileStatus![_rarCurrentEntry.Key] = e.CompressedBytesRead;

        BytesDone = _rarfileStatus.Sum(m => m.Value);
    }
}
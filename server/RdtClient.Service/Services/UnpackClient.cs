using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.DebridClients;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace RdtClient.Service.Services;

public class UnpackClient(Download download, String destinationPath)
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly Torrent _torrent = download.Torrent ?? throw new($"Torrent is null");
    public Boolean Finished { get; private set; }

    public String? Error { get; private set; }

    public Int32 Progess { get; private set; }

    public void Start()
    {
        Progess = 0;

        try
        {
            var filePath = DownloadHelper.GetDownloadPath(destinationPath, _torrent, download) ?? throw new("Invalid download path");

            Task.Run(async delegate
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    await Unpack(filePath, _cancellationTokenSource.Token);
                }
            });
        }
        catch (Exception ex)
        {
            Error = $"An unexpected error occurred preparing download {download.Link} for torrent {_torrent.RdName}: {ex.Message}";
            Finished = true;
        }
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    private async Task Unpack(String filePath, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var extractPath = destinationPath;
            String? extractPathTemp = null;

            var archiveEntries = await GetArchiveFiles(filePath);

            if (!archiveEntries.Any(m => m.StartsWith(_torrent.RdName + @"\")) && !archiveEntries.Any(m => m.StartsWith(_torrent.RdName + "/")))
            {
                extractPath = Path.Combine(destinationPath, _torrent.RdName!);
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
                await Extract(filePath, extractPathTemp, cancellationToken);

                await FileHelper.Delete(filePath);

                var rarFiles = Directory.GetFiles(extractPathTemp, "*.r00", SearchOption.TopDirectoryOnly);

                foreach (var rarFile in rarFiles)
                {
                    var mainRarFile = Path.ChangeExtension(rarFile, ".rar");

                    if (File.Exists(mainRarFile))
                    {
                        await Extract(mainRarFile, extractPath, cancellationToken);
                    }

                    await FileHelper.DeleteDirectory(extractPathTemp);
                }
            }
            else
            {
                await Extract(filePath, extractPath, cancellationToken);

                await FileHelper.Delete(filePath);
            }

            if (_torrent.ClientKind == Provider.TorBox)
            {
                TorBoxDebridClient.MoveHashDirContents(extractPath, _torrent);
            }
        }
        catch (Exception ex)
        {
            Error = $"An unexpected error occurred unpacking {download.Link} for torrent {_torrent.RdName}: {ex.Message}";
        }
        finally
        {
            Finished = true;
        }
    }

    private static async Task<IList<String>> GetArchiveFiles(String filePath)
    {
        await using Stream stream = File.OpenRead(filePath);

        var extension = Path.GetExtension(filePath);

        IArchive archive;

        if (extension == ".zip")
        {
            archive = ZipArchive.OpenArchive(stream);
        }
        else
        {
            archive = RarArchive.OpenArchive(stream);
        }

        var entries = archive.Entries
                             .Where(entry => !entry.IsDirectory)
                             .Select(m => m.Key!)
                             .ToList();

        archive.Dispose();

        return entries;
    }

    private async Task Extract(String filePath, String extractPath, CancellationToken cancellationToken)
    {
        var parts = ArchiveFactory.GetFileParts(filePath);

        var fi = parts.Select(m => new FileInfo(m)).ToList();

        var extension = Path.GetExtension(filePath);

        // The async archive is used purely to keep honouring the cancellation token: the synchronous
        // WriteToDirectory has no overload that accepts one.
        IAsyncArchive archive;

        if (extension == ".zip")
        {
            archive = await ZipArchive.OpenAsyncArchive(fi, cancellationToken: cancellationToken);
        }
        else
        {
            archive = await RarArchive.OpenAsyncArchive(fi, cancellationToken: cancellationToken);
        }

        // A malicious entry makes WriteToDirectoryAsync throw, so dispose through await using to release the archive
        // file handles on that path too.
        await using (archive)
        {
            var progress = new Progress<ProgressReport>(report =>
            {
                if (report.PercentComplete.HasValue)
                {
                    Progess = (Int32)Math.Round(report.PercentComplete.Value);
                }
            });

            // ExtractFullPath recreates the directory structure held in the entry keys, matching the previous behaviour.
            // SharpCompress refuses entries that resolve outside extractPath since 0.48.0 and throws instead, which the
            // caller turns into a download error.
            await archive.WriteToDirectoryAsync(extractPath,
                                                new()
                                                {
                                                    Overwrite = true,
                                                    ExtractFullPath = true
                                                },
                                                progress,
                                                cancellationToken);
        }

        GC.Collect();
    }
}

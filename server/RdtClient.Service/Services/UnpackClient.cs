using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.DebridClients;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;

namespace RdtClient.Service.Services;

public class UnpackClient(Download download, String destinationPath)
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly Torrent _torrent = download.Torrent ?? throw new($"Torrent is null");
    public Boolean Finished { get; private set; }

    public String? Error { get; private set; }

    private Int64 _bytesTotal;
    private Int64 _bytesDone;

    /// <summary>
    ///     Uncompressed size of the archive being extracted, 0 until it is opened.
    /// </summary>
    public Int64 BytesTotal => Interlocked.Read(ref _bytesTotal);

    public Int64 BytesDone => Interlocked.Read(ref _bytesDone);

    public void Start()
    {
        Interlocked.Exchange(ref _bytesTotal, 0);
        Interlocked.Exchange(ref _bytesDone, 0);

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
                Extract(filePath, extractPathTemp, cancellationToken);

                await FileHelper.Delete(filePath);

                var rarFiles = Directory.GetFiles(extractPathTemp, "*.r00", SearchOption.TopDirectoryOnly);

                foreach (var rarFile in rarFiles)
                {
                    var mainRarFile = Path.ChangeExtension(rarFile, ".rar");

                    if (File.Exists(mainRarFile))
                    {
                        Extract(mainRarFile, extractPath, cancellationToken);
                    }

                    await FileHelper.DeleteDirectory(extractPathTemp);
                }
            }
            else
            {
                var parts = Extract(filePath, extractPath, cancellationToken);

                await FileHelper.Delete(filePath);

                if (_torrent.Type == DownloadType.Ddl)
                {
                    foreach (var part in parts)
                    {
                        await FileHelper.Delete(part);
                    }
                }
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
            archive = ZipArchive.Open(stream);
        }
        else
        {
            archive = RarArchive.Open(stream);
        }

        var entries = archive.Entries
                             .Where(entry => !entry.IsDirectory)
                             .Select(m => m.Key!)
                             .ToList();

        archive.Dispose();

        return entries;
    }

    private List<String> Extract(String filePath, String extractPath, CancellationToken cancellationToken)
    {
        var parts = ArchiveFactory.GetFileParts(filePath).ToList();

        var fi = parts.Select(m => new FileInfo(m));

        var extension = Path.GetExtension(filePath);

        using IArchive archive = extension == ".zip" ? ZipArchive.Open(fi) : RarArchive.Open(fi);

        // Extract entry by entry through a counting stream: the library's ExtractToDirectory only reports progress once
        // per file, as a 0-1 fraction, so a release that is a single big video showed 0% until it was done.
        Interlocked.Exchange(ref _bytesTotal, archive.TotalUncompressSize);
        Interlocked.Exchange(ref _bytesDone, 0);

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(extractPath));

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = Path.GetFullPath(Path.Combine(root, entry.Key ?? throw new("Archive entry without a name")));

            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new($"Archive entry {entry.Key} points outside the download folder");
            }

            if (entry.IsDirectory)
            {
                Directory.CreateDirectory(path);

                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
            using var counting = new CountingStream(file, count => Interlocked.Add(ref _bytesDone, count), cancellationToken);
            entry.WriteTo(counting);
        }

        GC.Collect();

        return parts;
    }

    /// <summary>
    ///     Write-only pass-through that reports the bytes written and stops the extraction when it is cancelled.
    /// </summary>
    private sealed class CountingStream(Stream inner, Action<Int64> written, CancellationToken cancellationToken) : Stream
    {
        public override Boolean CanRead => false;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => true;
        public override Int64 Length => throw new NotSupportedException();

        public override Int64 Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<Byte> buffer)
        {
            cancellationToken.ThrowIfCancellationRequested();
            inner.Write(buffer);
            written(buffer.Length);
        }

        public override void Flush()
        {
            inner.Flush();
        }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            throw new NotSupportedException();
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(Int64 value)
        {
            throw new NotSupportedException();
        }
    }
}

using System.IO.Compression;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class UnpackClientTest : IDisposable
{
    private readonly String _root = Path.Combine(Path.GetTempPath(), "rdt-unpack-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private Download ArchiveDownload(params (String Name, Int32 Size)[] entries)
    {
        var torrent = new Torrent
        {
            TorrentId = Guid.NewGuid(),
            RdName = "Release",
            Type = DownloadType.Ddl,
            Hash = "hash"
        };
        var download = new Download
        {
            DownloadId = Guid.NewGuid(),
            TorrentId = torrent.TorrentId,
            Torrent = torrent,
            Path = "https://hoster.example/release.zip",
            Link = "https://debrid.example/dl/release.zip",
            FileName = "release.zip"
        };

        Directory.CreateDirectory(Path.Combine(_root, "Release"));

        using var zip = ZipFile.Open(Path.Combine(_root, "Release", "release.zip"), ZipArchiveMode.Create);

        foreach (var (name, size) in entries)
        {
            using var stream = zip.CreateEntry(name, CompressionLevel.NoCompression).Open();
            stream.Write(new Byte[size]);
        }

        return download;
    }

    private static async Task Run(UnpackClient client)
    {
        client.Start();

        for (var i = 0; i < 500 && !client.Finished; i++)
        {
            await Task.Delay(10);
        }

        Assert.True(client.Finished, "unpacking did not finish");
    }

    [Fact]
    public async Task Unpack_CountsEveryByteWritten()
    {
        var download = ArchiveDownload(("Release/movie.mkv", 3 * 1024 * 1024 + 17), ("Release/Subs/en.srt", 1000));
        var client = new UnpackClient(download, _root);

        await Run(client);

        Assert.Null(client.Error);
        Assert.Equal(3 * 1024 * 1024 + 17 + 1000, client.BytesTotal);
        Assert.Equal(client.BytesTotal, client.BytesDone);
        Assert.Equal(3 * 1024 * 1024 + 17, new FileInfo(Path.Combine(_root, "Release", "movie.mkv")).Length);
        Assert.True(File.Exists(Path.Combine(_root, "Release", "Subs", "en.srt")));
    }

    [Fact]
    public async Task Unpack_RefusesEntriesOutsideTheDownloadFolder()
    {
        var download = ArchiveDownload(("Release/ok.txt", 10), ("../../escaped.txt", 10));
        var client = new UnpackClient(download, _root);

        await Run(client);

        Assert.Contains("outside the download folder", client.Error);
        Assert.False(File.Exists(Path.GetFullPath(Path.Combine(_root, "../../escaped.txt"))));
    }
}

using System.IO.Compression;
using System.Text;
using SharpCompress.Archives;
using SharpCompress.Common;
using BclZipFile = System.IO.Compression.ZipFile;
using SharpCompressZipArchive = SharpCompress.Archives.Zip.ZipArchive;

namespace RdtClient.Service.Test.Services;

/// <summary>
///     Guards the extraction settings used by <see cref="RdtClient.Service.Services.UnpackClient" />.
///     Archive contents come from torrents and usenet, so entry keys are attacker controlled. SharpCompress before
///     0.48.0 combined the entry key with the destination without normalising it (GHSA-6c8g-7p36-r338), which allowed a
///     crafted archive to write files anywhere the process could reach. These tests fail if the dependency is ever
///     downgraded or the extraction options are loosened.
/// </summary>
public class ArchiveExtractionSecurityTest : IDisposable
{
    private readonly String _tempPath = Path.Combine(Path.GetTempPath(), $"rdt-extract-test-{Guid.NewGuid():N}");

    public ArchiveExtractionSecurityTest()
    {
        Directory.CreateDirectory(_tempPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Writes a zip using the BCL so the raw entry keys, including traversal sequences, are preserved verbatim.
    /// </summary>
    private String CreateZip(String name, params (String Key, String Content)[] entries)
    {
        var zipPath = Path.Combine(_tempPath, name);

        using var zip = BclZipFile.Open(zipPath, ZipArchiveMode.Create);

        foreach (var (key, content) in entries)
        {
            using var stream = zip.CreateEntry(key).Open();
            stream.Write(Encoding.UTF8.GetBytes(content));
        }

        return zipPath;
    }

    /// <summary>
    ///     Mirrors <see cref="RdtClient.Service.Services.UnpackClient" />: same open call, same extraction options.
    /// </summary>
    private static async Task Extract(String zipPath, String destination)
    {
        var archive = await SharpCompressZipArchive.OpenAsyncArchive(new FileInfo(zipPath));

        await using (archive)
        {
            await archive.WriteToDirectoryAsync(destination,
                                                new()
                                                {
                                                    Overwrite = true,
                                                    ExtractFullPath = true
                                                });
        }
    }

    [Theory]
    [InlineData("../escaped.txt")]
    [InlineData("../../escaped.txt")]
    [InlineData("sub/../../escaped.txt")]
    public async Task Extract_RefusesEntriesEscapingTheDestination(String maliciousKey)
    {
        var destination = Path.Combine(_tempPath, "root");
        Directory.CreateDirectory(destination);

        var zipPath = CreateZip("evil.zip", (maliciousKey, "PWNED"));

        await Assert.ThrowsAsync<ExtractionException>(() => Extract(zipPath, destination));

        var escapedFiles = Directory.GetFiles(_tempPath, "escaped.txt", SearchOption.AllDirectories);

        Assert.Empty(escapedFiles);
    }

    [Fact]
    public async Task Extract_RefusesRootedEntries()
    {
        var destination = Path.Combine(_tempPath, "root");
        Directory.CreateDirectory(destination);

        // A rooted key makes Path.Combine discard the destination entirely.
        var zipPath = CreateZip("rooted.zip", ("/escaped.txt", "PWNED"));

        var exception = await Record.ExceptionAsync(() => Extract(zipPath, destination));

        // Either the entry is rejected, or it is written inside the destination. It must never land outside of it.
        if (exception != null)
        {
            Assert.IsType<ExtractionException>(exception);
        }

        Assert.Empty(Directory.GetFiles(_tempPath, "escaped.txt", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public async Task Extract_WritesLegitimateEntriesAndPreservesNesting()
    {
        var destination = Path.Combine(_tempPath, "root");
        Directory.CreateDirectory(destination);

        var zipPath = CreateZip("good.zip", ("file.txt", "one"), ("sub/dir/nested.txt", "two"));

        await Extract(zipPath, destination);

        Assert.Equal("one", await File.ReadAllTextAsync(Path.Combine(destination, "file.txt")));
        Assert.Equal("two", await File.ReadAllTextAsync(Path.Combine(destination, "sub", "dir", "nested.txt")));
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Test.Regression;

public class DownloadPathCollisionTests : IAsyncLifetime
{
    private readonly String _databasePath = Path.Combine(Path.GetTempPath(), $"rdt-client-path-collision-{Guid.NewGuid():N}.sqlite");

    [Fact]
    public async Task UpdatePath_WhenSiblingAlreadyUsesPath_SkipsWriteAndKeepsOriginalPath()
    {
        var torrentId = await SeedTorrentWithDownloadsAsync();

        await using var context = CreateContext();

        var downloadData = new DownloadData(context);

        var downloadB = await context.Downloads.AsNoTracking().FirstAsync(m => m.TorrentId == torrentId && m.Path == "https://example.invalid/b");

        var updated = await downloadData.UpdatePath(downloadB.DownloadId, "https://example.invalid/a");

        Assert.False(updated);

        await using var verifyContext = CreateContext();

        var reloaded = await verifyContext.Downloads.AsNoTracking().FirstAsync(m => m.DownloadId == downloadB.DownloadId);

        Assert.Equal("https://example.invalid/b", reloaded.Path);
    }

    [Fact]
    public async Task UpdatePath_WhenSiblingUsesPath_DoesNotPoisonTheChangeTracker()
    {
        var torrentId = await SeedTorrentWithDownloadsAsync();

        await using var context = CreateContext();

        var downloadData = new DownloadData(context);

        var downloads = await context.Downloads.AsNoTracking()
                                     .Where(m => m.TorrentId == torrentId)
                                     .OrderBy(m => m.Path)
                                     .ToListAsync();

        var updated = await downloadData.UpdatePath(downloads[1].DownloadId, "https://example.invalid/a");

        Assert.False(updated);

        var completedAt = DateTimeOffset.UtcNow;

        Exception? exception;

        try
        {
            await downloadData.UpdateCompleted(downloads[1].DownloadId, completedAt);

            exception = null;
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        Assert.Null(exception);

        await using var verifyContext = CreateContext();

        var reloaded = await verifyContext.Downloads.AsNoTracking().FirstAsync(m => m.DownloadId == downloads[1].DownloadId);

        Assert.Equal("https://example.invalid/b", reloaded.Path);
        Assert.NotNull(reloaded.Completed);
    }

    [Fact]
    public async Task UpdatePath_WhenPathIsFree_UpdatesAndReturnsTrue()
    {
        var torrentId = await SeedTorrentWithDownloadsAsync();

        await using var context = CreateContext();

        var downloadData = new DownloadData(context);

        var downloadB = await context.Downloads.AsNoTracking().FirstAsync(m => m.TorrentId == torrentId && m.Path == "https://example.invalid/b");

        var updated = await downloadData.UpdatePath(downloadB.DownloadId, "https://example.invalid/refreshed-b");

        Assert.True(updated);

        await using var verifyContext = CreateContext();

        var reloaded = await verifyContext.Downloads.AsNoTracking().FirstAsync(m => m.DownloadId == downloadB.DownloadId);

        Assert.Equal("https://example.invalid/refreshed-b", reloaded.Path);
    }

    [Fact]
    public async Task UpdatePath_WhenPathIsUnchanged_ReturnsTrueWithoutWriting()
    {
        var torrentId = await SeedTorrentWithDownloadsAsync();

        await using var context = CreateContext();

        var downloadData = new DownloadData(context);

        var downloadA = await context.Downloads.AsNoTracking().FirstAsync(m => m.TorrentId == torrentId && m.Path == "https://example.invalid/a");

        var updated = await downloadData.UpdatePath(downloadA.DownloadId, "https://example.invalid/a");

        Assert.True(updated);
    }

    [Fact]
    public async Task SeedSchema_EnforcesUniqueTorrentIdAndPathIndex()
    {
        var torrentId = await SeedTorrentWithDownloadsAsync();

        await using var context = CreateContext();

        var downloadB = await context.Downloads.AsNoTracking().FirstAsync(m => m.TorrentId == torrentId && m.Path == "https://example.invalid/b");

        context.Downloads.Add(new Download
        {
            DownloadId = Guid.NewGuid(),
            TorrentId = downloadB.TorrentId,
            Path = downloadB.Path,
            Added = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }

    private DataContext CreateContext()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            ForeignKeys = true
        }.ToString();

        var options = new DbContextOptionsBuilder<DataContext>()
                      .UseSqlite(connectionString)
                      .Options;

        return new(options);
    }

    private async Task<Guid> SeedTorrentWithDownloadsAsync()
    {
        var torrentId = Guid.NewGuid();

        await using var context = CreateContext();

        context.Torrents.Add(new()
        {
            TorrentId = torrentId,
            Hash = Guid.NewGuid().ToString("N"),
            Added = DateTimeOffset.UtcNow
        });

        context.Downloads.Add(new()
        {
            DownloadId = Guid.NewGuid(),
            TorrentId = torrentId,
            FileName = "download-a.bin",
            Path = "https://example.invalid/a",
            Added = DateTimeOffset.UtcNow,
            Completed = DateTimeOffset.UtcNow
        });

        context.Downloads.Add(new()
        {
            DownloadId = Guid.NewGuid(),
            TorrentId = torrentId,
            FileName = "download-b.bin",
            Path = "https://example.invalid/b",
            Added = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();

        return torrentId;
    }
}

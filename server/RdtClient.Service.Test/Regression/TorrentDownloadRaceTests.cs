using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Test.Regression;

public class TorrentDownloadRaceTests : IAsyncLifetime
{
    private readonly String _databasePath = Path.Combine(Path.GetTempPath(), $"rdt-client-race-{Guid.NewGuid():N}.sqlite");

    [Fact]
    public async Task Add_WhenTorrentIsDeletedAfterItWasRead_DoesNotThrowAndDoesNotInsertDownload()
    {
        var torrentId = await SeedTorrentAsync();

        await using (var readerContext = CreateContext())
        {
            var torrentData = new TorrentData(readerContext);
            var torrent = await torrentData.GetById(torrentId);

            Assert.NotNull(torrent);
        }

        await using (var deleteContext = CreateContext())
        {
            var torrentData = new TorrentData(deleteContext);
            await torrentData.Delete(torrentId);
        }

        DownloadAddResult result;

        await using (var addContext = CreateContext())
        {
            var downloadData = new DownloadData(addContext);
            result = await downloadData.TryAddForTorrent(torrentId, CreateDownloadInfo("race-a"));
        }

        Assert.Equal(DownloadAddResult.TorrentMissing, result);

        await using var verifyContext = CreateContext();
        Assert.False(await verifyContext.Torrents.AnyAsync(m => m.TorrentId == torrentId));
        Assert.False(await verifyContext.Downloads.AnyAsync());
    }

    [Fact]
    public async Task Delete_WhenDownloadIsInsertedBetweenChildAndParentDelete_DoesNotThrowAndRemovesTorrentGraph()
    {
        var torrentId = await SeedTorrentAsync();

        await using (var deleteChildrenContext = CreateContext())
        {
            var downloadData = new DownloadData(deleteChildrenContext);
            await downloadData.DeleteForTorrent(torrentId);
        }

        await using (var addContext = CreateContext())
        {
            var downloadData = new DownloadData(addContext);
            var result = await downloadData.TryAddForTorrent(torrentId, CreateDownloadInfo("race-b"));
            Assert.Equal(DownloadAddResult.Added, result);
        }

        Exception? exception;

        await using (var deleteParentContext = CreateContext())
        {
            var torrentData = new TorrentData(deleteParentContext);
            exception = await Record.ExceptionAsync(() => torrentData.Delete(torrentId));
        }

        Assert.Null(exception);

        await using var verifyContext = CreateContext();
        Assert.False(await verifyContext.Torrents.AnyAsync(m => m.TorrentId == torrentId));
        Assert.False(await verifyContext.Downloads.AnyAsync(m => m.TorrentId == torrentId));
    }

    [Fact]
    public async Task TryAddForTorrent_WhenDownloadAlreadyExists_DoesNotInsertDuplicate()
    {
        var torrentId = await SeedTorrentAsync();

        await using (var firstContext = CreateContext())
        {
            var downloadData = new DownloadData(firstContext);
            var result = await downloadData.TryAddForTorrent(torrentId, CreateDownloadInfo("race-c"));

            Assert.Equal(DownloadAddResult.Added, result);
        }

        await using (var secondContext = CreateContext())
        {
            var downloadData = new DownloadData(secondContext);
            var result = await downloadData.TryAddForTorrent(torrentId, CreateDownloadInfo("race-c"));

            Assert.Equal(DownloadAddResult.AlreadyExists, result);
        }

        await using var verifyContext = CreateContext();
        Assert.Equal(1, await verifyContext.Downloads.CountAsync(m => m.TorrentId == torrentId));
    }

    [Fact]
    public async Task Delete_WhenTorrentWasAlreadyDeleted_DoesNotThrow()
    {
        var torrentId = await SeedTorrentAsync();

        await using (var firstDeleteContext = CreateContext())
        {
            var torrentData = new TorrentData(firstDeleteContext);
            await torrentData.Delete(torrentId);
        }

        Exception? exception;

        await using (var secondDeleteContext = CreateContext())
        {
            var torrentData = new TorrentData(secondDeleteContext);
            exception = await Record.ExceptionAsync(() => torrentData.Delete(torrentId));
        }

        Assert.Null(exception);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
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

    private async Task<Guid> SeedTorrentAsync()
    {
        var torrentId = Guid.NewGuid();

        await using var context = CreateContext();
        context.Torrents.Add(new Torrent
        {
            TorrentId = torrentId,
            Hash = Guid.NewGuid().ToString("N"),
            Added = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();

        return torrentId;
    }

    private static DownloadInfo CreateDownloadInfo(String suffix)
    {
        return new()
        {
            FileName = $"download-{suffix}.bin",
            RestrictedLink = $"https://example.invalid/{suffix}"
        };
    }
}

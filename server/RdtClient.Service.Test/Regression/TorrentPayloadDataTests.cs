using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Test.Regression;

public class TorrentPayloadDataTests : IAsyncLifetime
{
    private readonly String _databasePath = Path.Combine(Path.GetTempPath(), $"rdt-client-payload-data-{Guid.NewGuid():N}.sqlite");

    [Fact]
    public async Task Add_ShouldCreatePayloadRow_AndDetailReadShouldIncludePayload()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        var torrentData = new TorrentData(context);
        var template = new Torrent
        {
            DownloadClient = DownloadClient.Bezzad,
            HostDownloadAction = TorrentHostDownloadAction.DownloadAll,
            DownloadAction = TorrentDownloadAction.DownloadAll,
            FinishedAction = TorrentFinishedAction.None
        };

        var added = await torrentData.Add(null,
                                          "hash-1",
                                          "magnet:?xt=urn:btih:hash-1",
                                          false,
                                          DownloadType.Torrent,
                                          DownloadClient.Bezzad,
                                          template);

        await using var verifyContext = CreateContext();
        var payloadRow = await verifyContext.TorrentPayloads.SingleAsync(m => m.TorrentId == added.TorrentId);
        Assert.Equal("magnet:?xt=urn:btih:hash-1", payloadRow.Content);

        var detailTorrent = await new TorrentData(verifyContext).GetById(added.TorrentId);
        Assert.NotNull(detailTorrent);
        Assert.NotNull(detailTorrent!.Payload);
        Assert.Equal("magnet:?xt=urn:btih:hash-1", detailTorrent.Payload!.Content);
    }

    [Fact]
    public async Task Get_ShouldNotMaterializePayloadNavigation()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        context.Torrents.Add(new Torrent
        {
            TorrentId = Guid.NewGuid(),
            Hash = "hash-2",
            Added = DateTimeOffset.UtcNow,
            Type = DownloadType.Nzb,
            IsFile = true,
            Payload = new()
            {
                Content = Convert.ToBase64String(new Byte[1024])
            }
        });

        await context.SaveChangesAsync();

        await using var readContext = CreateContext();
        var results = await new TorrentData(readContext).Get();

        Assert.Single(results);
        Assert.Null(results[0].Payload);
    }

    [Fact]
    public async Task Delete_ShouldRemovePayloadRow()
    {
        var torrentId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
            context.Torrents.Add(new Torrent
            {
                TorrentId = torrentId,
                Hash = "hash-3",
                Added = DateTimeOffset.UtcNow,
                Type = DownloadType.Torrent,
                Payload = new()
                {
                    TorrentId = torrentId,
                    Content = "magnet:?xt=urn:btih:hash-3"
                }
            });
            await context.SaveChangesAsync();
        }

        await using (var deleteContext = CreateContext())
        {
            await new TorrentData(deleteContext).Delete(torrentId);
        }

        await using var verifyContext = CreateContext();
        Assert.False(await verifyContext.Torrents.AnyAsync(m => m.TorrentId == torrentId));
        Assert.False(await verifyContext.TorrentPayloads.AnyAsync(m => m.TorrentId == torrentId));
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
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
}

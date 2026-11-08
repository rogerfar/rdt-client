using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Test.Data;

public sealed class DdlDataTest : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DataContext _dataContext;

    public DdlDataTest()
    {
        _connection = new("DataSource=:memory:");
        _connection.Open();

        _dataContext = new(new DbContextOptionsBuilder<DataContext>().UseSqlite(_connection).Options);
        _dataContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        _connection.Dispose();
    }

    private async Task<Torrent> AddTorrent(DownloadType type)
    {
        var torrent = new Torrent
        {
            TorrentId = Guid.NewGuid(),
            Hash = Guid.NewGuid().ToString("N"),
            Type = type,
            TorrentRetryAttempts = 2
        };

        _dataContext.Torrents.Add(torrent);
        await _dataContext.SaveChangesAsync();

        return torrent;
    }

    private async Task<Torrent> Reload(Guid torrentId)
    {
        return await _dataContext.Torrents.AsNoTracking().FirstAsync(m => m.TorrentId == torrentId);
    }

    [Fact]
    public async Task UpdateComplete_DoesNotScheduleARetryForAFailedDirectDownload()
    {
        var torrent = await AddTorrent(DownloadType.Ddl);

        await new TorrentData(_dataContext).UpdateComplete(torrent.TorrentId, "part2 failed", DateTimeOffset.UtcNow, true);

        var saved = await Reload(torrent.TorrentId);
        Assert.Null(saved.Retry);
        Assert.Equal(0, saved.RetryCount);
        Assert.Equal("part2 failed", saved.Error);
    }

    [Fact]
    public async Task UpdateComplete_StillRetriesOtherDownloadTypes()
    {
        var torrent = await AddTorrent(DownloadType.Nzb);

        await new TorrentData(_dataContext).UpdateComplete(torrent.TorrentId, "failed", DateTimeOffset.UtcNow, true);

        var saved = await Reload(torrent.TorrentId);
        Assert.NotNull(saved.Retry);
        Assert.Equal(1, saved.RetryCount);
    }

    [Fact]
    public async Task SwitchToNextMirror_RequeuesOnEachMirrorThenGivesUp()
    {
        var torrent = await AddTorrent(DownloadType.Ddl);
        var downloadData = new DownloadData(_dataContext);

        Assert.Equal(DownloadAddResult.Added,
                     await downloadData.TryAddForTorrent(torrent.TorrentId,
                                                         new()
                                                         {
                                                             FileName = null,
                                                             RestrictedLink = "https://rapidgator.net/file/a/x.part2.rar.html",
                                                             Mirrors = "https://nitroflare.com/view/B/x.part2.rar\nhttps://ddownload.com/c/x.part2.rar"
                                                         }));

        var download = await _dataContext.Downloads.AsNoTracking().FirstAsync(m => m.TorrentId == torrent.TorrentId);

        await downloadData.UpdateFileName(download.DownloadId, "x.part2.rar");
        await downloadData.UpdateError(download.DownloadId, "LINK_HOST_NOT_SUPPORTED");
        await downloadData.UpdateLastError(download.DownloadId, "rapidgator: LINK_HOST_NOT_SUPPORTED");
        await downloadData.UpdateCompleted(download.DownloadId, DateTimeOffset.UtcNow);

        Assert.Equal("https://nitroflare.com/view/B/x.part2.rar", await downloadData.SwitchToNextMirror(download.DownloadId));

        var saved = await _dataContext.Downloads.AsNoTracking().FirstAsync(m => m.DownloadId == download.DownloadId);
        Assert.Equal("https://nitroflare.com/view/B/x.part2.rar", saved.Path);
        Assert.Equal("https://ddownload.com/c/x.part2.rar", saved.Mirrors);
        Assert.Null(saved.Error);
        // The requeued download keeps the reason it left the previous host, for the UI.
        Assert.Equal("rapidgator: LINK_HOST_NOT_SUPPORTED", saved.LastError);
        Assert.Null(saved.Completed);
        Assert.Null(saved.Link);
        Assert.Null(saved.FileName);

        Assert.Equal("https://ddownload.com/c/x.part2.rar", await downloadData.SwitchToNextMirror(download.DownloadId));
        Assert.Null(await downloadData.SwitchToNextMirror(download.DownloadId));
    }
}

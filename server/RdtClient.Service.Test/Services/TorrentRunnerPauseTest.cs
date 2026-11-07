using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;
using ActiveDownload = RdtClient.Service.Services.DownloadClient;
using DownloadClientType = RdtClient.Data.Enums.DownloadClient;

namespace RdtClient.Service.Test.Services;

public class TorrentRunnerPauseTest : IAsyncLifetime
{
    private readonly String _databasePath = Path.Combine(Path.GetTempPath(), $"rdt-client-pause-{Guid.NewGuid():N}.sqlite");
    private readonly String _downloadPath = Path.Combine(Path.GetTempPath(), $"rdt-client-pause-{Guid.NewGuid():N}");
    private readonly TorrentRunnerState _runnerState = new();
    private readonly TestSettings _settings = new();
    private readonly List<Torrent> _torrents = [];

    private DataContext? _context;

    public async Task InitializeAsync()
    {
        _settings.Current.Provider.ApiKey = "test-api-key";
        _settings.Current.Provider.Provider = Provider.RealDebrid;
        _settings.Current.DownloadClient.DownloadPath = _downloadPath;
        _settings.Current.General.DownloadLimit = 2;

        _context = CreateContext();
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var client in _runnerState.ActiveDownloadClients.Values)
        {
            await client.Cancel();
        }

        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        if (Directory.Exists(_downloadPath))
        {
            Directory.Delete(_downloadPath, true);
        }
    }

    [Fact]
    public async Task Tick_WhenPaused_StopsActiveBezzadDownloadAndPutsItBackInTheQueue()
    {
        _settings.Current.General.DownloadsPaused = true;

        var (torrent, download) = await SeedAsync(DownloadClientType.Bezzad, DateTimeOffset.UtcNow.AddMinutes(-1));
        var client = new ActiveDownload(download, torrent, _downloadPath, null);
        _runnerState.ActiveDownloadClients[download.DownloadId] = client;

        await CreateRunner().Tick();

        Assert.False(_runnerState.ActiveDownloadClients.ContainsKey(download.DownloadId));
        Assert.True(client.Finished);

        var stored = await ReadDownloadAsync(download.DownloadId);
        Assert.Null(stored.DownloadStarted);
        Assert.NotNull(stored.DownloadQueued);
        Assert.Null(stored.DownloadFinished);
        Assert.Null(stored.UnpackingQueued);
        Assert.Null(stored.Error);
    }

    [Fact]
    public async Task Tick_WhenPaused_LeavesOtherDownloadClientsRunning()
    {
        _settings.Current.General.DownloadsPaused = true;

        var (torrent, download) = await SeedAsync(DownloadClientType.Symlink, DateTimeOffset.UtcNow.AddMinutes(-1));
        var client = new ActiveDownload(download, torrent, _downloadPath, null);
        _runnerState.ActiveDownloadClients[download.DownloadId] = client;

        await CreateRunner().Tick();

        Assert.True(_runnerState.ActiveDownloadClients.ContainsKey(download.DownloadId));
        Assert.False(client.Finished);
        Assert.NotNull((await ReadDownloadAsync(download.DownloadId)).DownloadStarted);
    }

    [Fact]
    public async Task Tick_WhenPaused_DoesNotStartQueuedDownloads()
    {
        _settings.Current.General.DownloadsPaused = true;

        var (_, download) = await SeedAsync(DownloadClientType.Bezzad, null);

        await CreateRunner().Tick();

        Assert.Empty(_runnerState.ActiveDownloadClients);
        Assert.Null((await ReadDownloadAsync(download.DownloadId)).DownloadStarted);
    }

    [Fact]
    public async Task Tick_WhenNotPaused_StartsQueuedDownloads()
    {
        // Control for the test above: the same queued download is picked up when downloads are not paused.
        var (_, download) = await SeedAsync(DownloadClientType.Bezzad, null);

        await CreateRunner().Tick();

        Assert.True(_runnerState.ActiveDownloadClients.ContainsKey(download.DownloadId));
        Assert.NotNull((await ReadDownloadAsync(download.DownloadId)).DownloadStarted);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Initialize_KeepsDownloadsPausedOnlyWhenConfigured(Boolean keepPausedAfterRestart, Boolean expectedPaused)
    {
        _settings.Current.General.DownloadsPaused = true;
        _settings.Current.General.KeepDownloadsPausedAfterRestart = keepPausedAfterRestart;

        await CreateRunner().Initialize();

        Assert.Equal(expectedPaused, _settings.Current.General.DownloadsPaused);
    }

    private TorrentRunner CreateRunner()
    {
        var torrentDataMock = new Mock<ITorrentData>();
        torrentDataMock.Setup(m => m.Get()).ReturnsAsync(() => _torrents);

        var torrents = new Torrents(Mock.Of<ILogger<Torrents>>(),
                                    torrentDataMock.Object,
                                    Mock.Of<IDownloads>(),
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    null!,
                                    _settings,
                                    _runnerState);

        return new(Mock.Of<ILogger<TorrentRunner>>(),
                   torrents,
                   new(new(CreateContext())),
                   new(null!, torrents),
                   Mock.Of<IHttpClientFactory>(),
                   new RateLimitCoordinator(),
                   _settings,
                   _runnerState);
    }

    private async Task<(Torrent Torrent, Download Download)> SeedAsync(DownloadClientType client, DateTimeOffset? downloadStarted)
    {
        var torrentId = Guid.NewGuid();
        var downloadId = Guid.NewGuid();

        _context!.Torrents.Add(new()
        {
            TorrentId = torrentId,
            Hash = Guid.NewGuid().ToString("N"),
            Added = DateTimeOffset.UtcNow
        });

        _context.Downloads.Add(new()
        {
            DownloadId = downloadId,
            TorrentId = torrentId,
            Path = "file.bin",
            Link = "http://127.0.0.1:9/file.bin",
            FileName = "file.bin",
            Added = DateTimeOffset.UtcNow.AddMinutes(-5),
            DownloadQueued = DateTimeOffset.UtcNow.AddMinutes(-5),
            DownloadStarted = downloadStarted
        });

        await _context.SaveChangesAsync();

        // The runner reads torrents through the mocked ITorrentData, so hand it detached copies.
        var download = new Download
        {
            DownloadId = downloadId,
            TorrentId = torrentId,
            Path = "file.bin",
            Link = "http://127.0.0.1:9/file.bin",
            FileName = "file.bin",
            Added = DateTimeOffset.UtcNow.AddMinutes(-5),
            DownloadQueued = DateTimeOffset.UtcNow.AddMinutes(-5),
            DownloadStarted = downloadStarted
        };

        var torrent = new Torrent
        {
            TorrentId = torrentId,
            Hash = "hash",
            RdName = "Torrent",
            Type = DownloadType.Torrent,
            RdStatus = TorrentStatus.Finished,
            DownloadClient = client,
            DownloadRetryAttempts = 3,
            Downloads = new List<Download>
            {
                download
            }
        };

        download.Torrent = torrent;
        _torrents.Add(torrent);

        return (torrent, download);
    }

    private async Task<Download> ReadDownloadAsync(Guid downloadId)
    {
        await using var context = CreateContext();

        return await context.Downloads.AsNoTracking().SingleAsync(m => m.DownloadId == downloadId);
    }

    private DataContext CreateContext()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            ForeignKeys = true
        }.ToString();

        return new(new DbContextOptionsBuilder<DataContext>().UseSqlite(connectionString).Options);
    }
}

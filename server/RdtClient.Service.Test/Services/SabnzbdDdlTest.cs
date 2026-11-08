using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class SabnzbdDdlTest
{
    private readonly AppSettings _appSettings = new()
    {
        Port = 6500
    };

    private readonly Mock<ILogger<Sabnzbd>> _loggerMock = new();
    private static readonly TestSettings Settings = new();
    private readonly Mock<Torrents> _torrentsMock = new(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, Settings, new TorrentRunnerState());

    [Fact]
    public async Task GetQueue_ShowsWaitingDdlAsQueuedAtZeroPercent()
    {
        _torrentsMock.Setup(t => t.Get())
                     .ReturnsAsync([
                         new()
                         {
                             Hash = "ddl1",
                             RdName = "Movie.2026.1080p-GRP",
                             RdProgress = 100,
                             Type = DownloadType.Ddl,
                             Downloads = []
                         }
                     ]);

        var result = await new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings, Settings).GetQueue();

        var slot = Assert.Single(result.Slots);
        Assert.Equal("ddl1", slot.NzoId);
        Assert.Equal("Queued", slot.Status);
        Assert.Equal("0", slot.Percentage);
    }

    [Fact]
    public async Task GetQueue_ShowsDdlWithLinksAsDownloadingWithDownloadProgressOnly()
    {
        _torrentsMock.Setup(t => t.Get())
                     .ReturnsAsync([
                         new()
                         {
                             Hash = "ddl1",
                             RdName = "Movie.2026.1080p-GRP",
                             RdProgress = 100,
                             RdStatus = TorrentStatus.Finished,
                             Type = DownloadType.Ddl,
                             Downloads = [new()]
                         }
                     ]);
        _torrentsMock.Setup(t => t.GetDownloadStats(It.IsAny<Guid>())).Returns((0, 1000, 250));

        var result = await new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings, Settings).GetQueue();

        var slot = Assert.Single(result.Slots);
        Assert.Equal("Downloading", slot.Status);
        Assert.Equal("25", slot.Percentage);
    }

    [Fact]
    public async Task GetHistory_IncludesCompletedDdl()
    {
        _torrentsMock.Setup(t => t.Get())
                     .ReturnsAsync([
                         new()
                         {
                             Hash = "ddl1",
                             RdName = "Movie.2026.1080p-GRP",
                             Category = "movies",
                             Type = DownloadType.Ddl,
                             Completed = DateTimeOffset.UtcNow,
                             Downloads = []
                         },
                         new()
                         {
                             Hash = "t1",
                             RdName = "Torrent",
                             Type = DownloadType.Torrent,
                             Completed = DateTimeOffset.UtcNow,
                             Downloads = []
                         }
                     ]);

        var result = await new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings, Settings).GetHistory();

        var slot = Assert.Single(result.Slots);
        Assert.Equal("ddl1", slot.NzoId);
        Assert.Equal("Completed", slot.Status);
        Assert.EndsWith(Path.Combine("movies", "Movie.2026.1080p-GRP"), slot.Path);
    }

    [Fact]
    public async Task AddFile_RoutesStandInNzbToDirectDownloadQueue()
    {
        const String post = "https://ddl-site.example/movie-2026-1080p-web-grp-5-gb/";
        var nzb = Encoding.UTF8.GetBytes($"""
                                          <?xml version="1.0" encoding="UTF-8"?>
                                          <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb"><head>
                                          <meta type="name">Movie.2026.1080p.WEB-GRP</meta><meta type="x-ddl-post">{post}</meta>
                                          </head><file poster="b" date="1" subject="s"><groups><group>g</group></groups>
                                          <segments><segment bytes="1" number="1">x@ddl.invalid</segment></segments></file></nzb>
                                          """);

        _torrentsMock.Setup(t => t.AddDdlToDebridQueue(post, "Movie.2026.1080p.WEB-GRP", It.IsAny<Torrent>()))
                     .ReturnsAsync(new Torrent { Hash = "ddlhash" });

        var result = await new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings, Settings).AddFile(nzb, "Movie.nzb", "movies", null);

        Assert.Equal("ddlhash", result);
        _torrentsMock.Verify(t => t.AddNzbFileToDebridQueue(It.IsAny<Byte[]>(), It.IsAny<String?>(), It.IsAny<Torrent>()), Times.Never);
    }

    [Fact]
    public async Task AddFile_SendsRegularNzbToUsenetQueue()
    {
        var nzb = Encoding.UTF8.GetBytes("""
                                         <?xml version="1.0" encoding="UTF-8"?>
                                         <nzb xmlns="http://www.newzbin.com/DTD/2003/nzb"><head><meta type="name">Real</meta></head>
                                         <file poster="p" date="1" subject="s"><groups><group>g</group></groups><segments><segment bytes="1" number="1">a@b</segment></segments></file></nzb>
                                         """);

        _torrentsMock.Setup(t => t.AddNzbFileToDebridQueue(It.IsAny<Byte[]>(), It.IsAny<String?>(), It.IsAny<Torrent>()))
                     .ReturnsAsync(new Torrent { Hash = "nzbhash" });

        var result = await new Sabnzbd(_loggerMock.Object, _torrentsMock.Object, _appSettings, Settings).AddFile(nzb, "Real.nzb", "movies", null);

        Assert.Equal("nzbhash", result);
        _torrentsMock.Verify(t => t.AddDdlToDebridQueue(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<Torrent>()), Times.Never);
    }
}

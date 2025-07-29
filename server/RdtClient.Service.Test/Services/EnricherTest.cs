using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Service.Services;
using MonoTorrent.BEncoding;

namespace RdtClient.Service.Test.Services;

public class EnricherTest : IDisposable
{
    private readonly MockRepository _mockRepository;
    private readonly Mock<ILogger<Enricher>> _loggerMock;
    private readonly Mock<ITrackerListGrabber> _trackerListGrabberMock;

    public EnricherTest()
    {
        _mockRepository = new(MockBehavior.Strict);
        _loggerMock = _mockRepository.Create<ILogger<Enricher>>(MockBehavior.Loose);
        _trackerListGrabberMock = _mockRepository.Create<ITrackerListGrabber>();
    }

    public void Dispose()
    {
        _mockRepository.VerifyAll();
    }

    private const String TestMagnetLink =
        "magnet:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567&dn=TestFile&tr=http%3A%2F%2Ftracker1.com%2Fannounce&tr=http%3A%2F%2Ftracker2.com%2Fannounce";

    // Helper methods for creating BEncodedDictionary objects for torrents
    private static BEncodedDictionary CreateStandardTorrentDict(String announceUrl, List<String>? announceListTier = null)
    {
        var torrentDict = new BEncodedDictionary
        {
            ["announce"] = new BEncodedString(announceUrl)
        };

        var finalAnnounceList = new BEncodedList();

        if (announceListTier != null)
        {
            var tier = new BEncodedList();

            foreach (var tracker in announceListTier)
            {
                tier.Add(new BEncodedString(tracker));
            }

            finalAnnounceList.Add(tier);
        }
        else
        {
            finalAnnounceList.Add(new BEncodedList
            {
                new BEncodedString(announceUrl)
            });
        }

        torrentDict["announce-list"] = finalAnnounceList;

        if (!torrentDict.ContainsKey("info"))
        {
            torrentDict["info"] = new BEncodedDictionary();
        }

        return torrentDict;
    }

    private static BEncodedDictionary CreateTorrentDictWithOnlyAnnounce(String announceUrl)
    {
        return new()
        {
            ["announce"] = new BEncodedString(announceUrl),
            ["info"] = new BEncodedDictionary()
        };
    }

    private static BEncodedDictionary CreateTorrentDictWithNoTrackers()
    {
        return new()
        {
            ["info"] = new BEncodedDictionary()
        };
    }

    private static BEncodedDictionary CreateTorrentDictWithComplexAnnounceList(String primaryAnnounceUrl, List<List<String>> announceListTiers)
    {
        var torrentDict = new BEncodedDictionary
        {
            ["announce"] = new BEncodedString(primaryAnnounceUrl)
        };

        var bencodedAnnounceList = new BEncodedList();

        foreach (var tier in announceListTiers)
        {
            var bencodedTier = new BEncodedList();

            foreach (var tracker in tier)
            {
                bencodedTier.Add(new BEncodedString(tracker));
            }

            bencodedAnnounceList.Add(bencodedTier);
        }

        torrentDict["announce-list"] = bencodedAnnounceList;

        if (!torrentDict.ContainsKey("info"))
        {
            torrentDict["info"] = new BEncodedDictionary();
        }

        return torrentDict;
    }

    private void SetupTrackerListGrabber(String[] trackerList)
    {
        _trackerListGrabberMock
            .Setup(t => t.GetTrackers())
            .ReturnsAsync(trackerList)
            .Verifiable();
    }

    [Fact]
    public async Task EnrichMagnetLink_WhenNoTrackers_ReturnsOriginal()
    {
        // Arrange
        SetupTrackerListGrabber([]);

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enriched = await enricher.EnrichMagnetLink(TestMagnetLink);

        // Assert
        Assert.Equal(TestMagnetLink, enriched);
    }

    [Theory]
    [InlineData("magnet:?xt=urn:btih:HASH",
                   "magnet:?xt=urn:btih:HASH&tr=http%3A%2F%2Fnew-tracker.com%2Fannounce",
                   "http://new-tracker.com/announce")]
    [InlineData("magnet:?xt=urn:btih:HASH",
                   "magnet:?xt=urn:btih:HASH&tr=http%3A%2F%2Ftracker1.com%2Fannounce&tr=http%3A%2F%2Ftracker2.com%2Fannounce",
                   "http://tracker1.com/announce",
                   "http://tracker2.com/announce")]
    [InlineData("magnet:?xt=urn:btih:HASH", "magnet:?xt=urn:btih:HASH&tr=udp%3A%2F%2Ftracker1", "udp://tracker1")]
    [InlineData("magnet:?xt=urn:btih:HASH", "magnet:?xt=urn:btih:HASH&tr=udp%3A%2F%2Ftracker1&tr=udp%3A%2F%2Ftracker2", "udp://tracker1", "udp://tracker2")]
    [InlineData("magnet:?xt=urn:btih:HASH", "magnet:?xt=urn:btih:HASH")]
    [InlineData("magnet:?",
                   "magnet:?tr=udp%3A%2F%2Ftracker1",
                   "udp://tracker1")]
    [InlineData("magnet:?",
                   "magnet:?tr=udp%3A%2F%2Ftracker1&tr=udp%3A%2F%2Ftracker2",
                   "udp://tracker1",
                   "udp://tracker2")]
    [InlineData("magnet:?",
                   "magnet:?")]
    public async Task EnrichMagnetLink_WhenTrackersProvided_AddsTrackers(String magnetLink, String expectedResult, params String[] newTrackers)
    {
        // Arrange
        SetupTrackerListGrabber(newTrackers);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enriched = await enricher.EnrichMagnetLink(magnetLink);

        // Assert
        Assert.Equal(expectedResult, enriched);
    }

    [Fact]
    public async Task EnrichMagnetLink_WhenDuplicatesProvided_AddsUniqueTrackers()
    {
        // Arrange
        SetupTrackerListGrabber(["http://new-tracker.com/announce", "http://tracker1.com/announce"]);

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enriched = await enricher.EnrichMagnetLink(TestMagnetLink);

        // Assert
        Assert.Equal(TestMagnetLink + $"&tr={Uri.EscapeDataString("http://new-tracker.com/announce")}", enriched);
    }

    [Fact]
    public async Task EnrichMagnetLink_WhenGrabberFails_ThrowsException()
    {
        // Arrange
        _trackerListGrabberMock
            .Setup(t => t.GetTrackers())
            .ThrowsAsync(new InvalidOperationException("Unable to fetch tracker list for enrichment."));

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => enricher.EnrichMagnetLink(TestMagnetLink));
    }

    [Fact]
    public async Task EnrichMagnetLink_WhenTrackersAdded_PreservesParameters()
    {
        // Arrange
        var magnetLink = "magnet:?xt=urn:btih:HASH&dn=MyFile&tr=udp%3A%2F%2Fexisting";

        var newTrackers = new[]
        {
            "udp://new1", "udp://existing"
        };

        SetupTrackerListGrabber(newTrackers);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var result = await enricher.EnrichMagnetLink(magnetLink);

        // Assert
        var queryParams = System.Web.HttpUtility.ParseQueryString(new Uri(result).Query);
        Assert.Equal("urn:btih:HASH", queryParams["xt"]);
        Assert.Equal("MyFile", queryParams["dn"]);
    }

    [Fact]
    public async Task EnrichMagnetLink_WhenTrackersAdded_IncludesAllTrackers()
    {
        // Arrange
        var magnetLink = "magnet:?xt=urn:btih:HASH&dn=MyFile&tr=udp%3A%2F%2Fexisting";

        var newTrackers = new[]
        {
            "udp://new1", "udp://existing"
        };

        SetupTrackerListGrabber(newTrackers);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var result = await enricher.EnrichMagnetLink(magnetLink);

        // Assert
        var queryParams = System.Web.HttpUtility.ParseQueryString(new Uri(result).Query);
        var trValues = queryParams.GetValues("tr")?.ToList() ?? new List<String>();

        Assert.Contains("udp://existing", trValues);
        Assert.Contains("udp://new1", trValues);
        Assert.Equal(2, trValues.Count);
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenTrackersAdded_CreatesAnnounceKey()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";
        var torrentDict = CreateStandardTorrentDict(originalTracker);
        var originalTorrentBytes = torrentDict.Encode();
        SetupTrackerListGrabber([newTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce"));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenTrackersAdded_CreatesAnnounceListKey()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";
        var torrentDict = CreateStandardTorrentDict(originalTracker);
        var originalTorrentBytes = torrentDict.Encode();
        SetupTrackerListGrabber([newTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce-list"));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenTrackersAdded_IncludesAllTrackers()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";
        var torrentDict = CreateStandardTorrentDict(originalTracker);
        var originalTorrentBytes = torrentDict.Encode();
        SetupTrackerListGrabber([newTracker]); // Grabber returns newTracker
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();
        Assert.Contains(originalTracker, flattened);
        Assert.Contains(newTracker, flattened);
        Assert.Equal(2, flattened.Count);
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenTrackersAdded_SetsPrimaryAnnounce()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";
        var torrentDict = CreateStandardTorrentDict(originalTracker);
        var originalTorrentBytes = torrentDict.Encode();
        SetupTrackerListGrabber([newTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();
        var announce = ((BEncodedString)enrichedDict["announce"]).Text;
        Assert.Equal(flattened.First(), announce);
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenDuplicatesProvided_AddsUniqueTrackers()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var duplicateTracker = "http://tracker1.com/announce";

        var torrentDict = CreateStandardTorrentDict(originalTracker);
        var originalTorrentBytes = torrentDict.Encode();

        SetupTrackerListGrabber([duplicateTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();

        Assert.Single(flattened);
        Assert.Contains(originalTracker, flattened);
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNoAnnounceList_CreatesAnnounceListKey()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";
        var torrentDict = CreateTorrentDictWithOnlyAnnounce(originalTracker);
        var originalTorrentBytes = torrentDict.Encode();
        SetupTrackerListGrabber([newTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce-list"));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNoAnnounceList_AddsOriginalTracker()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";
        var torrentDict = CreateTorrentDictWithOnlyAnnounce(originalTracker);
        var originalTorrentBytes = torrentDict.Encode();
        SetupTrackerListGrabber([newTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();
        Assert.Contains(originalTracker, flattened);
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNoAnnounceList_AddsGrabbedTrackers()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";
        var torrentDict = CreateTorrentDictWithOnlyAnnounce(originalTracker);
        var originalTorrentBytes = torrentDict.Encode();
        SetupTrackerListGrabber([newTracker]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();
        Assert.Contains(newTracker, flattened);
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenGrabberFails_ThrowsException()
    {
        // Arrange
        _trackerListGrabberMock
            .Setup(t => t.GetTrackers())
            .ThrowsAsync(new InvalidOperationException("Unable to fetch tracker list for enrichment."));

        var torrentDict = CreateStandardTorrentDict("http://tracker1.com/announce");
        var originalTorrentBytes = torrentDict.Encode();

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => enricher.EnrichTorrentBytes(originalTorrentBytes));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNoNewTrackers_ReturnsOriginalBytes()
    {
        // Arrange
        var torrentDict = CreateStandardTorrentDict("http://existing.tracker/announce");
        var originalTorrentBytes = torrentDict.Encode();

        SetupTrackerListGrabber([]);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var resultBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);

        // Assert
        Assert.Equal(originalTorrentBytes, resultBytes);
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNoTrackers_ReturnsOriginalBytes()
    {
        // Arrange
        var torrentDict = CreateTorrentDictWithNoTrackers();
        var originalTorrentBytes = torrentDict.Encode();

        SetupTrackerListGrabber([]); // Grabber returns no new trackers
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var resultBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);

        // Assert
        Assert.Equal(originalTorrentBytes, resultBytes);

        var decodedResult = BEncodedValue.Decode<BEncodedDictionary>(resultBytes);
        Assert.False(decodedResult.ContainsKey("announce"));
        Assert.False(decodedResult.ContainsKey("announce-list"));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNullInput_ThrowsArgumentException()
    {
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);
        await Assert.ThrowsAsync<ArgumentException>(() => enricher.EnrichTorrentBytes(null!));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenEmptyInput_ThrowsArgumentException()
    {
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);
        await Assert.ThrowsAsync<ArgumentException>(() => enricher.EnrichTorrentBytes([]));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenInvalidInput_ThrowsInvalidOperationException()
    {
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        var notTorrent = new Byte[]
        {
            1, 2, 3, 4, 5
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => enricher.EnrichTorrentBytes(notTorrent));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNoAnnounceFields_CreatesAnnounceField()
    {
        // Arrange
        var torrentDict = CreateTorrentDictWithNoTrackers();
        var originalBytes = torrentDict.Encode();

        var newTrackers = new[]
        {
            "udp://tracker1", "udp://tracker2"
        };

        SetupTrackerListGrabber(newTrackers);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce"));
        Assert.Equal(newTrackers[0], ((BEncodedString)enrichedDict["announce"]).Text);
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNoAnnounceFields_CreatesAnnounceListKey()
    {
        // Arrange
        var torrentDict = CreateTorrentDictWithNoTrackers();
        var originalBytes = torrentDict.Encode();

        var newTrackers = new[]
        {
            "udp://tracker1", "udp://tracker2"
        };

        SetupTrackerListGrabber(newTrackers);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce-list"));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenNoAnnounceFields_AddsGrabbedTrackers()
    {
        // Arrange
        var torrentDict = CreateTorrentDictWithNoTrackers();
        var originalBytes = torrentDict.Encode();

        var newTrackers = new[]
        {
            "udp://tracker1", "udp://tracker2"
        };

        SetupTrackerListGrabber(newTrackers);
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        var announceList = (BEncodedList)enrichedDict["announce-list"];
        Assert.Equal(newTrackers.Length, announceList.Count);

        Assert.Collection(announceList,
                          tier => Assert.Equal(newTrackers[0], ((BEncodedString)((BEncodedList)tier)[0]).Text),
                          tier => Assert.Equal(newTrackers[1], ((BEncodedString)((BEncodedList)tier)[0]).Text));
    }

    [Fact]
    public async Task EnrichTorrentBytes_WhenExistingTrackers_MergesNewTrackers()
    {
        // Arrange
        var primaryAnnounceUrl = "http://existing.com/announce";

        var announceListTiers = new List<List<String>>
        {
            new()
            {
                "http://tier1.com"
            },
            new()
            {
                primaryAnnounceUrl
            }
        };

        var torrentDict = CreateTorrentDictWithComplexAnnounceList(primaryAnnounceUrl, announceListTiers);
        var originalBytes = torrentDict.Encode();

        var newTrackersFromGrabber = new[]
        {
            "udp://new1", "http://existing.com/announce"
        };

        SetupTrackerListGrabber(newTrackersFromGrabber);

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        var finalAnnounceList = ((BEncodedList)enrichedDict["announce-list"])
                                .Cast<BEncodedList>()
                                .SelectMany(tier => tier.Cast<BEncodedString>().Select(t => t.Text))
                                .ToList();

        Assert.Contains("http://tier1.com", finalAnnounceList);
        Assert.Contains("http://existing.com/announce", finalAnnounceList);
        Assert.Contains("udp://new1", finalAnnounceList);
        Assert.Equal(3, finalAnnounceList.Count);
        Assert.Contains(((BEncodedString)enrichedDict["announce"]).Text, finalAnnounceList);
    }
}

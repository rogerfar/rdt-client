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
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _loggerMock = _mockRepository.Create<ILogger<Enricher>>(MockBehavior.Loose);
        _trackerListGrabberMock = _mockRepository.Create<ITrackerListGrabber>();
    }

    public void Dispose()
    {
        _mockRepository.VerifyAll();
    }

    private const String TestMagnetLink = "magnet:?xt=urn:btih:1234567890123456789012345678901234567890&dn=TestFile&tr=http%3A%2F%2Ftracker1.com%2Fannounce&tr=http%3A%2F%2Ftracker2.com%2Fannounce";

    [Fact]
    public async Task EnrichMagnetLink_AddsNoTrackers_WhenNoTrackersFromTrackerGrabber()
    {
        // Arrange
        SetupTrackerListGrabber([]);

        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enriched = await enricher.EnrichMagnetLink(TestMagnetLink);

        // Assert
        Assert.Equal(TestMagnetLink, enriched);
    }

    [Fact]
    public async Task EnrichMagnetLink_AddsTrackers_WhenTrackersFromTrackerGrabber()
    {
        // Arrange
        SetupTrackerListGrabber(["http://new-tracker.com/announce"]);

        var Enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enriched = await Enricher.EnrichMagnetLink(TestMagnetLink);

        // Assert
        Assert.Equal(TestMagnetLink + $"&tr={Uri.EscapeDataString("http://new-tracker.com/announce")}", enriched);
    }

    [Fact]
    public async Task EnrichMagnetLink_DoesNotAddDuplicateTrackers_WhenTrackersFromTrackerGrabberAlreadyPresent()
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
    public async Task EnrichMagnetLink_Throws_WhenTrackerGrabberThrows()
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
    public async Task EnrichTorrentBytes_AddsTrackers_WhenTrackersFromTrackerGrabber()
    {
        // Arrange
        var originalTracker = "http://tracker1.com/announce";
        var newTracker = "http://new-tracker.com/announce";

        var torrentDict = new BEncodedDictionary
        {
            ["announce"] = new BEncodedString(originalTracker),
            ["announce-list"] = new BEncodedList
                {
                    new BEncodedList { new BEncodedString(originalTracker) }
                }
        };

        var originalTorrentBytes = torrentDict.Encode();

        SetupTrackerListGrabber(new[] { newTracker });
        var enricher = new Enricher(_loggerMock.Object, _trackerListGrabberMock.Object);

        // Act
        var enrichedBytes = await enricher.EnrichTorrentBytes(originalTorrentBytes);
        var enrichedDict = BEncodedValue.Decode<BEncodedDictionary>(enrichedBytes);

        // Assert
        Assert.True(enrichedDict.ContainsKey("announce"));
        Assert.True(enrichedDict.ContainsKey("announce-list"));

        var announceList = (BEncodedList)enrichedDict["announce-list"];
        var flattened = announceList.Cast<BEncodedList>().SelectMany(l => l.Cast<BEncodedString>().Select(s => s.Text)).ToList();

        Assert.Contains(originalTracker, flattened);
        Assert.Contains(newTracker, flattened);

        var announce = ((BEncodedString)enrichedDict["announce"]).Text;
        Assert.Equal(flattened.First(), announce);
    }

    private void SetupTrackerListGrabber(String[] trackerList)
    {
        _trackerListGrabberMock
            .Setup(t => t.GetTrackers())
            .ReturnsAsync(trackerList)
            .Verifiable();
    }
}
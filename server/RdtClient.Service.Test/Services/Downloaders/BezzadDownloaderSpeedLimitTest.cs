using RdtClient.Service.Services;
using RdtClient.Service.Services.Downloaders;

namespace RdtClient.Service.Test.Services.Downloaders;

// Touches the static Settings and the static active downloader list, so keep it out of parallel runs.
[CollectionDefinition(nameof(BezzadDownloaderSpeedLimitCollection), DisableParallelization = true)]
public class BezzadDownloaderSpeedLimitCollection;

[Collection(nameof(BezzadDownloaderSpeedLimitCollection))]
public class BezzadDownloaderSpeedLimitTest
{
    private const Int64 MegaByte = 1024 * 1024;

    [Theory]
    [InlineData(0, false, 1, 0)]
    [InlineData(0, true, 3, 0)]
    [InlineData(-5, true, 2, 0)]
    [InlineData(40, false, 1, 40 * MegaByte)]
    [InlineData(40, false, 4, 40 * MegaByte)]
    [InlineData(40, true, 0, 40 * MegaByte)]
    [InlineData(40, true, 1, 40 * MegaByte)]
    [InlineData(40, true, 2, 20 * MegaByte)]
    [InlineData(40, true, 4, 10 * MegaByte)]
    [InlineData(3000, false, 1, 3000 * MegaByte)]
    public void CalculateMaxBytesPerSecond_SplitsOnlyWhenShared(Int32 maxSpeed, Boolean shared, Int32 activeCount, Int64 expected)
    {
        Assert.Equal(expected, BezzadDownloader.CalculateMaxBytesPerSecond(maxSpeed, shared, activeCount));
    }

    [Fact]
    public void CalculateMaxBytesPerSecond_NeverRoundsAPositiveLimitDownToUnlimited()
    {
        // 1 MB/s split 2,000,000 ways is under 1 byte each; 0 would mean unlimited.
        Assert.Equal(1, BezzadDownloader.CalculateMaxBytesPerSecond(1, true, 2_000_000));
    }

    [Fact]
    public async Task SharedLimit_RebalancesAsDownloadsJoinAndLeave()
    {
        var settings = Settings.Get.DownloadClient;
        var (oldSpeed, oldShared) = (settings.MaxSpeed, settings.MaxSpeedShared);

        settings.MaxSpeed = 40;
        settings.MaxSpeedShared = true;

        var baseline = BezzadDownloader.ActiveCount;
        var first = NewDownloader();
        var second = NewDownloader();

        try
        {
            // Resume registers a download as active without starting a network transfer.
            await first.Resume();
            Assert.Equal(baseline + 1, BezzadDownloader.ActiveCount);
            Assert.Equal(40 * MegaByte, first.CurrentMaxBytesPerSecond);

            await second.Resume();
            Assert.Equal(baseline + 2, BezzadDownloader.ActiveCount);
            Assert.Equal(20 * MegaByte, first.CurrentMaxBytesPerSecond);
            Assert.Equal(20 * MegaByte, second.CurrentMaxBytesPerSecond);

            // A paused download stops counting and hands its share back.
            await second.Pause();
            Assert.Equal(baseline + 1, BezzadDownloader.ActiveCount);
            Assert.Equal(40 * MegaByte, first.CurrentMaxBytesPerSecond);

            await second.Resume();
            Assert.Equal(20 * MegaByte, first.CurrentMaxBytesPerSecond);

            // A cancelled download leaves for good, and Resume does not bring it back.
            await second.Cancel();
            await second.Resume();
            Assert.Equal(baseline + 1, BezzadDownloader.ActiveCount);
            Assert.Equal(40 * MegaByte, first.CurrentMaxBytesPerSecond);
        }
        finally
        {
            await first.Cancel();
            await second.Cancel();
            settings.MaxSpeed = oldSpeed;
            settings.MaxSpeedShared = oldShared;
        }

        Assert.Equal(baseline, BezzadDownloader.ActiveCount);
    }

    [Fact]
    public async Task PerDownloadLimit_IsUnchangedByOtherDownloads()
    {
        var settings = Settings.Get.DownloadClient;
        var (oldSpeed, oldShared) = (settings.MaxSpeed, settings.MaxSpeedShared);

        settings.MaxSpeed = 40;
        settings.MaxSpeedShared = false;

        var first = NewDownloader();
        var second = NewDownloader();

        try
        {
            await first.Resume();
            await second.Resume();

            Assert.Equal(40 * MegaByte, first.CurrentMaxBytesPerSecond);
            Assert.Equal(40 * MegaByte, second.CurrentMaxBytesPerSecond);
        }
        finally
        {
            await first.Cancel();
            await second.Cancel();
            settings.MaxSpeed = oldSpeed;
            settings.MaxSpeedShared = oldShared;
        }
    }

    [Fact]
    public void NewDownload_IsSizedAsTheNextOneToStart()
    {
        var settings = Settings.Get.DownloadClient;
        var (oldSpeed, oldShared) = (settings.MaxSpeed, settings.MaxSpeedShared);

        settings.MaxSpeed = 40;
        settings.MaxSpeedShared = true;

        try
        {
            // Nothing else active: a freshly created download would get the whole limit.
            var downloader = NewDownloader();
            Assert.Equal(40 * MegaByte / (BezzadDownloader.ActiveCount + 1), downloader.CurrentMaxBytesPerSecond);
        }
        finally
        {
            settings.MaxSpeed = oldSpeed;
            settings.MaxSpeedShared = oldShared;
        }
    }

    private static BezzadDownloader NewDownloader()
    {
        return new("http://127.0.0.1:9/never-fetched", Path.Combine(Path.GetTempPath(), $"rdt-speed-{Guid.NewGuid():N}.bin"));
    }
}

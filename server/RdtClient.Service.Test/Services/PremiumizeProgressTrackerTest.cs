using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class PremiumizeProgressTrackerTest
{
    [Fact]
    public void Update_WhenProgressIncreases_IsNotStalledAndHasSpeed()
    {
        var tracker = new PremiumizeProgressTracker();
        var torrentId = Guid.NewGuid();

        tracker.Update(torrentId, 0.1);
        Thread.Sleep(50);
        tracker.Update(torrentId, 0.2);

        Assert.False(tracker.IsStalled(torrentId));
        Assert.True(tracker.GetSpeedBytesPerSec(torrentId) > 0);
        Assert.True(tracker.GetProgressPerMin(torrentId) > 0);
    }

    [Fact]
    public void Update_WhenNoProgressForStallThreshold_IsStalled()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = new PremiumizeProgressTracker
        {
            UtcNowOverride = () => now
        };
        var torrentId = Guid.NewGuid();

        tracker.Update(torrentId, 0.5);
        now = now.Add(PremiumizeProgressTracker.StallThreshold);
        tracker.Update(torrentId, 0.5);

        Assert.True(tracker.IsStalled(torrentId));
    }

    [Fact]
    public void Update_WhenProgressIncreasesAfterStallPeriod_ResetsStallTimer()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = new PremiumizeProgressTracker
        {
            UtcNowOverride = () => now
        };
        var torrentId = Guid.NewGuid();

        tracker.Update(torrentId, 0.5);
        now = now.Add(PremiumizeProgressTracker.StallThreshold);
        tracker.Update(torrentId, 0.5);

        Assert.True(tracker.IsStalled(torrentId));

        now = now.Add(TimeSpan.FromSeconds(1));
        tracker.Update(torrentId, 0.6);

        Assert.False(tracker.IsStalled(torrentId));
        Assert.True(tracker.GetSpeedBytesPerSec(torrentId) > 0);
    }

    [Fact]
    public void Remove_ClearsTrackedState()
    {
        var tracker = new PremiumizeProgressTracker();
        var torrentId = Guid.NewGuid();

        tracker.Update(torrentId, 0.5);
        tracker.Remove(torrentId);

        Assert.False(tracker.IsStalled(torrentId));
        Assert.Null(tracker.GetSpeedBytesPerSec(torrentId));
        Assert.Null(tracker.GetProgressPerMin(torrentId));
    }
}

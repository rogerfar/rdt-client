using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class UncachedHashMemoryTest
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NeedsCheck_UnknownHash_IsTrue()
    {
        var memory = new UncachedHashMemory(TimeSpan.FromMinutes(30));

        Assert.True(memory.NeedsCheck("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0));
    }

    [Fact]
    public void NeedsCheck_RecordedUncached_IsFalseWithinInterval()
    {
        var memory = new UncachedHashMemory(TimeSpan.FromMinutes(30));

        memory.RecordUncached("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0);

        Assert.False(memory.NeedsCheck("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0.AddMinutes(29)));
    }

    [Fact]
    public void NeedsCheck_RecordedUncached_IsTrueAfterInterval()
    {
        // A hash can BECOME cached later - the memory must re-ask the provider
        // once the interval has passed instead of rejecting forever.
        var memory = new UncachedHashMemory(TimeSpan.FromMinutes(30));

        memory.RecordUncached("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0);

        Assert.True(memory.NeedsCheck("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0.AddMinutes(30)));
    }

    [Fact]
    public void RecordUncached_CountsSightings()
    {
        var memory = new UncachedHashMemory(TimeSpan.FromMinutes(30));

        Assert.Equal(1, memory.RecordUncached("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0));
        Assert.Equal(2, memory.RecordUncached("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0.AddMinutes(31)));
    }

    [Fact]
    public void RecordSighting_CountsButKeepsTheRecheckWindow()
    {
        // Re-offers inside the interval must not push the next provider check
        // further away - under a constant re-offer stream the hash would then
        // never be re-checked and a later cache hit would never be discovered.
        var memory = new UncachedHashMemory(TimeSpan.FromMinutes(30));

        memory.RecordUncached("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0);
        var timesSeen = memory.RecordSighting("d7a46713eaee18c746b3254b7d1492a50fd9d6ce");

        Assert.Equal(2, timesSeen);
        Assert.True(memory.NeedsCheck("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0.AddMinutes(30)));
    }

    [Fact]
    public void RecordCached_ForgetsTheHash()
    {
        var memory = new UncachedHashMemory(TimeSpan.FromMinutes(30));

        memory.RecordUncached("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0);
        memory.RecordCached("d7a46713eaee18c746b3254b7d1492a50fd9d6ce");

        Assert.True(memory.NeedsCheck("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0.AddMinutes(1)));
    }

    [Fact]
    public void Hashes_AreCaseInsensitive()
    {
        // Infohashes arrive in mixed case from magnets vs .torrent parsing.
        var memory = new UncachedHashMemory(TimeSpan.FromMinutes(30));

        memory.RecordUncached("D7A46713EAEE18C746B3254B7D1492A50FD9D6CE", T0);

        Assert.False(memory.NeedsCheck("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", T0.AddMinutes(1)));
        Assert.Equal(2, memory.RecordSighting("d7a46713eaee18c746b3254b7d1492a50fd9d6ce"));
    }

    [Fact]
    public void RecordSighting_UnknownHash_StartsAtOne()
    {
        var memory = new UncachedHashMemory(TimeSpan.FromMinutes(30));

        Assert.Equal(1, memory.RecordSighting("d7a46713eaee18c746b3254b7d1492a50fd9d6ce"));
    }
}

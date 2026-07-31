namespace RdtClient.Service.Services;

public interface IPremiumizeProgressTracker
{
    void Update(Guid torrentId, Double progressFraction);

    Boolean IsStalled(Guid torrentId);

    Int64? GetSpeedBytesPerSec(Guid torrentId);

    Double? GetProgressPerMin(Guid torrentId);

    void Remove(Guid torrentId);
}

namespace RdtClient.Data.Models.Internal;

public class DiskSpaceStatus
{
    public Boolean IsPaused { get; set; }
    public Int64 AvailableSpaceGB { get; set; }
    public Int32 ThresholdGB { get; set; }
    public DateTimeOffset LastCheckTime { get; set; }
}

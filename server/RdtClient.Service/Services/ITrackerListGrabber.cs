namespace RdtClient.Service.Services;

public interface ITrackerListGrabber
{
    Task<String[]> GetTrackers();
}
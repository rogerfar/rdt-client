namespace RdtClient.Data.Enums
{
    public enum TorrentStatus
    {
        RealDebrid = 0,
        WaitingForDownload,
        Downloading,
        Finished,

        Error = 99
    }
}

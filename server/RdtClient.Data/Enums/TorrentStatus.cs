namespace RdtClient.Data.Enums
{
    public enum TorrentStatus
    {
        RealDebrid = 0,
        WaitingForDownload = 1,
        DownloadQueued = 2,
        Downloading = 3,
        Finished = 4,

        Error = 99
    }
}

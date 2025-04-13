namespace RdtClient.Data.Enums;

public enum TorrentStatus
{
    Queued = 0,

    Processing = 1,
    WaitingForFileSelection = 2,
    Downloading = 3,
    Finished = 4,
    Uploading = 5,

    Error = 99
}
namespace RdtClient.Data.Enums;

public enum TorrentStatus
{
    Processing = 0,
    WaitingForFileSelection = 1,
    Downloading = 2,
    Finished = 3,
    Uploading = 4,

    Error = 99
}
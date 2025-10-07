using System.ComponentModel;

namespace RdtClient.Data.Enums;

public enum DownloadClient
{
    [Description("Bezzad Downloader")]
    Bezzad,

    [Description("Aria2c")]
    Aria2c,

    [Description("Symlink Downloader")]
    Symlink,

    [Description("Synology DownloadStation")]
    DownloadStation,
}
using System.ComponentModel;

namespace RdtClient.Data.Enums;

public enum TorrentHostDownloadAction
{
    [Description("Download all files to host")]
    DownloadAll = 0,

    [Description("Don't download any files to host")]
    DownloadNone = 1,
}
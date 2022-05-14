using System.ComponentModel;

namespace RdtClient.Data.Enums;

public enum DownloadClient
{
    [Description("Simple Downloader")]
    Simple,

    [Description("Multi-Part Downloader")]
    MultiPart,

    [Description("Aria2c")]
    Aria2c
}
using System.ComponentModel;

namespace RdtClient.Data.Enums;

public enum TorrentFinishedAction
{
    [Description("No Action")]
    None = 0,

    [Description("Remove Torrent From Client And Provider")]
    RemoveAllTorrents = 1,

    [Description("Remove Torrent From Provider")]
    RemoveRealDebrid = 2,
    
    [Description("Remove Torrent From Client")]
    RemoveClient = 3
}
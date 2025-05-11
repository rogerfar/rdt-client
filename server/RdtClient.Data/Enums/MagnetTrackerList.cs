using System.ComponentModel;

namespace RdtClient.Data.Enums;

public enum MagnetTrackerEnrichment
{
    [Description("None (do not modify magnet links)")]
    None,

    [Description("Best trackers")]
    trackers_best,

    [Description("All trackers")]
    trackers_all,

    [Description("All UDP trackers")]
    trackers_all_udp,

    [Description("All HTTP trackers")]
    trackers_all_http,

    [Description("All HTTPS trackers")]
    trackers_all_https,

    [Description("All WebSocket (WS) trackers")]
    trackers_all_ws,

    [Description("Best IP-only trackers")]
    trackers_best_ip,

    [Description("All I2P trackers")]
    trackers_all_i2p,

    [Description("All IP-only trackers")]
    trackers_all_ip
}

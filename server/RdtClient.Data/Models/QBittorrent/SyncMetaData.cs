using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class SyncMetaData
{
    [JsonPropertyName("categories")]
    public IDictionary<String, TorrentCategory>? Categories { get; set; }

    [JsonPropertyName("full_update")]
    public Boolean? FullUpdate { get; set; }

    [JsonPropertyName("rid")]
    public Int64? Rid { get; set; }

    [JsonPropertyName("server_state")]
    public SyncMetaDataServerState? ServerState { get; set; }

    [JsonPropertyName("tags")]
    public IList<Object>? Tags { get; set; }

    [JsonPropertyName("torrents")]
    public IDictionary<String, TorrentInfo>? Torrents { get; set; }

    [JsonPropertyName("trackers")]
    public IDictionary<String, List<String>>? Trackers { get; set; }
}

public class SyncMetaDataServerState
{
    [JsonPropertyName("alltime_dl")]
    public Int64? AlltimeDl { get; set; }

    [JsonPropertyName("alltime_ul")]
    public Int64? AlltimeUl { get; set; }

    [JsonPropertyName("average_time_queue")]
    public Int64? AverageTimeQueue { get; set; }

    [JsonPropertyName("connection_status")]
    public String? ConnectionStatus { get; set; }

    [JsonPropertyName("dht_nodes")]
    public Int64? DhtNodes { get; set; }

    [JsonPropertyName("dl_info_data")]
    public Int64? DlInfoData { get; set; }

    [JsonPropertyName("dl_info_speed")]
    public Int64? DlInfoSpeed { get; set; }

    [JsonPropertyName("dl_rate_limit")]
    public Int64? DlRateLimit { get; set; }

    [JsonPropertyName("free_space_on_disk")]
    public Int64? FreeSpaceOnDisk { get; set; }

    [JsonPropertyName("global_ratio")]
    public String? GlobalRatio { get; set; }

    [JsonPropertyName("queued_io_jobs")]
    public Int64? QueuedIoJobs { get; set; }

    [JsonPropertyName("queueing")]
    public Boolean? Queueing { get; set; }

    [JsonPropertyName("read_cache_hits")]
    public String? ReadCacheHits { get; set; }

    [JsonPropertyName("read_cache_overload")]
    public String? ReadCacheOverload { get; set; }

    [JsonPropertyName("refresh_interval")]
    public Int64? RefreshInterval { get; set; }

    [JsonPropertyName("total_buffers_size")]
    public Int64? TotalBuffersSize { get; set; }

    [JsonPropertyName("total_peer_connections")]
    public Int64? TotalPeerConnections { get; set; }

    [JsonPropertyName("total_queued_size")]
    public Int64? TotalQueuedSize { get; set; }

    [JsonPropertyName("total_wasted_session")]
    public Int64? TotalWastedSession { get; set; }

    [JsonPropertyName("up_info_data")]
    public Int64? UpInfoData { get; set; }

    [JsonPropertyName("up_info_speed")]
    public Int64? UpInfoSpeed { get; set; }

    [JsonPropertyName("up_rate_limit")]
    public Int64? UpRateLimit { get; set; }

    [JsonPropertyName("use_alt_speed_limits")]
    public Boolean? UseAltSpeedLimits { get; set; }

    [JsonPropertyName("write_cache_overload")]
    public String? WriteCacheOverload { get; set; }
}
using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class AppPreferences
{
    [JsonPropertyName("add_trackers")]
    public String? AddTrackers { get; set; }

    [JsonPropertyName("add_trackers_enabled")]
    public Boolean? AddTrackersEnabled { get; set; }

    [JsonPropertyName("alt_dl_limit")]
    public Int64? AltDlLimit { get; set; }

    [JsonPropertyName("alt_up_limit")]
    public Int64? AltUpLimit { get; set; }

    [JsonPropertyName("alternative_webui_enabled")]
    public Boolean? AlternativeWebuiEnabled { get; set; }

    [JsonPropertyName("alternative_webui_path")]
    public String? AlternativeWebuiPath { get; set; }

    [JsonPropertyName("announce_ip")]
    public String? AnnounceIp { get; set; }

    [JsonPropertyName("announce_to_all_tiers")]
    public Boolean? AnnounceToAllTiers { get; set; }

    [JsonPropertyName("announce_to_all_trackers")]
    public Boolean? AnnounceToAllTrackers { get; set; }

    [JsonPropertyName("anonymous_mode")]
    public Boolean? AnonymousMode { get; set; }

    [JsonPropertyName("async_io_threads")]
    public Int64? AsyncIoThreads { get; set; }

    [JsonPropertyName("auto_delete_mode")]
    public Int64? AutoDeleteMode { get; set; }

    [JsonPropertyName("auto_tmm_enabled")]
    public Boolean? AutoTmmEnabled { get; set; }

    [JsonPropertyName("autorun_enabled")]
    public Boolean? AutorunEnabled { get; set; }

    [JsonPropertyName("autorun_program")]
    public String? AutorunProgram { get; set; }

    [JsonPropertyName("banned_IPs")]
    public String? BannedIPs { get; set; }

    [JsonPropertyName("bittorrent_protocol")]
    public Int64? BittorrentProtocol { get; set; }

    [JsonPropertyName("bypass_auth_subnet_whitelist")]
    public String? BypassAuthSubnetWhitelist { get; set; }

    [JsonPropertyName("bypass_auth_subnet_whitelist_enabled")]
    public Boolean? BypassAuthSubnetWhitelistEnabled { get; set; }

    [JsonPropertyName("bypass_local_auth")]
    public Boolean? BypassLocalAuth { get; set; }

    [JsonPropertyName("category_changed_tmm_enabled")]
    public Boolean? CategoryChangedTmmEnabled { get; set; }

    [JsonPropertyName("checking_memory_use")]
    public Int64? CheckingMemoryUse { get; set; }

    [JsonPropertyName("create_subfolder_enabled")]
    public Boolean? CreateSubfolderEnabled { get; set; }

    [JsonPropertyName("current_interface_address")]
    public String? CurrentInterfaceAddress { get; set; }

    [JsonPropertyName("current_network_interface")]
    public String? CurrentNetworkInterface { get; set; }

    [JsonPropertyName("dht")]
    public Boolean? Dht { get; set; }

    [JsonPropertyName("disk_cache")]
    public Int64? DiskCache { get; set; }

    [JsonPropertyName("disk_cache_ttl")]
    public Int64? DiskCacheTtl { get; set; }

    [JsonPropertyName("dl_limit")]
    public Int64? DlLimit { get; set; }

    [JsonPropertyName("dont_count_slow_torrents")]
    public Boolean? DontCountSlowTorrents { get; set; }

    [JsonPropertyName("dyndns_domain")]
    public String? DyndnsDomain { get; set; }

    [JsonPropertyName("dyndns_enabled")]
    public Boolean? DyndnsEnabled { get; set; }

    [JsonPropertyName("dyndns_password")]
    public String? DyndnsPassword { get; set; }

    [JsonPropertyName("dyndns_service")]
    public Int64? DyndnsService { get; set; }

    [JsonPropertyName("dyndns_username")]
    public String? DyndnsUsername { get; set; }

    [JsonPropertyName("embedded_tracker_port")]
    public Int64? EmbeddedTrackerPort { get; set; }

    [JsonPropertyName("enable_coalesce_read_write")]
    public Boolean? EnableCoalesceReadWrite { get; set; }

    [JsonPropertyName("enable_embedded_tracker")]
    public Boolean? EnableEmbeddedTracker { get; set; }

    [JsonPropertyName("enable_multi_connections_from_same_ip")]
    public Boolean? EnableMultiConnectionsFromSameIp { get; set; }

    [JsonPropertyName("enable_os_cache")]
    public Boolean? EnableOsCache { get; set; }

    [JsonPropertyName("enable_piece_extent_affinity")]
    public Boolean? EnablePieceExtentAffinity { get; set; }

    [JsonPropertyName("enable_super_seeding")]
    public Boolean? EnableSuperSeeding { get; set; }

    [JsonPropertyName("enable_upload_suggestions")]
    public Boolean? EnableUploadSuggestions { get; set; }

    [JsonPropertyName("encryption")]
    public Int64? Encryption { get; set; }

    [JsonPropertyName("export_dir")]
    public String? ExportDir { get; set; }

    [JsonPropertyName("export_dir_fin")]
    public String? ExportDirFin { get; set; }

    [JsonPropertyName("file_pool_size")]
    public Int64? FilePoolSize { get; set; }

    [JsonPropertyName("incomplete_files_ext")]
    public Boolean? IncompleteFilesExt { get; set; }

    [JsonPropertyName("ip_filter_enabled")]
    public Boolean? IpFilterEnabled { get; set; }

    [JsonPropertyName("ip_filter_path")]
    public String? IpFilterPath { get; set; }

    [JsonPropertyName("ip_filter_trackers")]
    public Boolean? IpFilterTrackers { get; set; }

    [JsonPropertyName("limit_lan_peers")]
    public Boolean? LimitLanPeers { get; set; }

    [JsonPropertyName("limit_tcp_overhead")]
    public Boolean? LimitTcpOverhead { get; set; }

    [JsonPropertyName("limit_utp_rate")]
    public Boolean? LimitUtpRate { get; set; }

    [JsonPropertyName("listen_port")]
    public Int64? ListenPort { get; set; }

    [JsonPropertyName("locale")]
    public String? Locale { get; set; }

    [JsonPropertyName("lsd")]
    public Boolean? Lsd { get; set; }

    [JsonPropertyName("mail_notification_auth_enabled")]
    public Boolean? MailNotificationAuthEnabled { get; set; }

    [JsonPropertyName("mail_notification_email")]
    public String? MailNotificationEmail { get; set; }

    [JsonPropertyName("mail_notification_enabled")]
    public Boolean? MailNotificationEnabled { get; set; }

    [JsonPropertyName("mail_notification_password")]
    public String? MailNotificationPassword { get; set; }

    [JsonPropertyName("mail_notification_sender")]
    public String? MailNotificationSender { get; set; }

    [JsonPropertyName("mail_notification_smtp")]
    public String? MailNotificationSmtp { get; set; }

    [JsonPropertyName("mail_notification_ssl_enabled")]
    public Boolean? MailNotificationSslEnabled { get; set; }

    [JsonPropertyName("mail_notification_username")]
    public String? MailNotificationUsername { get; set; }

    [JsonPropertyName("max_active_downloads")]
    public Int64? MaxActiveDownloads { get; set; }

    [JsonPropertyName("max_active_torrents")]
    public Int64? MaxActiveTorrents { get; set; }

    [JsonPropertyName("max_active_uploads")]
    public Int64? MaxActiveUploads { get; set; }

    [JsonPropertyName("max_connec")]
    public Int64? MaxConnec { get; set; }

    [JsonPropertyName("max_connec_per_torrent")]
    public Int64? MaxConnecPerTorrent { get; set; }

    [JsonPropertyName("max_ratio")]
    public Int64? MaxRatio { get; set; }

    [JsonPropertyName("max_ratio_act")]
    public Int64? MaxRatioAct { get; set; }

    [JsonPropertyName("max_ratio_enabled")]
    public Boolean? MaxRatioEnabled { get; set; }

    [JsonPropertyName("max_seeding_time")]
    public Int64? MaxSeedingTime { get; set; }

    [JsonPropertyName("max_seeding_time_enabled")]
    public Boolean? MaxSeedingTimeEnabled { get; set; }

    [JsonPropertyName("max_uploads")]
    public Int64? MaxUploads { get; set; }

    [JsonPropertyName("max_uploads_per_torrent")]
    public Int64? MaxUploadsPerTorrent { get; set; }

    [JsonPropertyName("outgoing_ports_max")]
    public Int64? OutgoingPortsMax { get; set; }

    [JsonPropertyName("outgoing_ports_min")]
    public Int64? OutgoingPortsMin { get; set; }

    [JsonPropertyName("pex")]
    public Boolean? Pex { get; set; }

    [JsonPropertyName("preallocate_all")]
    public Boolean? PreallocateAll { get; set; }

    [JsonPropertyName("proxy_auth_enabled")]
    public Boolean? ProxyAuthEnabled { get; set; }

    [JsonPropertyName("proxy_ip")]
    public String? ProxyIp { get; set; }

    [JsonPropertyName("proxy_password")]
    public String? ProxyPassword { get; set; }

    [JsonPropertyName("proxy_peer_connections")]
    public Boolean? ProxyPeerConnections { get; set; }

    [JsonPropertyName("proxy_port")]
    public Int64? ProxyPort { get; set; }

    [JsonPropertyName("proxy_torrents_only")]
    public Boolean? ProxyTorrentsOnly { get; set; }

    [JsonPropertyName("proxy_type")]
    public Int64? ProxyType { get; set; }

    [JsonPropertyName("proxy_username")]
    public String? ProxyUsername { get; set; }

    [JsonPropertyName("queueing_enabled")]
    public Boolean? QueueingEnabled { get; set; }

    [JsonPropertyName("random_port")]
    public Boolean? RandomPort { get; set; }

    [JsonPropertyName("recheck_completed_torrents")]
    public Boolean? RecheckCompletedTorrents { get; set; }

    [JsonPropertyName("resolve_peer_countries")]
    public Boolean? ResolvePeerCountries { get; set; }

    [JsonPropertyName("rss_auto_downloading_enabled")]
    public Boolean? RssAutoDownloadingEnabled { get; set; }

    [JsonPropertyName("rss_max_articles_per_feed")]
    public Int64? RssMaxArticlesPerFeed { get; set; }

    [JsonPropertyName("rss_processing_enabled")]
    public Boolean? RssProcessingEnabled { get; set; }

    [JsonPropertyName("rss_refresh_interval")]
    public Int64? RssRefreshInterval { get; set; }

    [JsonPropertyName("save_path")]
    public String? SavePath { get; set; }

    [JsonPropertyName("save_path_changed_tmm_enabled")]
    public Boolean? SavePathChangedTmmEnabled { get; set; }

    [JsonPropertyName("save_resume_data_interval")]
    public Int64? SaveResumeDataInterval { get; set; }

    [JsonPropertyName("scan_dirs")]
    public ScanDirs? ScanDirs { get; set; }

    [JsonPropertyName("schedule_from_hour")]
    public Int64? ScheduleFromHour { get; set; }

    [JsonPropertyName("schedule_from_min")]
    public Int64? ScheduleFromMin { get; set; }

    [JsonPropertyName("schedule_to_hour")]
    public Int64? ScheduleToHour { get; set; }

    [JsonPropertyName("schedule_to_min")]
    public Int64? ScheduleToMin { get; set; }

    [JsonPropertyName("scheduler_days")]
    public Int64? SchedulerDays { get; set; }

    [JsonPropertyName("scheduler_enabled")]
    public Boolean? SchedulerEnabled { get; set; }

    [JsonPropertyName("send_buffer_low_watermark")]
    public Int64? SendBufferLowWatermark { get; set; }

    [JsonPropertyName("send_buffer_watermark")]
    public Int64? SendBufferWatermark { get; set; }

    [JsonPropertyName("send_buffer_watermark_factor")]
    public Int64? SendBufferWatermarkFactor { get; set; }

    [JsonPropertyName("slow_torrent_dl_rate_threshold")]
    public Int64? SlowTorrentDlRateThreshold { get; set; }

    [JsonPropertyName("slow_torrent_inactive_timer")]
    public Int64? SlowTorrentInactiveTimer { get; set; }

    [JsonPropertyName("slow_torrent_ul_rate_threshold")]
    public Int64? SlowTorrentUlRateThreshold { get; set; }

    [JsonPropertyName("socket_backlog_size")]
    public Int64? SocketBacklogSize { get; set; }

    [JsonPropertyName("start_paused_enabled")]
    public Boolean? StartPausedEnabled { get; set; }

    [JsonPropertyName("stop_tracker_timeout")]
    public Int64? StopTrackerTimeout { get; set; }

    [JsonPropertyName("temp_path")]
    public String? TempPath { get; set; }

    [JsonPropertyName("temp_path_enabled")]
    public Boolean? TempPathEnabled { get; set; }

    [JsonPropertyName("torrent_changed_tmm_enabled")]
    public Boolean? TorrentChangedTmmEnabled { get; set; }

    [JsonPropertyName("up_limit")]
    public Int64? UpLimit { get; set; }

    [JsonPropertyName("upload_choking_algorithm")]
    public Int64? UploadChokingAlgorithm { get; set; }

    [JsonPropertyName("upload_slots_behavior")]
    public Int64? UploadSlotsBehavior { get; set; }

    [JsonPropertyName("upnp")]
    public Boolean? Upnp { get; set; }

    [JsonPropertyName("upnp_lease_duration")]
    public Int64? UpnpLeaseDuration { get; set; }

    [JsonPropertyName("use_https")]
    public Boolean? UseHttps { get; set; }

    [JsonPropertyName("utp_tcp_mixed_mode")]
    public Int64? UtpTcpMixedMode { get; set; }

    [JsonPropertyName("web_ui_address")]
    public String? WebUiAddress { get; set; }

    [JsonPropertyName("web_ui_ban_duration")]
    public Int64? WebUiBanDuration { get; set; }

    [JsonPropertyName("web_ui_clickjacking_protection_enabled")]
    public Boolean? WebUiClickjackingProtectionEnabled { get; set; }

    [JsonPropertyName("web_ui_csrf_protection_enabled")]
    public Boolean? WebUiCsrfProtectionEnabled { get; set; }

    [JsonPropertyName("web_ui_domain_list")]
    public String? WebUiDomainList { get; set; }

    [JsonPropertyName("web_ui_host_header_validation_enabled")]
    public Boolean? WebUiHostHeaderValidationEnabled { get; set; }

    [JsonPropertyName("web_ui_https_cert_path")]
    public String? WebUiHttpsCertPath { get; set; }

    [JsonPropertyName("web_ui_https_key_path")]
    public String? WebUiHttpsKeyPath { get; set; }

    [JsonPropertyName("web_ui_max_auth_fail_count")]
    public Int64? WebUiMaxAuthFailCount { get; set; }

    [JsonPropertyName("web_ui_port")]
    public Int64? WebUiPort { get; set; }

    [JsonPropertyName("web_ui_secure_cookie_enabled")]
    public Boolean? WebUiSecureCookieEnabled { get; set; }

    [JsonPropertyName("web_ui_session_timeout")]
    public Int64? WebUiSessionTimeout { get; set; }

    [JsonPropertyName("web_ui_upnp")]
    public Boolean? WebUiUpnp { get; set; }

    [JsonPropertyName("web_ui_username")]
    public String? WebUiUsername { get; set; }
}

public class ScanDirs
{
}
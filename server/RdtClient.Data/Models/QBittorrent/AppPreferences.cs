using System;
using Newtonsoft.Json;

namespace RdtClient.Data.Models.QBittorrent
{
    namespace QuickType
    {
        public class AppPreferences
        {
            [JsonProperty("add_trackers")]
            public String AddTrackers { get; set; }

            [JsonProperty("add_trackers_enabled")]
            public Boolean AddTrackersEnabled { get; set; }

            [JsonProperty("alt_dl_limit")]
            public Int64 AltDlLimit { get; set; }

            [JsonProperty("alt_up_limit")]
            public Int64 AltUpLimit { get; set; }

            [JsonProperty("alternative_webui_enabled")]
            public Boolean AlternativeWebuiEnabled { get; set; }

            [JsonProperty("alternative_webui_path")]
            public String AlternativeWebuiPath { get; set; }

            [JsonProperty("announce_ip")]
            public String AnnounceIp { get; set; }

            [JsonProperty("announce_to_all_tiers")]
            public Boolean AnnounceToAllTiers { get; set; }

            [JsonProperty("announce_to_all_trackers")]
            public Boolean AnnounceToAllTrackers { get; set; }

            [JsonProperty("anonymous_mode")]
            public Boolean AnonymousMode { get; set; }

            [JsonProperty("async_io_threads")]
            public Int64 AsyncIoThreads { get; set; }

            [JsonProperty("auto_delete_mode")]
            public Int64 AutoDeleteMode { get; set; }

            [JsonProperty("auto_tmm_enabled")]
            public Boolean AutoTmmEnabled { get; set; }

            [JsonProperty("autorun_enabled")]
            public Boolean AutorunEnabled { get; set; }

            [JsonProperty("autorun_program")]
            public String AutorunProgram { get; set; }

            [JsonProperty("banned_IPs")]
            public String BannedIPs { get; set; }

            [JsonProperty("bittorrent_protocol")]
            public Int64 BittorrentProtocol { get; set; }

            [JsonProperty("bypass_auth_subnet_whitelist")]
            public String BypassAuthSubnetWhitelist { get; set; }

            [JsonProperty("bypass_auth_subnet_whitelist_enabled")]
            public Boolean BypassAuthSubnetWhitelistEnabled { get; set; }

            [JsonProperty("bypass_local_auth")]
            public Boolean BypassLocalAuth { get; set; }

            [JsonProperty("category_changed_tmm_enabled")]
            public Boolean CategoryChangedTmmEnabled { get; set; }

            [JsonProperty("checking_memory_use")]
            public Int64 CheckingMemoryUse { get; set; }

            [JsonProperty("create_subfolder_enabled")]
            public Boolean CreateSubfolderEnabled { get; set; }

            [JsonProperty("current_interface_address")]
            public String CurrentInterfaceAddress { get; set; }

            [JsonProperty("current_network_interface")]
            public String CurrentNetworkInterface { get; set; }

            [JsonProperty("dht")]
            public Boolean Dht { get; set; }

            [JsonProperty("disk_cache")]
            public Int64 DiskCache { get; set; }

            [JsonProperty("disk_cache_ttl")]
            public Int64 DiskCacheTtl { get; set; }

            [JsonProperty("dl_limit")]
            public Int64 DlLimit { get; set; }

            [JsonProperty("dont_count_slow_torrents")]
            public Boolean DontCountSlowTorrents { get; set; }

            [JsonProperty("dyndns_domain")]
            public String DyndnsDomain { get; set; }

            [JsonProperty("dyndns_enabled")]
            public Boolean DyndnsEnabled { get; set; }

            [JsonProperty("dyndns_password")]
            public String DyndnsPassword { get; set; }

            [JsonProperty("dyndns_service")]
            public Int64 DyndnsService { get; set; }

            [JsonProperty("dyndns_username")]
            public String DyndnsUsername { get; set; }

            [JsonProperty("embedded_tracker_port")]
            public Int64 EmbeddedTrackerPort { get; set; }

            [JsonProperty("enable_coalesce_read_write")]
            public Boolean EnableCoalesceReadWrite { get; set; }

            [JsonProperty("enable_embedded_tracker")]
            public Boolean EnableEmbeddedTracker { get; set; }

            [JsonProperty("enable_multi_connections_from_same_ip")]
            public Boolean EnableMultiConnectionsFromSameIp { get; set; }

            [JsonProperty("enable_os_cache")]
            public Boolean EnableOsCache { get; set; }

            [JsonProperty("enable_piece_extent_affinity")]
            public Boolean EnablePieceExtentAffinity { get; set; }

            [JsonProperty("enable_super_seeding")]
            public Boolean EnableSuperSeeding { get; set; }

            [JsonProperty("enable_upload_suggestions")]
            public Boolean EnableUploadSuggestions { get; set; }

            [JsonProperty("encryption")]
            public Int64 Encryption { get; set; }

            [JsonProperty("export_dir")]
            public String ExportDir { get; set; }

            [JsonProperty("export_dir_fin")]
            public String ExportDirFin { get; set; }

            [JsonProperty("file_pool_size")]
            public Int64 FilePoolSize { get; set; }

            [JsonProperty("incomplete_files_ext")]
            public Boolean IncompleteFilesExt { get; set; }

            [JsonProperty("ip_filter_enabled")]
            public Boolean IpFilterEnabled { get; set; }

            [JsonProperty("ip_filter_path")]
            public String IpFilterPath { get; set; }

            [JsonProperty("ip_filter_trackers")]
            public Boolean IpFilterTrackers { get; set; }

            [JsonProperty("limit_lan_peers")]
            public Boolean LimitLanPeers { get; set; }

            [JsonProperty("limit_tcp_overhead")]
            public Boolean LimitTcpOverhead { get; set; }

            [JsonProperty("limit_utp_rate")]
            public Boolean LimitUtpRate { get; set; }

            [JsonProperty("listen_port")]
            public Int64 ListenPort { get; set; }

            [JsonProperty("locale")]
            public String Locale { get; set; }

            [JsonProperty("lsd")]
            public Boolean Lsd { get; set; }

            [JsonProperty("mail_notification_auth_enabled")]
            public Boolean MailNotificationAuthEnabled { get; set; }

            [JsonProperty("mail_notification_email")]
            public String MailNotificationEmail { get; set; }

            [JsonProperty("mail_notification_enabled")]
            public Boolean MailNotificationEnabled { get; set; }

            [JsonProperty("mail_notification_password")]
            public String MailNotificationPassword { get; set; }

            [JsonProperty("mail_notification_sender")]
            public String MailNotificationSender { get; set; }

            [JsonProperty("mail_notification_smtp")]
            public String MailNotificationSmtp { get; set; }

            [JsonProperty("mail_notification_ssl_enabled")]
            public Boolean MailNotificationSslEnabled { get; set; }

            [JsonProperty("mail_notification_username")]
            public String MailNotificationUsername { get; set; }

            [JsonProperty("max_active_downloads")]
            public Int64 MaxActiveDownloads { get; set; }

            [JsonProperty("max_active_torrents")]
            public Int64 MaxActiveTorrents { get; set; }

            [JsonProperty("max_active_uploads")]
            public Int64 MaxActiveUploads { get; set; }

            [JsonProperty("max_connec")]
            public Int64 MaxConnec { get; set; }

            [JsonProperty("max_connec_per_torrent")]
            public Int64 MaxConnecPerTorrent { get; set; }

            [JsonProperty("max_ratio")]
            public Int64 MaxRatio { get; set; }

            [JsonProperty("max_ratio_act")]
            public Int64 MaxRatioAct { get; set; }

            [JsonProperty("max_ratio_enabled")]
            public Boolean MaxRatioEnabled { get; set; }

            [JsonProperty("max_seeding_time")]
            public Int64 MaxSeedingTime { get; set; }

            [JsonProperty("max_seeding_time_enabled")]
            public Boolean MaxSeedingTimeEnabled { get; set; }

            [JsonProperty("max_uploads")]
            public Int64 MaxUploads { get; set; }

            [JsonProperty("max_uploads_per_torrent")]
            public Int64 MaxUploadsPerTorrent { get; set; }

            [JsonProperty("outgoing_ports_max")]
            public Int64 OutgoingPortsMax { get; set; }

            [JsonProperty("outgoing_ports_min")]
            public Int64 OutgoingPortsMin { get; set; }

            [JsonProperty("pex")]
            public Boolean Pex { get; set; }

            [JsonProperty("preallocate_all")]
            public Boolean PreallocateAll { get; set; }

            [JsonProperty("proxy_auth_enabled")]
            public Boolean ProxyAuthEnabled { get; set; }

            [JsonProperty("proxy_ip")]
            public String ProxyIp { get; set; }

            [JsonProperty("proxy_password")]
            public String ProxyPassword { get; set; }

            [JsonProperty("proxy_peer_connections")]
            public Boolean ProxyPeerConnections { get; set; }

            [JsonProperty("proxy_port")]
            public Int64 ProxyPort { get; set; }

            [JsonProperty("proxy_torrents_only")]
            public Boolean ProxyTorrentsOnly { get; set; }

            [JsonProperty("proxy_type")]
            public Int64 ProxyType { get; set; }

            [JsonProperty("proxy_username")]
            public String ProxyUsername { get; set; }

            [JsonProperty("queueing_enabled")]
            public Boolean QueueingEnabled { get; set; }

            [JsonProperty("random_port")]
            public Boolean RandomPort { get; set; }

            [JsonProperty("recheck_completed_torrents")]
            public Boolean RecheckCompletedTorrents { get; set; }

            [JsonProperty("resolve_peer_countries")]
            public Boolean ResolvePeerCountries { get; set; }

            [JsonProperty("rss_auto_downloading_enabled")]
            public Boolean RssAutoDownloadingEnabled { get; set; }

            [JsonProperty("rss_max_articles_per_feed")]
            public Int64 RssMaxArticlesPerFeed { get; set; }

            [JsonProperty("rss_processing_enabled")]
            public Boolean RssProcessingEnabled { get; set; }

            [JsonProperty("rss_refresh_interval")]
            public Int64 RssRefreshInterval { get; set; }

            [JsonProperty("save_path")]
            public String SavePath { get; set; }

            [JsonProperty("save_path_changed_tmm_enabled")]
            public Boolean SavePathChangedTmmEnabled { get; set; }

            [JsonProperty("save_resume_data_interval")]
            public Int64 SaveResumeDataInterval { get; set; }

            [JsonProperty("scan_dirs")]
            public ScanDirs ScanDirs { get; set; }

            [JsonProperty("schedule_from_hour")]
            public Int64 ScheduleFromHour { get; set; }

            [JsonProperty("schedule_from_min")]
            public Int64 ScheduleFromMin { get; set; }

            [JsonProperty("schedule_to_hour")]
            public Int64 ScheduleToHour { get; set; }

            [JsonProperty("schedule_to_min")]
            public Int64 ScheduleToMin { get; set; }

            [JsonProperty("scheduler_days")]
            public Int64 SchedulerDays { get; set; }

            [JsonProperty("scheduler_enabled")]
            public Boolean SchedulerEnabled { get; set; }

            [JsonProperty("send_buffer_low_watermark")]
            public Int64 SendBufferLowWatermark { get; set; }

            [JsonProperty("send_buffer_watermark")]
            public Int64 SendBufferWatermark { get; set; }

            [JsonProperty("send_buffer_watermark_factor")]
            public Int64 SendBufferWatermarkFactor { get; set; }

            [JsonProperty("slow_torrent_dl_rate_threshold")]
            public Int64 SlowTorrentDlRateThreshold { get; set; }

            [JsonProperty("slow_torrent_inactive_timer")]
            public Int64 SlowTorrentInactiveTimer { get; set; }

            [JsonProperty("slow_torrent_ul_rate_threshold")]
            public Int64 SlowTorrentUlRateThreshold { get; set; }

            [JsonProperty("socket_backlog_size")]
            public Int64 SocketBacklogSize { get; set; }

            [JsonProperty("start_paused_enabled")]
            public Boolean StartPausedEnabled { get; set; }

            [JsonProperty("stop_tracker_timeout")]
            public Int64 StopTrackerTimeout { get; set; }

            [JsonProperty("temp_path")]
            public String TempPath { get; set; }

            [JsonProperty("temp_path_enabled")]
            public Boolean TempPathEnabled { get; set; }

            [JsonProperty("torrent_changed_tmm_enabled")]
            public Boolean TorrentChangedTmmEnabled { get; set; }

            [JsonProperty("up_limit")]
            public Int64 UpLimit { get; set; }

            [JsonProperty("upload_choking_algorithm")]
            public Int64 UploadChokingAlgorithm { get; set; }

            [JsonProperty("upload_slots_behavior")]
            public Int64 UploadSlotsBehavior { get; set; }

            [JsonProperty("upnp")]
            public Boolean Upnp { get; set; }

            [JsonProperty("upnp_lease_duration")]
            public Int64 UpnpLeaseDuration { get; set; }

            [JsonProperty("use_https")]
            public Boolean UseHttps { get; set; }

            [JsonProperty("utp_tcp_mixed_mode")]
            public Int64 UtpTcpMixedMode { get; set; }

            [JsonProperty("web_ui_address")]
            public String WebUiAddress { get; set; }

            [JsonProperty("web_ui_ban_duration")]
            public Int64 WebUiBanDuration { get; set; }

            [JsonProperty("web_ui_clickjacking_protection_enabled")]
            public Boolean WebUiClickjackingProtectionEnabled { get; set; }

            [JsonProperty("web_ui_csrf_protection_enabled")]
            public Boolean WebUiCsrfProtectionEnabled { get; set; }

            [JsonProperty("web_ui_domain_list")]
            public String WebUiDomainList { get; set; }

            [JsonProperty("web_ui_host_header_validation_enabled")]
            public Boolean WebUiHostHeaderValidationEnabled { get; set; }

            [JsonProperty("web_ui_https_cert_path")]
            public String WebUiHttpsCertPath { get; set; }

            [JsonProperty("web_ui_https_key_path")]
            public String WebUiHttpsKeyPath { get; set; }

            [JsonProperty("web_ui_max_auth_fail_count")]
            public Int64 WebUiMaxAuthFailCount { get; set; }

            [JsonProperty("web_ui_port")]
            public Int64 WebUiPort { get; set; }

            [JsonProperty("web_ui_secure_cookie_enabled")]
            public Boolean WebUiSecureCookieEnabled { get; set; }

            [JsonProperty("web_ui_session_timeout")]
            public Int64 WebUiSessionTimeout { get; set; }

            [JsonProperty("web_ui_upnp")]
            public Boolean WebUiUpnp { get; set; }

            [JsonProperty("web_ui_username")]
            public String WebUiUsername { get; set; }
        }

        public class ScanDirs
        {
        }
    }
}
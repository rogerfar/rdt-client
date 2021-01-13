using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RdtClient.Service.Models.QBittorrent;
using RdtClient.Service.Models.QBittorrent.QuickType;

namespace RdtClient.Service.Services
{
    public interface IQBittorrent
    {
        Task<Boolean> AuthLogin(String userName, String password);
        Task AuthLogout();
        Task<AppPreferences> AppPreferences();
        Task<String> AppDefaultSavePath();
        Task<IList<TorrentInfo>> TorrentInfo();
        Task<IList<TorrentFileItem>> TorrentFileContents(String hash);
        Task<TorrentProperties> TorrentProperties(String hash);
        Task TorrentsDelete(String hash, Boolean deleteFiles);
        Task TorrentsAdd(String magnetLink, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete);
        Task TorrentsAddFile(Byte[] fileBytes, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete);
        Task TorrentsSetCategory(String hash, String category);
        Task<IDictionary<String, TorrentCategory>> TorrentsCategories();
    }

    public class QBittorrent : IQBittorrent
    {
        private readonly IAuthentication _authentication;
        private readonly ISettings _settings;
        private readonly ITorrents _torrents;

        public QBittorrent(ISettings settings, IAuthentication authentication, ITorrents torrents)
        {
            _settings = settings;
            _authentication = authentication;
            _torrents = torrents;
        }

        public async Task<Boolean> AuthLogin(String userName, String password)
        {
            var login = await _authentication.Login(userName, password);

            return login.Succeeded;
        }

        public async Task AuthLogout()
        {
            await _authentication.Logout();
        }

        public async Task<AppPreferences> AppPreferences()
        {
            var preferences = new AppPreferences
            {
                AddTrackers = "",
                AddTrackersEnabled = false,
                AltDlLimit = 10240,
                AltUpLimit = 10240,
                AlternativeWebuiEnabled = false,
                AlternativeWebuiPath = "",
                AnnounceIp = "",
                AnnounceToAllTiers = true,
                AnnounceToAllTrackers = false,
                AnonymousMode = false,
                AsyncIoThreads = 4,
                AutoDeleteMode = 0,
                AutoTmmEnabled = false,
                AutorunEnabled = false,
                AutorunProgram = "",
                BannedIPs = "",
                BittorrentProtocol = 0,
                BypassAuthSubnetWhitelist = "",
                BypassAuthSubnetWhitelistEnabled = false,
                BypassLocalAuth = false,
                CategoryChangedTmmEnabled = false,
                CheckingMemoryUse = 32,
                CreateSubfolderEnabled = true,
                CurrentInterfaceAddress = "",
                CurrentNetworkInterface = "",
                Dht = true,
                DiskCache = -1,
                DiskCacheTtl = 60,
                DlLimit = 0,
                DontCountSlowTorrents = false,
                DyndnsDomain = "changeme.dyndns.org",
                DyndnsEnabled = false,
                DyndnsPassword = "",
                DyndnsService = 0,
                DyndnsUsername = "",
                EmbeddedTrackerPort = 9000,
                EnableCoalesceReadWrite = true,
                EnableEmbeddedTracker = false,
                EnableMultiConnectionsFromSameIp = false,
                EnableOsCache = true,
                EnablePieceExtentAffinity = false,
                EnableSuperSeeding = false,
                EnableUploadSuggestions = false,
                Encryption = 0,
                ExportDir = "",
                ExportDirFin = "",
                FilePoolSize = 40,
                IncompleteFilesExt = false,
                IpFilterEnabled = false,
                IpFilterPath = "",
                IpFilterTrackers = false,
                LimitLanPeers = true,
                LimitTcpOverhead = false,
                LimitUtpRate = true,
                ListenPort = 31193,
                Locale = "en",
                Lsd = true,
                MailNotificationAuthEnabled = false,
                MailNotificationEmail = "",
                MailNotificationEnabled = false,
                MailNotificationPassword = "",
                MailNotificationSender = "qBittorrentNotification@example.com",
                MailNotificationSmtp = "smtp.changeme.com",
                MailNotificationSslEnabled = false,
                MailNotificationUsername = "",
                MaxActiveDownloads = 3,
                MaxActiveTorrents = 5,
                MaxActiveUploads = 3,
                MaxConnec = 500,
                MaxConnecPerTorrent = 100,
                MaxRatio = -1,
                MaxRatioAct = 0,
                MaxRatioEnabled = false,
                MaxSeedingTime = -1,
                MaxSeedingTimeEnabled = false,
                MaxUploads = -1,
                MaxUploadsPerTorrent = -1,
                OutgoingPortsMax = 0,
                OutgoingPortsMin = 0,
                Pex = true,
                PreallocateAll = false,
                ProxyAuthEnabled = false,
                ProxyIp = "0.0.0.0",
                ProxyPassword = "",
                ProxyPeerConnections = false,
                ProxyPort = 8080,
                ProxyTorrentsOnly = false,
                ProxyType = 0,
                ProxyUsername = "",
                QueueingEnabled = false,
                RandomPort = false,
                RecheckCompletedTorrents = false,
                ResolvePeerCountries = true,
                RssAutoDownloadingEnabled = false,
                RssMaxArticlesPerFeed = 50,
                RssProcessingEnabled = false,
                RssRefreshInterval = 30,
                SavePath = "",
                SavePathChangedTmmEnabled = false,
                SaveResumeDataInterval = 60,
                ScanDirs = new ScanDirs(),
                ScheduleFromHour = 8,
                ScheduleFromMin = 0,
                ScheduleToHour = 20,
                ScheduleToMin = 0,
                SchedulerDays = 0,
                SchedulerEnabled = false,
                SendBufferLowWatermark = 10,
                SendBufferWatermark = 500,
                SendBufferWatermarkFactor = 50,
                SlowTorrentDlRateThreshold = 2,
                SlowTorrentInactiveTimer = 60,
                SlowTorrentUlRateThreshold = 2,
                SocketBacklogSize = 30,
                StartPausedEnabled = false,
                StopTrackerTimeout = 1,
                TempPath = "",
                TempPathEnabled = false,
                TorrentChangedTmmEnabled = true,
                UpLimit = 0,
                UploadChokingAlgorithm = 1,
                UploadSlotsBehavior = 0,
                Upnp = true,
                UpnpLeaseDuration = 0,
                UseHttps = false,
                UtpTcpMixedMode = 0,
                WebUiAddress = "*",
                WebUiBanDuration = 3600,
                WebUiClickjackingProtectionEnabled = true,
                WebUiCsrfProtectionEnabled = true,
                WebUiDomainList = "*",
                WebUiHostHeaderValidationEnabled = true,
                WebUiHttpsCertPath = "",
                WebUiHttpsKeyPath = "",
                WebUiMaxAuthFailCount = 5,
                WebUiPort = 8080,
                WebUiSecureCookieEnabled = true,
                WebUiSessionTimeout = 3600,
                WebUiUpnp = false,
                WebUiUsername = ""
            };

            var savePath = await AppDefaultSavePath();

            preferences.SavePath = savePath;
            preferences.TempPath = $"{savePath}temp{Path.DirectorySeparatorChar}";

            var user = await _authentication.GetUser();

            if (user != null)
            {
                preferences.WebUiUsername = user.UserName;
            }

            return preferences;
        }

        public async Task<String> AppDefaultSavePath()
        {
            var downloadPath = await _settings.GetString("MappedPath");
            downloadPath = downloadPath.TrimEnd('\\')
                                       .TrimEnd('/');
            
            downloadPath += Path.DirectorySeparatorChar;
            
            return downloadPath;
        }

        public async Task<IList<TorrentInfo>> TorrentInfo()
        {
            var savePath = await AppDefaultSavePath();
            
            var results = new List<TorrentInfo>();

            var torrents = await _torrents.Get();

            var prio = 0;
            foreach (var torrent in torrents)
            {
                var downloadPath = savePath;
                
                if (!String.IsNullOrWhiteSpace(torrent.Category))
                {
                    downloadPath = Path.Combine(downloadPath, torrent.Category);
                }

                var torrentPath = Path.Combine(downloadPath, torrent.RdName);

                var bytesDone = torrent.RdProgress;
                var bytesTotal = torrent.RdSize;
                var speed = torrent.RdSpeed ?? 0;

                if (torrent.Downloads.Count > 0)
                {
                    bytesDone = torrent.Downloads.Sum(m => m.BytesDone);
                    bytesTotal = torrent.Downloads.Sum(m => m.BytesTotal);
                    speed = (Int32) torrent.Downloads.Average(m => m.Speed);
                }

                var result = new TorrentInfo
                {
                    AddedOn = torrent.Added.ToUnixTimeSeconds(),
                    AmountLeft = (Int64) (bytesDone * (100.0 - bytesTotal) / 100.0),
                    AutoTmm = false,
                    Availability = 2,
                    Category = torrent.Category ?? "",
                    Completed = (Int64) (bytesTotal * (bytesDone / 100.0)),
                    CompletionOn = torrent.Completed?.ToUnixTimeSeconds(),
                    DlLimit = -1,
                    Dlspeed = speed,
                    Downloaded = (Int64) (bytesTotal * (bytesDone / 100.0)),
                    DownloadedSession = (Int64) (bytesTotal * (bytesDone / 100.0)),
                    Eta = 0,
                    FlPiecePrio = false,
                    ForceStart = false,
                    Hash = torrent.Hash,
                    LastActivity = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    MagnetUri = "",
                    MaxRatio = -1,
                    MaxSeedingTime = -1,
                    Name = torrent.RdName,
                    NumComplete = 10,
                    NumIncomplete = 0,
                    NumLeechs = 100,
                    NumSeeds = 100,
                    Priority = ++prio,
                    Progress = (Int64) (bytesDone / 100.0),
                    Ratio = 1,
                    RatioLimit = 1,
                    ContentPath = torrentPath,
                    SavePath = downloadPath,
                    SeedingTimeLimit = 1,
                    SeenComplete = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    SeqDl = false,
                    State = "downloading",
                    Size = bytesTotal,
                    SuperSeeding = false,
                    Tags = "",
                    TimeActive = (Int64) (torrent.Added - DateTimeOffset.UtcNow).TotalMinutes,
                    TotalSize = bytesTotal,
                    Tracker = "udp://tracker.opentrackr.org:1337",
                    UpLimit = -1,
                    Uploaded = (Int64) (bytesTotal * (bytesDone / 100.0)),
                    UploadedSession = (Int64) (bytesTotal * (bytesDone / 100.0)),
                    Upspeed = speed
                };

                if (torrent.Completed.HasValue)
                {
                    // Indicates completed torrent and not seeding anymore.
                    result.State = "pausedUP";

                    if (torrent.Downloads.Count > 0)
                    {
                        var error = torrent.Downloads.FirstOrDefault(m => m.Error != null);

                        if (error != null)
                        {
                            result.State = "error";
                        }
                    }
                }
                
                results.Add(result);
            }

            return results;
        }
        public async Task<IList<TorrentFileItem>> TorrentFileContents(String hash)
        {
            var results = new List<TorrentFileItem>();

            var torrent = await _torrents.GetByHash(hash);

            if (torrent == null)
            {
                return null;
            }

            foreach (var file in torrent.Files)
            {
                var result = new TorrentFileItem
                {
                    Name = file.Path
                };

                results.Add(result);
            }

            return results;
        }

        public async Task<TorrentProperties> TorrentProperties(String hash)
        {
            var savePath = await AppDefaultSavePath();
            
            var torrent = await _torrents.GetByHash(hash);

            if (torrent == null)
            {
                return null;
            }
            
            if (!String.IsNullOrWhiteSpace(torrent.Category))
            {
                savePath = Path.Combine(savePath, torrent.Category);
            }
            
            var bytesDone = torrent.RdProgress;
            var bytesTotal = torrent.RdSize;
            var speed = torrent.RdSpeed ?? 0;

            if (torrent.Downloads.Count > 0)
            {
                bytesDone = torrent.Downloads.Sum(m => m.BytesDone);
                bytesTotal = torrent.Downloads.Sum(m => m.BytesTotal);
                speed = (Int32) torrent.Downloads.Average(m => m.Speed);
            }

            var result = new TorrentProperties
            {
                AdditionDate = torrent.Added.ToUnixTimeSeconds(),
                Comment = "RealDebridClient <https://github.com/rogerfar/rdt-client>",
                CompletionDate = torrent.Completed?.ToUnixTimeSeconds() ?? -1,
                CreatedBy = "RealDebridClient <https://github.com/rogerfar/rdt-client>",
                CreationDate = torrent.Added.ToUnixTimeSeconds(),
                DlLimit = -1,
                DlSpeed = speed,
                DlSpeedAvg = speed,
                Eta = 0,
                LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                NbConnections = 0,
                NbConnectionsLimit = 100,
                Peers = 0,
                PeersTotal = 2,
                PieceSize = torrent.Files.Count > 0 ? bytesTotal / torrent.Files.Count : 0,
                PiecesHave = torrent.Downloads.Count(m => m.Completed.HasValue),
                PiecesNum = torrent.Files.Count,
                Reannounce = 0,
                SavePath = savePath,
                SeedingTime = 1,
                Seeds = 100,
                SeedsTotal = 100,
                ShareRatio = 9999,
                TimeElapsed = (Int64) (torrent.Added - DateTimeOffset.UtcNow).TotalMinutes,
                TotalDownloaded = (Int64) (bytesTotal * (bytesDone / 100.0)),
                TotalDownloadedSession = (Int64) (bytesTotal * (bytesDone / 100.0)),
                TotalSize = bytesTotal,
                TotalUploaded = (Int64) (bytesTotal * (bytesDone / 100.0)),
                TotalUploadedSession = (Int64) (bytesTotal * (bytesDone / 100.0)),
                TotalWasted = 0,
                UpLimit = -1,
                UpSpeed = speed,
                UpSpeedAvg = speed
            };

            return result;
        }

        public async Task TorrentsDelete(String hash, Boolean deleteFiles)
        {
            var torrent = await _torrents.GetByHash(hash);

            if (torrent == null)
            {
                return;
            }

            await _torrents.Delete(torrent.TorrentId, true, true, deleteFiles);
        }

        public async Task TorrentsAdd(String magnetLink, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete)
        {
            await _torrents.UploadMagnet(magnetLink, autoDownload, autoUnpack, autoDelete);
        }

        public async Task TorrentsAddFile(Byte[] fileBytes, Boolean autoDownload, Boolean autoUnpack, Boolean autoDelete)
        {
            await _torrents.UploadFile(fileBytes, autoDownload, autoUnpack, autoDelete);
        }

        public async Task TorrentsSetCategory(String hash, String category)
        {
            await _torrents.UpdateCategory(hash, category);
        }

        public async Task<IDictionary<String, TorrentCategory>> TorrentsCategories()
        {
            var torrents = await _torrents.Get();

            var savePath = await AppDefaultSavePath();

            var torrentsToGroup = torrents.Where(m => !String.IsNullOrWhiteSpace(m.Category))
                                          .ToList();

            var results = new Dictionary<String, TorrentCategory>();

            if (torrentsToGroup.Count > 0)
            {
                results = torrentsToGroup.GroupBy(m => m.Category)
                                         .First()
                                         .ToDictionary(m => m.Category,
                                                       m => new TorrentCategory
                                                       {
                                                           Name = m.Category,
                                                           SavePath = ""
                                                       });
            }

            return results;
        }
    }
}
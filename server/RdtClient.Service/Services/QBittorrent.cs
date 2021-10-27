using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RdtClient.Data.Enums;
using RdtClient.Service.Models.QBittorrent;
using RdtClient.Service.Models.QBittorrent.QuickType;

namespace RdtClient.Service.Services
{
    public class QBittorrent
    {
        private readonly Authentication _authentication;
        private readonly Settings _settings;
        private readonly Torrents _torrents;
        private readonly Downloads _downloads;

        public QBittorrent(Settings settings, Authentication authentication, Torrents torrents, Downloads downloads)
        {
            _settings = settings;
            _authentication = authentication;
            _torrents = torrents;
            _downloads = downloads;
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

            var savePath = AppDefaultSavePath();

            preferences.SavePath = savePath;
            preferences.TempPath = $"{savePath}temp{Path.DirectorySeparatorChar}";

            var user = await _authentication.GetUser();

            if (user != null)
            {
                preferences.WebUiUsername = user.UserName;
            }

            return preferences;
        }

        public String AppDefaultSavePath()
        {
            var downloadPath = Settings.Get.MappedPath;

            downloadPath = downloadPath.TrimEnd('\\')
                                       .TrimEnd('/');

            downloadPath += Path.DirectorySeparatorChar;

            return downloadPath;
        }

        public async Task<IList<TorrentInfo>> TorrentInfo()
        {
            var savePath = AppDefaultSavePath();

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

                var torrentPath = downloadPath;
                if (!String.IsNullOrWhiteSpace(torrent.RdName))
                {
                    torrentPath = Path.Combine(downloadPath, torrent.RdName) + Path.DirectorySeparatorChar;
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

                var result = new TorrentInfo
                {
                    AddedOn = torrent.Added.ToUnixTimeSeconds(),
                    AmountLeft = bytesTotal - bytesDone,
                    AutoTmm = false,
                    Availability = 2,
                    Category = torrent.Category ?? "",
                    Completed = bytesDone,
                    CompletionOn = torrent.Completed?.ToUnixTimeSeconds(),
                    ContentPath = torrentPath,
                    DlLimit = -1,
                    Dlspeed = speed,
                    Downloaded = bytesDone,
                    DownloadedSession = bytesDone,
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
                    Progress = bytesDone / (Single) bytesTotal,
                    Ratio = 1,
                    RatioLimit = 1,
                    SavePath = downloadPath,
                    SeedingTimeLimit = 1,
                    SeenComplete = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    SeqDl = false,
                    Size = bytesTotal,
                    State = "downloading",
                    SuperSeeding = false,
                    Tags = "",
                    TimeActive = (Int64) (DateTimeOffset.UtcNow - torrent.Added).TotalSeconds,
                    TotalSize = bytesTotal,
                    Tracker = "udp://tracker.opentrackr.org:1337",
                    UpLimit = -1,
                    Uploaded = bytesDone,
                    UploadedSession = bytesDone,
                    Upspeed = speed
                };

                if (torrent.Completed.HasValue)
                {
                    var allDownloadsComplete = torrent.Downloads.All(m => m.Completed.HasValue);
                    var hasDownloadsWithErrors = torrent.Downloads.Any(m => m.Error != null);

                    if (torrent.Downloads.Count == 0 || hasDownloadsWithErrors || torrent.RdStatus == RealDebridStatus.Error)
                    {
                        result.State = "error";
                    }
                    else if (allDownloadsComplete)
                    {
                        result.State = "pausedUP";
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

            foreach (var file in torrent.Files.Where(m => m.Selected))
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
            var savePath = AppDefaultSavePath();

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
                PieceSize = bytesTotal,
                PiecesHave = torrent.Downloads.Count(m => m.Completed.HasValue),
                PiecesNum = torrent.Downloads.Count,
                Reannounce = 0,
                SavePath = savePath,
                SeedingTime = 1,
                Seeds = 100,
                SeedsTotal = 100,
                ShareRatio = 9999,
                TimeElapsed = (Int64) (DateTimeOffset.UtcNow - torrent.Added).TotalSeconds,
                TotalDownloaded = bytesDone,
                TotalDownloadedSession = bytesDone,
                TotalSize = bytesTotal,
                TotalUploaded = bytesDone,
                TotalUploadedSession = bytesDone,
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

        public async Task TorrentsAddMagnet(String magnetLink, String category, Int32? priority)
        {
            var downloadAction = Settings.Get.OnlyDownloadAvailableFiles == 1 ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll;

            await _torrents.UploadMagnet(magnetLink, category, downloadAction, TorrentFinishedAction.None, Settings.Get.MinFileSize, null, priority);
        }

        public async Task TorrentsAddFile(Byte[] fileBytes, String category, Int32? priority)
        {
            var downloadAction = Settings.Get.OnlyDownloadAvailableFiles == 1 ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll;

            await _torrents.UploadFile(fileBytes, category, downloadAction, TorrentFinishedAction.None, Settings.Get.MinFileSize, null, priority);
        }

        public async Task TorrentsSetCategory(String hash, String category)
        {
            await _torrents.UpdateCategory(hash, category);
        }

        public async Task<IDictionary<String, TorrentCategory>> TorrentsCategories()
        {
            var torrents = await _torrents.Get();

            var torrentsToGroup = torrents.Where(m => !String.IsNullOrWhiteSpace(m.Category))
                                          .Select(m => m.Category.ToLower())
                                          .ToList();

            var categories = Settings.Get
                                     .Categories
                                     .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            torrentsToGroup.AddRange(categories);

            var results = new Dictionary<String, TorrentCategory>();

            if (torrentsToGroup.Count > 0)
            {
                results = torrentsToGroup.Distinct(StringComparer.CurrentCultureIgnoreCase)
                                         .ToDictionary(m => m,
                                                       m => new TorrentCategory
                                                       {
                                                           Name = m,
                                                           SavePath = ""
                                                       });
            }

            return results;
        }

        public async Task CategoryCreate(String category)
        {
            var categoriesSetting = Settings.Get.Categories;

            if (String.IsNullOrWhiteSpace(categoriesSetting))
            {
                categoriesSetting = category;
            }
            else
            {
                var categoryList = categoriesSetting
                                   .Split(",", StringSplitOptions.RemoveEmptyEntries)
                                   .Distinct(StringComparer.CurrentCultureIgnoreCase)
                                   .ToList();

                if (!categoryList.Contains(category))
                {
                    categoryList.Add(category);
                }

                categoriesSetting = String.Join(",", categoryList);
            }

            await _settings.UpdateString("Categories", categoriesSetting);
        }

        public async Task CategoryRemove(String category)
        {
            var categoriesSetting = Settings.Get.Categories;

            if (String.IsNullOrWhiteSpace(categoriesSetting))
            {
                return;
            }

            var categoryList = categoriesSetting.Split(",", StringSplitOptions.RemoveEmptyEntries)
                                                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                                                .ToList();

            categoryList = categoryList.Where(m => m != category).ToList();

            categoriesSetting = String.Join(",", categoryList);

            await _settings.UpdateString("Categories", categoriesSetting);
        }

        public async Task TorrentsTopPrio(String hash)
        {
            await _torrents.UpdatePriority(hash, 1);
        }

        public async Task TorrentPause(String hash)
        {
            var torrent = await _torrents.GetByHash(hash);

            if (torrent == null)
            {
                return;
            }

            var downloads = await _downloads.GetForTorrent(torrent.TorrentId);

            foreach (var download in downloads)
            {
                if (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
                {
                    await downloadClient.Pause();
                }
            }
        }

        public async Task TorrentResume(String hash)
        {
            var torrent = await _torrents.GetByHash(hash);

            if (torrent == null)
            {
                return;
            }

            var downloads = await _downloads.GetForTorrent(torrent.TorrentId);

            foreach (var download in downloads)
            {
                if (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
                {
                    await downloadClient.Resume();
                }
            }
        }
    }
}

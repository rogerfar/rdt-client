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
        Task TorrentsAdd(String magnetLink, Boolean autoDownload, Boolean autoDelete);
        Task TorrentsAddFile(Byte[] fileBytes, Boolean autoDownload, Boolean autoDelete);
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
            var downloadPath = await _settings.GetString("DownloadFolder");
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
                var result = new TorrentInfo();
                result.AddedOn = torrent.RdAdded.ToUnixTimeSeconds();
                result.AmountLeft = (Int64) (torrent.RdSize * (100.0 - torrent.RdProgress) / 100.0);
                result.AutoTmm = false;
                result.Availability = 2;
                result.Category = torrent.Category ?? "";
                result.Completed = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0));
                result.CompletionOn = torrent.RdEnded?.ToUnixTimeSeconds();
                result.DlLimit = -1;
                result.Dlspeed = torrent.RdSpeed ?? 0;
                result.Downloaded = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0));
                result.DownloadedSession = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0));
                result.Eta = 0;
                result.FlPiecePrio = false;
                result.ForceStart = false;
                result.Hash = torrent.Hash;
                result.LastActivity = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                result.MagnetUri = "";
                result.MaxRatio = -1;
                result.MaxSeedingTime = -1;
                result.Name = torrent.RdName;
                result.NumComplete = 10;
                result.NumIncomplete = 0;
                result.NumLeechs = 100;
                result.NumSeeds = 100;
                result.Priority = ++prio;
                result.Progress = (Int64) (torrent.RdProgress / 100.0);
                result.Ratio = 1;
                result.RatioLimit = 1;
                result.SavePath = savePath;
                result.SeedingTimeLimit = 1;
                result.SeenComplete = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                result.SeqDl = false;
                result.Size = torrent.RdSize;
                result.SuperSeeding = false;
                result.Tags = "";
                result.TimeActive = (Int64) (torrent.RdAdded - DateTimeOffset.UtcNow).TotalMinutes;
                result.TotalSize = torrent.RdSize;
                result.Tracker = "udp://tracker.opentrackr.org:1337";
                result.UpLimit = -1;
                result.Uploaded = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0));
                result.UploadedSession = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0));
                result.Upspeed = torrent.RdSpeed ?? 0;
                result.State = torrent.Status switch
                {
                    TorrentStatus.RealDebrid => "downloading",
                    TorrentStatus.WaitingForDownload => "downloading",
                    TorrentStatus.DownloadQueued => "downloading",
                    TorrentStatus.Downloading => "downloading",
                    TorrentStatus.Finished => "pausedUP",
                    TorrentStatus.Error => "error",
                    _ => throw new ArgumentOutOfRangeException()
                };

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
                var result = new TorrentFileItem();
                result.Name = file.Path;
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

            var result = new TorrentProperties
            {
                AdditionDate = torrent.RdAdded.ToUnixTimeSeconds(),
                Comment = "RealDebridClient <https://github.com/rogerfar/rdt-client>",
                CompletionDate = torrent.RdEnded?.ToUnixTimeSeconds() ?? -1,
                CreatedBy = "RealDebridClient <https://github.com/rogerfar/rdt-client>",
                CreationDate = torrent.RdAdded.ToUnixTimeSeconds(),
                DlLimit = -1,
                DlSpeed = torrent.RdSpeed ?? 0,
                DlSpeedAvg = torrent.RdSpeed ?? 0,
                Eta = 0,
                LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                NbConnections = 0,
                NbConnectionsLimit = 100,
                Peers = 0,
                PeersTotal = 2,
                PieceSize = torrent.Files.Count > 0 ? torrent.RdSize / torrent.Files.Count : 0,
                PiecesHave = torrent.Downloads.Count(m => m.Status == DownloadStatus.Finished),
                PiecesNum = torrent.Files.Count,
                Reannounce = 0,
                SavePath = savePath,
                SeedingTime = 1,
                Seeds = 100,
                SeedsTotal = 100,
                ShareRatio = 9999,
                TimeElapsed = (Int64) (torrent.RdAdded - DateTimeOffset.UtcNow).TotalMinutes,
                TotalDownloaded = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0)),
                TotalDownloadedSession = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0)),
                TotalSize = torrent.RdSize,
                TotalUploaded = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0)),
                TotalUploadedSession = (Int64) (torrent.RdSize * (torrent.RdProgress / 100.0)),
                TotalWasted = 0,
                UpLimit = -1,
                UpSpeed = torrent.RdSpeed ?? 0,
                UpSpeedAvg = torrent.RdSpeed ?? 0
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

            await _torrents.Delete(torrent.TorrentId);

            if (deleteFiles)
            {
                var savePath = await AppDefaultSavePath();

                var torrentPath = Path.Combine(savePath, torrent.RdName);

                if (Directory.Exists(torrentPath))
                {
                    Directory.Delete(torrentPath, true);
                }
            }
        }

        public async Task TorrentsAdd(String magnetLink, Boolean autoDownload, Boolean autoDelete)
        {
            await _torrents.UploadMagnet(magnetLink, autoDownload, autoDelete);
        }

        public async Task TorrentsAddFile(Byte[] fileBytes, Boolean autoDownload, Boolean autoDelete)
        {
            await _torrents.UploadFile(fileBytes, autoDownload, autoDelete);
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
                                                           SavePath = savePath
                                                       });
            }

            return results;
        }
    }
}
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.QBittorrent;

namespace RdtClient.Service.Services;

public class QBittorrent(ILogger<QBittorrent> logger, Settings settings, Authentication authentication, Torrents torrents, Downloads downloads)
{
    public async Task<Boolean> AuthLogin(String userName, String password)
    {
        logger.LogDebug("Auth login");

        var login = await authentication.Login(userName, password);

        return login.Succeeded;
    }

    public async Task AuthLogout()
    {
        logger.LogDebug("Auth logout");

        await authentication.Logout();
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
            ScanDirs = new(),
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

        var savePath = Settings.AppDefaultSavePath;

        preferences.SavePath = savePath;
        preferences.TempPath = $"{savePath}temp{Path.DirectorySeparatorChar}";

        var user = await authentication.GetUser();

        if (user != null)
        {
            preferences.WebUiUsername = user.UserName;
        }

        return preferences;
    }

    public async Task<IList<TorrentInfo>> TorrentInfo()
    {
        var savePath = Settings.AppDefaultSavePath;

        var results = new List<TorrentInfo>();

        var allTorrents = await torrents.Get();

        var prio = 0;

        Double downloadProgress = 0;

        foreach (var torrent in allTorrents)
        {
            var downloadPath = savePath;

            if (!String.IsNullOrWhiteSpace(torrent.Category))
            {
                downloadPath = Path.Combine(downloadPath, torrent.Category);
            }

            var torrentPath = downloadPath;
            if (!String.IsNullOrWhiteSpace(torrent.RdName))
            {
                // Alldebrid stores single file torrents at the root folder.
                if (torrent.ClientKind == Provider.AllDebrid && torrent.Files.Count == 1)
                {
                    torrentPath = Path.Combine(downloadPath, torrent.Files[0].Path);
                } else
                {
                    torrentPath = Path.Combine(downloadPath, torrent.RdName) + Path.DirectorySeparatorChar;
                }
            }

            Int64 bytesDone = 0;
            Int64 bytesTotal = 0;
            Int64 speed = 0;
            Double rdProgress = 0;
            try
            {
                rdProgress = (torrent.RdProgress ?? 0.0) / 100.0;
                bytesTotal = torrent.RdSize ?? 1;
                bytesDone = (Int64) (bytesTotal * rdProgress);
                speed = torrent.RdSpeed ?? 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"""
                Error calculating progress:
                RdProgress: {torrent.RdProgress}
                RdSize: {torrent.RdSize}
                RdSpeed: {torrent.RdSpeed}
                """);
            }

            if (torrent.Downloads.Count > 0)
            {
                bytesDone = torrent.Downloads.Sum(m => m.BytesDone);
                bytesTotal = torrent.Downloads.Sum(m => m.BytesTotal);
                speed = (Int32) torrent.Downloads.Average(m => m.Speed);
                downloadProgress = bytesTotal > 0 ? (Double) bytesDone / bytesTotal : 0;
            }

            var progress = (rdProgress + downloadProgress) / 2.0;

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
                NumLeechs = 1,
                NumSeeds = torrent.RdSeeders ?? 1,
                Priority = ++prio,
                Progress = (Single)progress,
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

            if (!String.IsNullOrWhiteSpace(torrent.Error))
            {
                result.State = "error";
            }
            else if (torrent.Completed.HasValue)
            {
                result.State = "pausedUP";
            }
            else if (torrent.RdStatus == TorrentStatus.Downloading && torrent.RdSeeders < 1)
            {
                result.State = "stalledDL";
            }

            results.Add(result);
        }

        return results;
    }

    public async Task<IList<TorrentFileItem>?> TorrentFileContents(String hash)
    {
        var results = new List<TorrentFileItem>();

        var torrent = await torrents.GetByHash(hash);

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

    public async Task<TorrentProperties?> TorrentProperties(String hash)
    {
        var savePath = Settings.AppDefaultSavePath;

        var torrent = await torrents.GetByHash(hash);

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
        if (deleteFiles)
        {
            logger.LogDebug($"Delete {hash}, with files");
        }
        else
        {
            logger.LogDebug($"Delete {hash}, no files");
        }

        var torrent = await torrents.GetByHash(hash);

        if (torrent == null)
        {
            return;
        }

        switch (Settings.Get.Integrations.Default.FinishedAction)
        {
            case TorrentFinishedAction.RemoveAllTorrents:
                logger.LogDebug("Removing torrents from debrid provider and RDT-Client, no files");
                await torrents.Delete(torrent.TorrentId, true, true, deleteFiles);

                break;
            case TorrentFinishedAction.RemoveRealDebrid:
                logger.LogDebug("Removing torrents from debrid provider, no files");
                await torrents.Delete(torrent.TorrentId, false, true, deleteFiles);

                break;
            case TorrentFinishedAction.RemoveClient:
                logger.LogDebug("Removing torrents from client, no files");
                await torrents.Delete(torrent.TorrentId, true, false, deleteFiles);

                break;
            case TorrentFinishedAction.None:
                logger.LogDebug("Not removing torrents or files");

                break;
            default:
                logger.LogDebug($"Invalid torrent FinishedAction {torrent.FinishedAction}", torrent);

                break;
        }
    }

    public async Task TorrentsAddMagnet(String magnetLink, String? category, Int32? priority)
    {
        logger.LogDebug($"Add magnet {category}");

        var torrent = new Torrent
        {
            Category = category,
            DownloadClient = Settings.Get.DownloadClient.Client,
            HostDownloadAction = Settings.Get.Integrations.Default.HostDownloadAction,
            DownloadAction = Settings.Get.Integrations.Default.OnlyDownloadAvailableFiles ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll,
            FinishedAction = TorrentFinishedAction.None,
            DownloadMinSize = Settings.Get.Integrations.Default.MinFileSize,
            IncludeRegex = Settings.Get.Integrations.Default.IncludeRegex,
            ExcludeRegex = Settings.Get.Integrations.Default.ExcludeRegex,
            TorrentRetryAttempts = Settings.Get.Integrations.Default.TorrentRetryAttempts,
            DownloadRetryAttempts = Settings.Get.Integrations.Default.DownloadRetryAttempts,
            DeleteOnError = Settings.Get.Integrations.Default.DeleteOnError,
            Lifetime = Settings.Get.Integrations.Default.TorrentLifetime,
            Priority = priority ?? (Settings.Get.Integrations.Default.Priority > 0 ? Settings.Get.Integrations.Default.Priority : null)
        };

        await torrents.UploadMagnet(magnetLink, torrent);
    }

    public async Task TorrentsAddFile(Byte[] fileBytes, String? category, Int32? priority)
    {
        logger.LogDebug($"Add file {category}");

        var torrent = new Torrent
        {
            Category = category,
            DownloadClient = Settings.Get.DownloadClient.Client,
            HostDownloadAction = Settings.Get.Integrations.Default.HostDownloadAction,
            DownloadAction = Settings.Get.Integrations.Default.OnlyDownloadAvailableFiles ? TorrentDownloadAction.DownloadAvailableFiles : TorrentDownloadAction.DownloadAll,
            FinishedAction = TorrentFinishedAction.None,
            DownloadMinSize = Settings.Get.Integrations.Default.MinFileSize,
            IncludeRegex = Settings.Get.Integrations.Default.IncludeRegex,
            ExcludeRegex = Settings.Get.Integrations.Default.ExcludeRegex,
            TorrentRetryAttempts = Settings.Get.Integrations.Default.TorrentRetryAttempts,
            DownloadRetryAttempts = Settings.Get.Integrations.Default.DownloadRetryAttempts,
            DeleteOnError = Settings.Get.Integrations.Default.DeleteOnError,
            Lifetime = Settings.Get.Integrations.Default.TorrentLifetime,
            Priority = priority ?? (Settings.Get.Integrations.Default.Priority > 0 ? Settings.Get.Integrations.Default.Priority : null)
        };

        await torrents.UploadFile(fileBytes, torrent);
    }

    public async Task TorrentsSetCategory(String hash, String? category)
    {
        await torrents.UpdateCategory(hash, category);
    }

    public async Task<IDictionary<String, TorrentCategory>> TorrentsCategories()
    {
        var allTorrents = await torrents.Get();

        var torrentsToGroup = allTorrents.Where(m => !String.IsNullOrWhiteSpace(m.Category))
                                      .Select(m => m.Category!.ToLower())
                                      .ToList();

        var categoryList = (Settings.Get.General.Categories ?? "")
                           .Split(",", StringSplitOptions.RemoveEmptyEntries)
                           .Distinct(StringComparer.CurrentCultureIgnoreCase)
                           .Select(m => m.Trim())
                           .ToList();

        torrentsToGroup.AddRange(categoryList);

        var results = new Dictionary<String, TorrentCategory>();

        if (torrentsToGroup.Count > 0)
        {
            results = torrentsToGroup.Distinct(StringComparer.CurrentCultureIgnoreCase)
                                     .ToDictionary(m => m,
                                                   m => new TorrentCategory
                                                   {
                                                       Name = m,
                                                       SavePath = Path.Combine(Settings.AppDefaultSavePath, m)
                                                   });
        }

        return results;
    }

    public async Task CategoryCreate(String? category)
    {
        if (category == null)
        {
            return;
        }

        category = category.Trim();

        var categoriesSetting = Settings.Get.General.Categories;

        var categoryList = (categoriesSetting ?? "")
                           .Split(",", StringSplitOptions.RemoveEmptyEntries)
                           .Distinct(StringComparer.CurrentCultureIgnoreCase)
                           .Select(m => m.Trim())
                           .ToList();

        if (!categoryList.Contains(category))
        {
            categoryList.Add(category);
        }

        categoriesSetting = String.Join(",", categoryList);

        await settings.Update("General:Categories", categoriesSetting);
    }

    public async Task CategoryRemove(String? category)
    {
        if (category == null)
        {
            return;
        }

        category = category.Trim();

        var categoriesSetting = Settings.Get.General.Categories;

        var categoryList = (categoriesSetting ?? "")
                           .Split(",", StringSplitOptions.RemoveEmptyEntries)
                           .Distinct(StringComparer.CurrentCultureIgnoreCase)
                           .Select(m => m.Trim())
                           .ToList();

        categoryList = categoryList.Where(m => m != category).ToList();

        categoriesSetting = String.Join(",", categoryList);

        await settings.Update("General:Categories", categoriesSetting);
    }

    public async Task TorrentsTopPrio(String hash)
    {
        await torrents.UpdatePriority(hash, 1);
    }

    public async Task TorrentPause(String hash)
    {
        var torrent = await torrents.GetByHash(hash);

        if (torrent == null)
        {
            return;
        }

        var downloadsForTorrent = await downloads.GetForTorrent(torrent.TorrentId);

        foreach (var download in downloadsForTorrent)
        {
            if (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
            {
                await downloadClient.Pause();
            }
        }
    }

    public async Task TorrentResume(String hash)
    {
        var torrent = await torrents.GetByHash(hash);

        if (torrent == null)
        {
            return;
        }

        var downloadsForTorrent = await downloads.GetForTorrent(torrent.TorrentId);

        foreach (var download in downloadsForTorrent)
        {
            if (TorrentRunner.ActiveDownloadClients.TryGetValue(download.DownloadId, out var downloadClient))
            {
                await downloadClient.Resume();
            }
        }
    }

    public async Task<SyncMetaData> SyncMainData()
    {
        var torrentInfo = await TorrentInfo();

        var categories = await TorrentsCategories();

        var activeDownloads = TorrentRunner.ActiveDownloadClients.Sum(m => m.Value.Speed);

        return new()
        {
            Categories = categories,
            FullUpdate = true,
            Rid = 0,
            Tags = null,
            Trackers = new Dictionary<String, List<String>>(),
            Torrents = torrentInfo.ToDictionary(m => m.Hash, m => m),
            ServerState = new()
            {
                DlInfoSpeed = activeDownloads,
                UpInfoSpeed = 0
            }
        };
    }

    public static TransferInfo TransferInfo()
    {
        var activeDownloads = TorrentRunner.ActiveDownloadClients.Sum(m => m.Value.Speed);

        return new()
        {
            ConnectionStatus = "connected",
            DlInfoData = DownloadClient.GetTotalBytesDownloadedThisSession(),
            DlInfoSpeed = activeDownloads,
            DlRateLimit = Settings.Get.DownloadClient.MaxSpeed
        };
    }
}
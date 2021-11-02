using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RDNET;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services.TorrentClients
{
    public class RealDebridTorrentClient : ITorrentClient
    {
        private readonly ILogger<RealDebridTorrentClient> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private TimeSpan _offset;

        public RealDebridTorrentClient(ILogger<RealDebridTorrentClient> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private RdNetClient GetClient()
        {
            var apiKey = Settings.Get.RealDebridApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("Real-Debrid API Key not set in the settings");
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var rdtNetClient = new RdNetClient(null, httpClient, 5);
            rdtNetClient.UseApiAuthentication(apiKey);

            // Get the server time to fix up the timezones on results
            var serverTime = rdtNetClient.Api.GetIsoTimeAsync().Result;
            _offset = serverTime.Offset;

            return rdtNetClient;
        }

        private TorrentClientTorrent Map(Torrent torrent)
        {
            return new TorrentClientTorrent
            {
                Id = torrent.Id,
                Filename = torrent.Filename,
                OriginalFilename = torrent.OriginalFilename,
                Hash = torrent.Hash,
                Bytes = torrent.Bytes,
                OriginalBytes = torrent.OriginalBytes,
                Host = torrent.Host,
                Split = torrent.Split,
                Progress = torrent.Progress,
                Status = torrent.Status,
                Added = ChangeTimeZone(torrent.Added).Value,
                Files = (torrent.Files ?? new List<TorrentFile>()).Select(m => new TorrentClientFile
                {
                    Path = m.Path,
                    Bytes = m.Bytes,
                    Id = m.Id,
                    Selected = m.Selected
                }).ToList(),
                Links = torrent.Links,
                Ended = ChangeTimeZone(torrent.Ended),
                Speed = torrent.Speed,
                Seeders = torrent.Seeders,
            };
        }

        public async Task<IList<TorrentClientTorrent>> GetTorrents()
        {
            var page = 0;
            var results = new List<Torrent>();

            while (true)
            {
                var pagedResults = await GetClient().Torrents.GetAsync(page, 100);

                results.AddRange(pagedResults);

                if (pagedResults.Count == 0)
                {
                    break;
                }

                page += 100;
            }

            return results.Select(Map).ToList();
        }

        public async Task<TorrentClientUser> GetUser()
        {
            var user = await GetClient().User.GetAsync();
            
            return new TorrentClientUser
            {
                Username = user.Username,
                Expiration = user.Premium > 0 ? user.Expiration : null
            };
        }

        public async Task<String> AddMagnet(String magnetLink)
        {
            var result = await GetClient().Torrents.AddMagnetAsync(magnetLink);

            return result.Id;
        }

        public async Task<String> AddFile(Byte[] bytes)
        {
            var result = await GetClient().Torrents.AddFileAsync(bytes);

            return result.Id;
        }

        public async Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
        {
            var result = await GetClient().Torrents.GetAvailableFiles(hash);

            var files = result.SelectMany(m => m.Value).SelectMany(m => m.Value).SelectMany(m => m.Values);

            var groups = files.GroupBy(m => $"{m.Filename}-{m.Filesize}");

            var torrentClientAvailableFiles = groups.Select(m => new TorrentClientAvailableFile
            {
                Filename = m.First().Filename,
                Filesize = m.First().Filesize
            } ).ToList();

            return torrentClientAvailableFiles;
        }

        public async Task SelectFiles(Data.Models.Data.Torrent torrent)
        {
            var files = torrent.Files;

            Log("Seleting files", torrent);

            if (torrent.DownloadAction == TorrentDownloadAction.DownloadAvailableFiles)
            {
                Log($"Determining which files are already available on RealDebrid", torrent);

                var availableFiles = await GetAvailableFiles(torrent.Hash);

                Log($"Found {files.Count}/{torrent.Files.Count} available files on RealDebrid", torrent);

                files = torrent.Files.Where(m => availableFiles.Any(f => m.Path.EndsWith(f.Filename))).ToList();
            }
            else if (torrent.DownloadAction == TorrentDownloadAction.DownloadAll)
            {
                Log("Selecting all files", torrent);
                files = torrent.Files.ToList();
            }
            else if (torrent.DownloadAction == TorrentDownloadAction.DownloadManual)
            {
                Log("Selecting manual selected files", torrent);
                files = torrent.Files.Where(m => torrent.ManualFiles.Any(f => m.Path.EndsWith(f))).ToList();
            }

            Log($"Selecting {files.Count}/{torrent.Files.Count} files", torrent);

            if (torrent.DownloadAction != TorrentDownloadAction.DownloadManual && torrent.DownloadMinSize > 0)
            {
                var minFileSize = torrent.DownloadMinSize * 1024 * 1024;

                Log($"Determining which files are over {minFileSize} bytes", torrent);

                files = files.Where(m => m.Bytes > minFileSize)
                             .ToList();

                Log($"Found {files.Count} files that match the minimum file size criterea", torrent);
            }

            if (files.Count == 0)
            {
                Log($"Filtered all files out! Downloading ALL files instead!", torrent);

                files = torrent.Files;
            }

            var fileIds = files.Select(m => m.Id.ToString()).ToArray();

            Log($"Selecting files:");

            foreach (var file in files)
            {
                Log($"{file.Id}: {file.Path} ({file.Bytes}b)");
            }

            Log("", torrent);

            await GetClient().Torrents.SelectFilesAsync(torrent.RdId, fileIds.ToArray());
        }

        public async Task<TorrentClientTorrent> GetInfo(String torrentId)
        {
            var result = await GetClient().Torrents.GetInfoAsync(torrentId);

            return Map(result);
        }

        public async Task Delete(String torrentId)
        {
            await GetClient().Torrents.DeleteAsync(torrentId);
        }

        public async Task<String> Unrestrict(String link)
        {
            var result = await GetClient().Unrestrict.LinkAsync(link);

            return result.Download;
        }

        public async Task<Data.Models.Data.Torrent> UpdateData(Data.Models.Data.Torrent torrent, TorrentClientTorrent torrentClientTorrent)
        {
            try
            {
                var rdTorrent = await GetInfo(torrent.RdId);

                if (!String.IsNullOrWhiteSpace(rdTorrent.Filename))
                {
                    torrent.RdName = rdTorrent.Filename;
                }

                if (!String.IsNullOrWhiteSpace(rdTorrent.OriginalFilename))
                {
                    torrent.RdName = rdTorrent.OriginalFilename;
                }

                if (rdTorrent.Bytes > 0)
                {
                    torrent.RdSize = rdTorrent.Bytes;
                }
                else if (rdTorrent.OriginalBytes > 0)
                {
                    torrent.RdSize = rdTorrent.OriginalBytes;
                }

                if (rdTorrent.Files != null)
                {
                    torrent.RdFiles = JsonConvert.SerializeObject(rdTorrent.Files);
                }

                torrent.RdHost = rdTorrent.Host;
                torrent.RdSplit = rdTorrent.Split;
                torrent.RdProgress = rdTorrent.Progress;
                torrent.RdAdded = rdTorrent.Added;
                torrent.RdEnded = rdTorrent.Ended;
                torrent.RdSpeed = rdTorrent.Speed;
                torrent.RdSeeders = rdTorrent.Seeders;
                torrent.RdStatusRaw = rdTorrent.Status;

                torrent.RdStatus = rdTorrent.Status switch
                {
                    "magnet_error" => TorrentStatus.Error,
                    "magnet_conversion" => TorrentStatus.Processing,
                    "waiting_files_selection" => TorrentStatus.WaitingForFileSelection,
                    "queued" => TorrentStatus.Downloading,
                    "downloading" => TorrentStatus.Downloading,
                    "downloaded" => TorrentStatus.Finished,
                    "error" => TorrentStatus.Error,
                    "virus" => TorrentStatus.Error,
                    "compressing" => TorrentStatus.Downloading,
                    "uploading" => TorrentStatus.Uploading,
                    "dead" => TorrentStatus.Error,
                    _ => TorrentStatus.Error
                };
            }
            catch (Exception ex)
            {
                if (ex.Message == "Resource not found")
                {
                    torrent.RdStatusRaw = "deleted";
                }
                else
                {
                    throw;
                }
            }

            return torrent;
        }

        public async Task<IList<String>> GetDownloadLinks(Data.Models.Data.Torrent torrent)
        {
            var rdTorrent = await GetInfo(torrent.RdId);

            var downloadLinks = rdTorrent.Links.Where(m => !String.IsNullOrWhiteSpace(m)).ToList();

            Log($"Found {downloadLinks.Count} links", torrent);

            foreach (var link in downloadLinks)
            {
                Log($"{link}", torrent);
            }

            // Check if all the links are set that have been selected
            if (torrent.Files.Count(m => m.Selected) == downloadLinks.Count)
            {
                return downloadLinks;
            }

            // Check if all all the links are set for manual selection
            if (torrent.ManualFiles.Count == downloadLinks.Count)
            {
                return downloadLinks;
            }

            // If there is only 1 link, delay for 1 minute to see if more links pop up.
            if (downloadLinks.Count == 1 && torrent.RdEnded.HasValue && DateTime.UtcNow > torrent.RdEnded.Value.ToUniversalTime().AddMinutes(1))
            {
                var rem = torrent.RdEnded.Value.ToUniversalTime() - DateTimeOffset.UtcNow;
                Log($"Delaying {rem.TotalSeconds} more seconds", torrent);
                return downloadLinks;
            }
            
            return null;
        }
        
        private DateTimeOffset? ChangeTimeZone(DateTimeOffset? dateTimeOffset)
        {
            return dateTimeOffset?.Subtract(_offset).ToOffset(_offset);
        }

        private void Log(String message, Data.Models.Data.Torrent torrent = null)
        {
            if (torrent != null)
            {
                message = $"{message} {torrent.ToLog()}";
            }

            _logger.LogDebug(message);
        }
    }
}

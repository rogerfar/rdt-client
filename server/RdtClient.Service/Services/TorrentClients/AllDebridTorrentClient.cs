using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AllDebridNET;
using RdtClient.Data.Models.TorrentClient;

namespace RdtClient.Service.Services.TorrentClients
{
    public class AllDebridTorrentClient : ITorrentClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AllDebridTorrentClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private AllDebridNETClient GetClient()
        {
            var apiKey = Settings.Get.RealDebridApiKey;

            if (String.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("All-Debrid API Key not set in the settings");
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var allDebridNetClient = new AllDebridNETClient("RealDebridClient", apiKey);

            return allDebridNetClient;
        }

        private static TorrentClientTorrent Map(Magnet torrent)
        {
            return new TorrentClientTorrent
            {
                Id = torrent.Id.ToString(),
                Filename = torrent.Filename,
                OriginalFilename = torrent.Filename,
                Hash = torrent.Hash,
                Bytes = torrent.Size,
                OriginalBytes = torrent.Size,
                Host = null,
                Split = 0,
                Progress = torrent.ProcessingPerc,
                Status = torrent.Status,
                Added = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(torrent.UploadDate),
                Files = (torrent.Links ?? new List<Link>()).Select(m => new TorrentClientFile
                {
                    Path = m.Filename,
                    Bytes = m.Size,
                    Id = 0,
                    Selected = true
                }).ToList(),
                Links = (torrent.Links ?? new List<Link>()).Select(m => m.LinkUrl.ToString()).ToList(),
                Ended = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(torrent.CompletionDate),
                Speed = torrent.DownloadSpeed,
                Seeders = torrent.Seeders
            };
        }

        public async Task<IList<TorrentClientTorrent>> GetTorrents()
        {
            var results = await GetClient().Magnet.StatusAllAsync();
            return results.Select(Map).ToList();
        }

        public async Task<TorrentClientUser> GetUser()
        {
            var user = await GetClient().User.GetAsync();
            
            return new TorrentClientUser
            {
                Username = user.Username,
                Expiration = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(user.PremiumUntil)
            };
        }

        public async Task<String> AddMagnet(String magnetLink)
        {
            var result = await GetClient().Magnet.UploadMagnetAsync(magnetLink);

            return result.Id.ToString();
        }

        public async Task<String> AddFile(Byte[] bytes)
        {
            var result = await GetClient().Magnet.UploadFileAsync(bytes);

            return result.Id.ToString();
        }

        public Task<List<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
        {
            return Task.FromResult(new List<TorrentClientAvailableFile>());
        }

        public Task SelectFiles(String torrentId, IList<String> fileIds)
        {
            return Task.CompletedTask;
        }

        public async Task<TorrentClientTorrent> GetInfo(String torrentId)
        {
            var result = await GetClient().Magnet.StatusAsync(torrentId);

            return Map(result);
        }

        public async Task Delete(String torrentId)
        {
            await GetClient().Magnet.DeleteAsync(torrentId);
        }

        public async Task<String> Unrestrict(String link)
        {
            var result = await GetClient().Links.DownloadLinkAsync(link);

            return result.Link;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RDNET;
using RdtClient.Service.Models.TorrentClient;

namespace RdtClient.Service.Services.TorrentClients
{
    public class AllDebridTorrentClient : ITorrentClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AllDebridTorrentClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private RdNetClient GetRdNetClient()
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

            return rdtNetClient;
        }

        private static TorrentClientTorrent Map(Torrent torrent)
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
                Added = torrent.Added,
                Files = (torrent.Files ?? new List<TorrentFile>()).Select(m => new TorrentClientTorrentFile
                {
                    Path = m.Path,
                    Bytes = m.Bytes,
                    Id = m.Id,
                    Selected = m.Selected
                }).ToList(),
                Links = torrent.Links,
                Ended = torrent.Ended,
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
                var pagedResults = await GetRdNetClient().Torrents.GetAsync(page, 100);

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
            var user = await GetRdNetClient().User.GetAsync();
            
            return new TorrentClientUser
            {
                Username = user.Username,
                Expiration = user.Expiration
            };
        }

        public async Task<String> AddMagnet(String magnetLink)
        {
            var result = await GetRdNetClient().Torrents.AddMagnetAsync(magnetLink);

            return result.Id;
        }

        public async Task<String> AddFile(Byte[] bytes)
        {
            var result = await GetRdNetClient().Torrents.AddFileAsync(bytes);

            return result.Id;
        }

        public async Task<List<TorrentClientAvailableFile>> GetAvailableFiles(String hash)
        {
            var result = await GetRdNetClient().Torrents.GetAvailableFiles(hash);

            var files = result.SelectMany(m => m.Value).SelectMany(m => m.Value).SelectMany(m => m.Values);

            var groups = files.GroupBy(m => $"{m.Filename}-{m.Filesize}");

            var torrentClientAvailableFiles = groups.Select(m => new TorrentClientAvailableFile
            {
                Filename = m.First().Filename,
                Filesize = m.First().Filesize
            } ).ToList();

            return torrentClientAvailableFiles;
        }

        public async Task SelectFiles(String torrentId, IList<String> fileIds)
        {
            await GetRdNetClient().Torrents.SelectFilesAsync(torrentId, fileIds.ToArray());
        }

        public async Task<TorrentClientTorrent> GetInfo(String torrentId)
        {
            var result = await GetRdNetClient().Torrents.GetInfoAsync(torrentId);

            return Map(result);
        }

        public async Task Delete(String torrentId)
        {
            await GetRdNetClient().Torrents.DeleteAsync(torrentId);
        }

        public async Task<String> Unrestrict(String link)
        {
            var result = await GetRdNetClient().Unrestrict.LinkAsync(link);

            return result.Download;
        }
    }
}

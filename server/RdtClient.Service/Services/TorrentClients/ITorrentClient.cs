using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RdtClient.Service.Models.TorrentClient;

namespace RdtClient.Service.Services.TorrentClients
{
    public interface ITorrentClient
    {
        Task<IList<TorrentClientTorrent>> GetTorrents();
        Task<TorrentClientUser> GetUser();
        Task<String> AddMagnet(String magnetLink);
        Task<String> AddFile(Byte[] bytes);
        Task<List<TorrentClientAvailableFile>> GetAvailableFiles(String hash);
        Task SelectFiles(String torrentId, IList<String> fileIds);
        Task<TorrentClientTorrent> GetInfo(String torrentId);
        Task Delete(String torrentId);
        Task<String> Unrestrict(String link);
    }
}

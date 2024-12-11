using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.TorrentClient;

namespace RdtClient.Service.Services.TorrentClients;

public interface ITorrentClient
{
    Task<IList<TorrentClientTorrent>> GetTorrents();
    Task<TorrentClientUser> GetUser();
    Task<String> AddMagnet(String magnetLink);
    Task<String> AddFile(Byte[] bytes);
    Task<IList<TorrentClientAvailableFile>> GetAvailableFiles(String hash);
    Task SelectFiles(Torrent torrent);
    Task Delete(String torrentId);
    Task<String> Unrestrict(String link);
    Task<Torrent> UpdateData(Torrent torrent, TorrentClientTorrent? torrentClientTorrent);
    Task<IList<String>?> GetDownloadLinks(Torrent torrent);
    Task<String> GetFileName(String downloadUrl);
}
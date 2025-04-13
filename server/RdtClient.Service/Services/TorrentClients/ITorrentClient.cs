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
    /// <summary>
    /// Tell the debrid provider which files to download.
    /// </summary>
    /// <remark>
    /// Not all providers support this feature.
    /// </remark>
    /// <param name="torrent">The torrent to select files for</param>
    /// <returns>Number of files selected</returns>
    Task<Int32?> SelectFiles(Torrent torrent);
    Task Delete(String torrentId);
    Task<String> Unrestrict(String link);
    Task<Torrent> UpdateData(Torrent torrent, TorrentClientTorrent? torrentClientTorrent);
    Task<IList<DownloadInfo>?> GetDownloadInfos(Torrent torrent);
    /// <summary>
    /// To be called only when <see cref="Data.Models.Data.Download" />.<see cref="Data.Models.Data.Download.FileName" /> is not set by
    /// <see cref="GetDownloadInfos" />
    /// </summary>
    /// <param name="download">The download to get the filename of</param>
    /// <returns>The filename of the download</returns>
    Task<String> GetFileName(Download download);
}
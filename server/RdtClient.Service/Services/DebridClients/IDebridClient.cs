using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.DebridClient;

namespace RdtClient.Service.Services.DebridClients;

public interface IDebridClient
{
    Task<IList<DebridClientTorrent>> GetDownloads();
    Task<DebridClientUser> GetUser();
    Task<String> AddTorrentMagnet(String magnetLink);
    Task<String> AddTorrentFile(Byte[] bytes);
    Task<String> AddNzbLink(String nzbLink);
    Task<String> AddNzbFile(Byte[] bytes, String? name);
    Task<IList<DebridClientAvailableFile>> GetAvailableFiles(String hash);
    /// <summary>
    ///     Tell the debrid provider which files to download.
    /// </summary>
    /// <remark>
    ///     Not all providers support this feature.
    /// </remark>
    /// <param name="torrent">The torrent to select files for</param>
    /// <returns>Number of files selected</returns>
    Task<Int32?> SelectFiles(Torrent torrent);
    Task Delete(Torrent torrent);
    Task<String> Unrestrict(Torrent torrent, String link);
    Task<Torrent> UpdateData(Torrent torrent, DebridClientTorrent? torrentClientTorrent);
    Task<IList<DownloadInfo>?> GetDownloadInfos(Torrent torrent);

    /// <summary>
    ///     To be called only when <see cref="Data.Models.Data.Download" />.<see cref="Data.Models.Data.Download.FileName" />
    ///     is not set by
    ///     <see cref="GetDownloadInfos" />
    /// </summary>
    /// <param name="download">The download to get the filename of</param>
    /// <returns>The filename of the download</returns>
    Task<String> GetFileName(Download download);
}

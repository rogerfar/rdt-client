using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data;

public interface ITorrentData
{
    Task<IList<Torrent>> Get();
    Task<Torrent?> GetById(Guid torrentId);
    Task<Torrent?> GetByHash(String hash);

    Task<Torrent> Add(String rdId,
                      String hash,
                      String? fileOrMagnetContents,
                      Boolean isFile,
                      DownloadClient downloadClient,
                      Torrent torrent);

    Task UpdateRdData(Torrent torrent);
    Task Update(Torrent torrent);
    Task UpdateCategory(Guid torrentId, String? category);
    Task UpdateComplete(Guid torrentId, String? error, DateTimeOffset? datetime, Boolean retry);
    Task UpdateFilesSelected(Guid torrentId, DateTimeOffset datetime);
    Task UpdatePriority(Guid torrentId, Int32? priority);
    Task UpdateRetry(Guid torrentId, DateTimeOffset? dateTime, Int32 retryCount);
    Task UpdateError(Guid torrentId, String error);
    Task Delete(Guid torrentId);
}

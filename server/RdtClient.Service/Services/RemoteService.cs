using Microsoft.AspNetCore.SignalR;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services;

public class RemoteService(IHubContext<RdtHub> hub, Torrents torrents)
{
    public async Task Update()
    {
        var allTorrents = await torrents.Get();

        var torrentDtos = allTorrents.Select(torrent => TorrentDtoMapper.ToUpdateDto(torrent, torrents.GetDownloadStats))
                                     .ToList();

        await hub.Clients.All.SendCoreAsync("update",
        [
            torrentDtos
        ]);
    }

    public async Task UpdateDiskSpaceStatus(Object status)
    {
        await hub.Clients.All.SendCoreAsync("diskSpaceStatus", [status]);
    }

    public async Task UpdateRateLimitStatus(RateLimitStatus status)
    {
        await hub.Clients.All.SendCoreAsync("rateLimitStatus", [status]);
    }
}

using Microsoft.AspNetCore.SignalR;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Services;

public class RemoteService(IHubContext<RdtHub> hub, Torrents torrents)
{
    public async Task Update()
    {
        var allTorrents = await torrents.Get();
            
        // Prevent infinite recursion when serializing
        foreach (var file in allTorrents.SelectMany(torrent => torrent.Downloads))
        {
            file.Torrent = null;
        }
            
        await hub.Clients.All.SendCoreAsync("update",
        [
            allTorrents
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
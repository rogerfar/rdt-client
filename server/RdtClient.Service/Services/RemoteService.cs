using Microsoft.AspNetCore.SignalR;

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
}
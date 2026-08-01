using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RdtClient.Service.Services;

/// <summary>
///     Broadcasts the full torrent list to every connected client, so it must never be reachable anonymously.
///     The AuthSetting policy still allows access when authentication is disabled in the settings.
/// </summary>
[Authorize(Policy = "AuthSetting")]
public class RdtHub : Hub
{
    private static readonly ConcurrentDictionary<String, String> Users = new();

    public static Boolean HasConnections => !Users.IsEmpty;

    public override async Task OnConnectedAsync()
    {
        Users.TryAdd(Context.ConnectionId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Users.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }
}

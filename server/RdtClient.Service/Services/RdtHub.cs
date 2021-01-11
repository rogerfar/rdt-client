using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace RdtClient.Service.Services
{
    public class RdtHub : Hub
    {
        private static readonly ConcurrentDictionary<String, String> Users = new ConcurrentDictionary<String, String>();

        public static Boolean HasConnections => Users.Any();

        public override async Task OnConnectedAsync()
        {
            Users.TryAdd(Context.ConnectionId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Users.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }
    }
}

using System;

namespace RdtClient.Service.Models.TorrentClient
{
    public class TorrentClientUser
    {
        public String Username { get; set; }
        public DateTimeOffset Expiration { get; set; }
    }
}

using System;
using Newtonsoft.Json;

namespace RdtClient.Data.Models.QBittorrent
{
    public class TorrentFileItem
    {
        [JsonProperty("name")]
        public String Name { get; set; }
    }
}

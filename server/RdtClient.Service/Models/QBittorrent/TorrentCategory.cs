using System;
using Newtonsoft.Json;

namespace RdtClient.Service.Models.QBittorrent
{
    public class TorrentCategory
    {
        [JsonProperty("name")]
        public String Name { get; set; }

        [JsonProperty("savePath")]
        public String SavePath { get; set; }
    }
}

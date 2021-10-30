using System;
using Newtonsoft.Json;

namespace RdtClient.Data.Models.QBittorrent
{
    public class AppBuildInfo
    {
        [JsonProperty("bitness")]
        public Int64 Bitness { get; set; }

        [JsonProperty("boost")]
        public String Boost { get; set; }

        [JsonProperty("libtorrent")]
        public String Libtorrent { get; set; }

        [JsonProperty("openssl")]
        public String Openssl { get; set; }

        [JsonProperty("qt")]
        public String Qt { get; set; }

        [JsonProperty("zlib")]
        public String Zlib { get; set; }
    }
}
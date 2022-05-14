using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class AppBuildInfo
{
    [JsonPropertyName("bitness")]
    public Int64? Bitness { get; set; }

    [JsonPropertyName("boost")]
    public String? Boost { get; set; }

    [JsonPropertyName("libtorrent")]
    public String? Libtorrent { get; set; }

    [JsonPropertyName("openssl")]
    public String? Openssl { get; set; }

    [JsonPropertyName("qt")]
    public String? Qt { get; set; }

    [JsonPropertyName("zlib")]
    public String? Zlib { get; set; }
}
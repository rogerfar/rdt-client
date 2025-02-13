namespace RdtClient.Data.Models.TorrentClient;

public class TorrentClientFile
{
    public Int64 Id { get; set; }
    public String Path { get; set; } = default!;
    public Int64 Bytes { get; set; }
    public Boolean Selected { get; set; }
    public String? DownloadLink { get; set; }
}
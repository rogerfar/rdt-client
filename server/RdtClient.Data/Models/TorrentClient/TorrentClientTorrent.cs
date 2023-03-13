namespace RdtClient.Data.Models.TorrentClient;

public class TorrentClientTorrent
{
    public String Id { get; set; } = default!;
    public String Filename { get; set; } = default!;
    public String? OriginalFilename { get; set; }
    public String Hash { get; set; } = default!;
    public Int64 Bytes { get; set; }
    public Int64 OriginalBytes { get; set; }
    public String? Host { get; set; }
    public Int64 Split { get; set; }
    public Int64 Progress { get; set; }
    public String? Status { get; set; }
    public String? Message { get; set; }
    public Int64 StatusCode { get; set; }
    public DateTimeOffset? Added { get; set; }
    public List<TorrentClientFile>? Files { get; set; }
    public List<String>? Links { get; set; }
    public DateTimeOffset? Ended { get; set; }
    public Int64? Speed { get; set; }
    public Int64? Seeders { get; set; }
}
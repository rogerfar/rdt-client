using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class TorrentProperties
{
    [JsonPropertyName("addition_date")]
    public Int64? AdditionDate { get; set; }

    [JsonPropertyName("comment")]
    public String? Comment { get; set; }

    [JsonPropertyName("completion_date")]
    public Int64? CompletionDate { get; set; }

    [JsonPropertyName("created_by")]
    public String? CreatedBy { get; set; }

    [JsonPropertyName("creation_date")]
    public Int64? CreationDate { get; set; }

    [JsonPropertyName("dl_limit")]
    public Int64? DlLimit { get; set; }

    [JsonPropertyName("dl_speed")]
    public Int64? DlSpeed { get; set; }

    [JsonPropertyName("dl_speed_avg")]
    public Int64? DlSpeedAvg { get; set; }

    [JsonPropertyName("eta")]
    public Int64? Eta { get; set; }

    [JsonPropertyName("last_seen")]
    public Int64? LastSeen { get; set; }

    [JsonPropertyName("nb_connections")]
    public Int64? NbConnections { get; set; }

    [JsonPropertyName("nb_connections_limit")]
    public Int64? NbConnectionsLimit { get; set; }

    [JsonPropertyName("peers")]
    public Int64? Peers { get; set; }

    [JsonPropertyName("peers_total")]
    public Int64? PeersTotal { get; set; }

    [JsonPropertyName("piece_size")]
    public Int64? PieceSize { get; set; }

    [JsonPropertyName("pieces_have")]
    public Int64? PiecesHave { get; set; }

    [JsonPropertyName("pieces_num")]
    public Int64? PiecesNum { get; set; }

    [JsonPropertyName("reannounce")]
    public Int64? Reannounce { get; set; }

    [JsonPropertyName("save_path")]
    public String? SavePath { get; set; }

    [JsonPropertyName("seeding_time")]
    public Int64? SeedingTime { get; set; }

    [JsonPropertyName("seeds")]
    public Int64? Seeds { get; set; }

    [JsonPropertyName("seeds_total")]
    public Int64? SeedsTotal { get; set; }

    [JsonPropertyName("share_ratio")]
    public Int64? ShareRatio { get; set; }

    [JsonPropertyName("time_elapsed")]
    public Int64? TimeElapsed { get; set; }

    [JsonPropertyName("total_downloaded")]
    public Int64? TotalDownloaded { get; set; }

    [JsonPropertyName("total_downloaded_session")]
    public Int64? TotalDownloadedSession { get; set; }

    [JsonPropertyName("total_size")]
    public Int64? TotalSize { get; set; }

    [JsonPropertyName("total_uploaded")]
    public Int64? TotalUploaded { get; set; }

    [JsonPropertyName("total_uploaded_session")]
    public Int64? TotalUploadedSession { get; set; }

    [JsonPropertyName("total_wasted")]
    public Int64? TotalWasted { get; set; }

    [JsonPropertyName("up_limit")]
    public Int64? UpLimit { get; set; }

    [JsonPropertyName("up_speed")]
    public Int64? UpSpeed { get; set; }

    [JsonPropertyName("up_speed_avg")]
    public Int64? UpSpeedAvg { get; set; }
}
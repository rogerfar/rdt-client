using System.Text.Json.Serialization;

namespace RdtClient.Data.Models.QBittorrent;

public class TorrentInfo
{
    [JsonPropertyName("added_on")]
    public Int64? AddedOn { get; set; }

    [JsonPropertyName("amount_left")]
    public Int64? AmountLeft { get; set; }

    [JsonPropertyName("auto_tmm")]
    public Boolean AutoTmm { get; set; }

    [JsonPropertyName("availability")]
    public Decimal Availability { get; set; }

    [JsonPropertyName("category")]
    public String? Category { get; set; }

    [JsonPropertyName("completed")]
    public Int64? Completed { get; set; }

    [JsonPropertyName("completion_on")]
    public Int64? CompletionOn { get; set; }
            
    [JsonPropertyName("content_path")]
    public String? ContentPath { get; set; }

    [JsonPropertyName("dl_limit")]
    public Int64? DlLimit { get; set; }

    [JsonPropertyName("dlspeed")]
    public Int64? Dlspeed { get; set; }

    [JsonPropertyName("downloaded")]
    public Int64? Downloaded { get; set; }

    [JsonPropertyName("downloaded_session")]
    public Int64? DownloadedSession { get; set; }

    [JsonPropertyName("eta")]
    public Int64? Eta { get; set; }

    [JsonPropertyName("f_l_piece_prio")]
    public Boolean FlPiecePrio { get; set; }

    [JsonPropertyName("force_start")]
    public Boolean ForceStart { get; set; }

    [JsonPropertyName("hash")]
    public String Hash { get; set; } = default!;

    [JsonPropertyName("last_activity")]
    public Int64? LastActivity { get; set; }

    [JsonPropertyName("magnet_uri")]
    public String? MagnetUri { get; set; }

    [JsonPropertyName("max_ratio")]
    public Int64? MaxRatio { get; set; }

    [JsonPropertyName("max_seeding_time")]
    public Int64? MaxSeedingTime { get; set; }

    [JsonPropertyName("name")]
    public String? Name { get; set; }

    [JsonPropertyName("num_complete")]
    public Int64? NumComplete { get; set; }

    [JsonPropertyName("num_incomplete")]
    public Int64? NumIncomplete { get; set; }

    [JsonPropertyName("num_leechs")]
    public Int64? NumLeechs { get; set; }

    [JsonPropertyName("num_seeds")]
    public Int64? NumSeeds { get; set; }

    [JsonPropertyName("priority")]
    public Int64? Priority { get; set; }

    [JsonPropertyName("progress")]
    public Single Progress { get; set; }

    [JsonPropertyName("ratio")]
    public Int64? Ratio { get; set; }

    [JsonPropertyName("ratio_limit")]
    public Int64? RatioLimit { get; set; }

    [JsonPropertyName("save_path")]
    public String? SavePath { get; set; }

    [JsonPropertyName("seeding_time_limit")]
    public Int64? SeedingTimeLimit { get; set; }

    [JsonPropertyName("seen_complete")]
    public Int64? SeenComplete { get; set; }

    [JsonPropertyName("seq_dl")]
    public Boolean SeqDl { get; set; }

    [JsonPropertyName("size")]
    public Int64? Size { get; set; }

    [JsonPropertyName("state")]
    public String? State { get; set; }

    [JsonPropertyName("super_seeding")]
    public Boolean SuperSeeding { get; set; }

    [JsonPropertyName("tags")]
    public String? Tags { get; set; }

    [JsonPropertyName("time_active")]
    public Int64? TimeActive { get; set; }

    [JsonPropertyName("total_size")]
    public Int64? TotalSize { get; set; }

    [JsonPropertyName("tracker")]
    public String? Tracker { get; set; }

    [JsonPropertyName("up_limit")]
    public Int64? UpLimit { get; set; }

    [JsonPropertyName("uploaded")]
    public Int64? Uploaded { get; set; }

    [JsonPropertyName("uploaded_session")]
    public Int64? UploadedSession { get; set; }

    [JsonPropertyName("upspeed")]
    public Int64? Upspeed { get; set; }
}
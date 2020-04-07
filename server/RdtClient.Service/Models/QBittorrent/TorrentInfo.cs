using System;
using Newtonsoft.Json;

namespace RdtClient.Service.Models.QBittorrent
{
    namespace QuickType
    {
        public class TorrentInfo
        {
            [JsonProperty("added_on")]
            public Int64 AddedOn { get; set; }

            [JsonProperty("amount_left")]
            public Int64 AmountLeft { get; set; }

            [JsonProperty("auto_tmm")]
            public Boolean AutoTmm { get; set; }

            [JsonProperty("availability")]
            public Decimal Availability { get; set; }

            [JsonProperty("category")]
            public String Category { get; set; }

            [JsonProperty("completed")]
            public Int64 Completed { get; set; }

            [JsonProperty("completion_on")]
            public Int64? CompletionOn { get; set; }

            [JsonProperty("dl_limit")]
            public Int64 DlLimit { get; set; }

            [JsonProperty("dlspeed")]
            public Int64 Dlspeed { get; set; }

            [JsonProperty("downloaded")]
            public Int64 Downloaded { get; set; }

            [JsonProperty("downloaded_session")]
            public Int64 DownloadedSession { get; set; }

            [JsonProperty("eta")]
            public Int64 Eta { get; set; }

            [JsonProperty("f_l_piece_prio")]
            public Boolean FlPiecePrio { get; set; }

            [JsonProperty("force_start")]
            public Boolean ForceStart { get; set; }

            [JsonProperty("hash")]
            public String Hash { get; set; }

            [JsonProperty("last_activity")]
            public Int64 LastActivity { get; set; }

            [JsonProperty("magnet_uri")]
            public String MagnetUri { get; set; }

            [JsonProperty("max_ratio")]
            public Int64 MaxRatio { get; set; }

            [JsonProperty("max_seeding_time")]
            public Int64 MaxSeedingTime { get; set; }

            [JsonProperty("name")]
            public String Name { get; set; }

            [JsonProperty("num_complete")]
            public Int64 NumComplete { get; set; }

            [JsonProperty("num_incomplete")]
            public Int64 NumIncomplete { get; set; }

            [JsonProperty("num_leechs")]
            public Int64 NumLeechs { get; set; }

            [JsonProperty("num_seeds")]
            public Int64 NumSeeds { get; set; }

            [JsonProperty("priority")]
            public Int64 Priority { get; set; }

            [JsonProperty("progress")]
            public Decimal Progress { get; set; }

            [JsonProperty("ratio")]
            public Int64 Ratio { get; set; }

            [JsonProperty("ratio_limit")]
            public Int64 RatioLimit { get; set; }

            [JsonProperty("save_path")]
            public String SavePath { get; set; }

            [JsonProperty("seeding_time_limit")]
            public Int64 SeedingTimeLimit { get; set; }

            [JsonProperty("seen_complete")]
            public Int64 SeenComplete { get; set; }

            [JsonProperty("seq_dl")]
            public Boolean SeqDl { get; set; }

            [JsonProperty("size")]
            public Int64 Size { get; set; }

            [JsonProperty("state")]
            public String State { get; set; }

            [JsonProperty("super_seeding")]
            public Boolean SuperSeeding { get; set; }

            [JsonProperty("tags")]
            public String Tags { get; set; }

            [JsonProperty("time_active")]
            public Int64 TimeActive { get; set; }

            [JsonProperty("total_size")]
            public Int64 TotalSize { get; set; }

            [JsonProperty("tracker")]
            public String Tracker { get; set; }

            [JsonProperty("up_limit")]
            public Int64 UpLimit { get; set; }

            [JsonProperty("uploaded")]
            public Int64 Uploaded { get; set; }

            [JsonProperty("uploaded_session")]
            public Int64 UploadedSession { get; set; }

            [JsonProperty("upspeed")]
            public Int64 Upspeed { get; set; }
        }
    }
}
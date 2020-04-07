using System;
using Newtonsoft.Json;

namespace RdtClient.Service.Models.QBittorrent
{
    public class TorrentProperties
    {
        [JsonProperty("addition_date")]
        public Int64 AdditionDate { get; set; }

        [JsonProperty("comment")]
        public String Comment { get; set; }

        [JsonProperty("completion_date")]
        public Int64 CompletionDate { get; set; }

        [JsonProperty("created_by")]
        public String CreatedBy { get; set; }

        [JsonProperty("creation_date")]
        public Int64 CreationDate { get; set; }

        [JsonProperty("dl_limit")]
        public Int64 DlLimit { get; set; }

        [JsonProperty("dl_speed")]
        public Int64 DlSpeed { get; set; }

        [JsonProperty("dl_speed_avg")]
        public Int64 DlSpeedAvg { get; set; }

        [JsonProperty("eta")]
        public Int64 Eta { get; set; }

        [JsonProperty("last_seen")]
        public Int64 LastSeen { get; set; }

        [JsonProperty("nb_connections")]
        public Int64 NbConnections { get; set; }

        [JsonProperty("nb_connections_limit")]
        public Int64 NbConnectionsLimit { get; set; }

        [JsonProperty("peers")]
        public Int64 Peers { get; set; }

        [JsonProperty("peers_total")]
        public Int64 PeersTotal { get; set; }

        [JsonProperty("piece_size")]
        public Int64 PieceSize { get; set; }

        [JsonProperty("pieces_have")]
        public Int64 PiecesHave { get; set; }

        [JsonProperty("pieces_num")]
        public Int64 PiecesNum { get; set; }

        [JsonProperty("reannounce")]
        public Int64 Reannounce { get; set; }

        [JsonProperty("save_path")]
        public String SavePath { get; set; }

        [JsonProperty("seeding_time")]
        public Int64 SeedingTime { get; set; }

        [JsonProperty("seeds")]
        public Int64 Seeds { get; set; }

        [JsonProperty("seeds_total")]
        public Int64 SeedsTotal { get; set; }

        [JsonProperty("share_ratio")]
        public Int64 ShareRatio { get; set; }

        [JsonProperty("time_elapsed")]
        public Int64 TimeElapsed { get; set; }

        [JsonProperty("total_downloaded")]
        public Int64 TotalDownloaded { get; set; }

        [JsonProperty("total_downloaded_session")]
        public Int64 TotalDownloadedSession { get; set; }

        [JsonProperty("total_size")]
        public Int64 TotalSize { get; set; }

        [JsonProperty("total_uploaded")]
        public Int64 TotalUploaded { get; set; }

        [JsonProperty("total_uploaded_session")]
        public Int64 TotalUploadedSession { get; set; }

        [JsonProperty("total_wasted")]
        public Int64 TotalWasted { get; set; }

        [JsonProperty("up_limit")]
        public Int64 UpLimit { get; set; }

        [JsonProperty("up_speed")]
        public Int64 UpSpeed { get; set; }

        [JsonProperty("up_speed_avg")]
        public Int64 UpSpeedAvg { get; set; }
    }
}
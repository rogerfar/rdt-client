using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RdtClient.Data.Enums;

namespace RdtClient.Data.Models.Data
{
    public class Download
    {
        [Key]
        public Guid DownloadId { get; set; }

        public Guid TorrentId { get; set; }

        public String Link { get; set; }

        public DateTimeOffset Added { get; set; }

        public DownloadStatus Status { get; set; }

        [ForeignKey("TorrentId")]
        public Torrent Torrent { get; set; }

        [NotMapped]
        public Int64 BytesSize { get; set; }

        [NotMapped]
        public Int64 BytesDownloaded { get; set; }

        [NotMapped]
        public Int64 Speed { get; set; }
    }
}

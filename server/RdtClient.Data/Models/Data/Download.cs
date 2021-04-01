using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RdtClient.Data.Models.Data
{
    public class Download
    {
        [Key]
        public Guid DownloadId { get; set; }

        public Guid TorrentId { get; set; }

        public String Path { get; set; }

        public String Link { get; set; }

        public DateTimeOffset Added { get; set; }
        
        public DateTimeOffset? DownloadQueued { get; set; }

        public DateTimeOffset? DownloadStarted { get; set; }

        public DateTimeOffset? DownloadFinished { get; set; }
        
        public DateTimeOffset? UnpackingQueued { get; set; }
        
        public DateTimeOffset? UnpackingStarted { get; set; }
        
        public DateTimeOffset? UnpackingFinished { get; set; }
        
        public DateTimeOffset? Completed { get; set; }

        public Int32 RetryCount { get; set; }
        
        public String Error { get; set; }

        [ForeignKey("TorrentId")]
        public Torrent Torrent { get; set; }

        [NotMapped]
        public Int64 BytesTotal { get; set; }

        [NotMapped]
        public Int64 BytesDone { get; set; }

        [NotMapped]
        public Int64 Speed { get; set; }
    }
}

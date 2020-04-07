using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using RDNET.Models;
using RdtClient.Data.Enums;

namespace RdtClient.Data.Models.Data
{
    public class Torrent
    {
        [Key]
        public Guid TorrentId { get; set; }

        public String Hash { get; set; }

        public String Category { get; set; }

        public TorrentStatus Status { get; set; }

        [InverseProperty("Torrent")]
        public IList<Download> Downloads { get; set; }

        public String RdId { get; set; }
        public String RdName { get; set; }
        public Int64 RdSize { get; set; }
        public String RdHost { get; set; }
        public Int64 RdSplit { get; set; }
        public Int64 RdProgress { get; set; }
        public String RdStatus { get; set; }
        public DateTimeOffset RdAdded { get; set; }
        public DateTimeOffset? RdEnded { get; set; }
        public Int64? RdSpeed { get; set; }
        public Int64? RdSeeders { get; set; }
        public String RdFiles { get; set; }

        [NotMapped]
        public IList<TorrentFile> Files
        {
            get
            {
                if (String.IsNullOrWhiteSpace(RdFiles))
                {
                    return new List<TorrentFile>();
                }

                try
                {
                    return JsonConvert.DeserializeObject<List<TorrentFile>>(RdFiles);
                }
                catch
                {
                    return new List<TorrentFile>();
                }
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using RDNET;
using RdtClient.Data.Enums;

namespace RdtClient.Data.Models.Data
{
    public class Torrent
    {
        [Key]
        public Guid TorrentId { get; set; }

        public String Hash { get; set; }

        public String Category { get; set; }
        
        public TorrentDownloadAction DownloadAction { get; set; }
        public TorrentFinishedAction FinishedAction { get; set; }
        public Int32 DownloadMinSize { get; set; }
        public String DownloadManualFiles { get; set; }

        public DateTimeOffset Added { get; set; }
        public DateTimeOffset? FilesSelected { get; set; }
        public DateTimeOffset? Completed { get; set; }

        public String FileOrMagnet { get; set; }
        public Boolean IsFile { get; set; }
        
        [InverseProperty("Torrent")]
        public IList<Download> Downloads { get; set; }

        public String RdId { get; set; }
        public String RdName { get; set; }
        public Int64 RdSize { get; set; }
        public String RdHost { get; set; }
        public Int64 RdSplit { get; set; }
        public Int64 RdProgress { get; set; }
        public RealDebridStatus RdStatus { get; set; }
        public String RdStatusRaw { get; set; }
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

        [NotMapped]
        public IList<String> ManualFiles
        {
            get
            {
                if (String.IsNullOrWhiteSpace(DownloadManualFiles))
                {
                    return new List<String>();
                }

                return DownloadManualFiles.Split(",");
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace RdtClient.Service.Models.TorrentClient
{
    public class TorrentClientTorrent
    {
        public String Id { get; set; }
        public String Filename { get; set; }
        public String OriginalFilename { get; set; }
        public String Hash { get; set; }
        public Int64 Bytes { get; set; }
        public Int64 OriginalBytes { get; set; }
        public String Host { get; set; }
        public Int64 Split { get; set; }
        public Int64 Progress { get; set; }
        public String Status { get; set; }
        public DateTimeOffset Added { get; set; }
        public List<TorrentClientTorrentFile> Files { get; set; }
        public List<String> Links { get; set; }
        public DateTimeOffset? Ended { get; set; }
        public Int64? Speed { get; set; }
        public Int64? Seeders { get; set; }
    }

    public class TorrentClientTorrentFile
    {
        public Int64 Id { get; set; }
        public String Path { get; set; }
        public Int64 Bytes { get; set; }
        public Boolean Selected { get; set; }
    }
}

﻿using System;

namespace RdtClient.Data.Models.Internal
{
    public class DbSettings
    {
        public String RealDebridApiKey { get; set; }
        public String DownloadPath { get; set; }
        public String DownloadClient { get; set; }
        public String TempPath { get; set; }
        public String MappedPath { get; set; }
        public Int32 DownloadLimit { get; set; }
        public Int32 UnpackLimit { get; set; }
        public Int32 MinFileSize { get; set; }
        public Int32 OnlyDownloadAvailableFiles { get; set; }
        public Int32 DownloadChunkCount { get; set; }
        public Int32 DownloadMaxSpeed { get; set; }
        public String ProxyServer { get; set; }
        public String LogLevel { get; set; }
        public String Categories { get; set; }
    }
}

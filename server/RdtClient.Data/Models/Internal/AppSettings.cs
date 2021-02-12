using System;

namespace RdtClient.Data.Models.Internal
{
    public class AppSettings
    {
        public AppSettingsLogging Logging { get; set; }
        public AppSettingsDatabase Database { get; set; }
        
        public String AllowedHosts { get; set; }
        public String HostUrl { get; set; }
    }

    public class AppSettingsLogging
    {
        public AppSettingsLoggingFile File { get; set; }
    }
    
    public class AppSettingsLoggingFile
    {
        public String Path { get; set; }
        public String Append { get; set; }
        public Int64 FileSizeLimitBytes { get; set; }
        public Int32 MaxRollingFiles { get; set; }
    }

    public class AppSettingsDatabase
    {
        public String Path { get; set; }
    }
}

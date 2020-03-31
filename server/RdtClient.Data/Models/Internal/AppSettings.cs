using System;

namespace RdtClient.Data.Models.Internal
{
    public class AppSettings
    {
        public AppSettingsConnectionStrings ConnectionStrings { get; set; }
    }

    public class AppSettingsConnectionStrings
    {
        public String Client { get; set; }
    }
}

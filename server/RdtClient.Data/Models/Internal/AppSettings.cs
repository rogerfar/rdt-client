namespace RdtClient.Data.Models.Internal;

public class AppSettings
{
    public AppSettingsLogging? Logging { get; set; }
    public AppSettingsDatabase? Database { get; set; }
    public AppSettingsSecurity? Security { get; set; }

    public Int32 Port { get; set; }
    public String? BasePath { get; set; }
}

public class AppSettingsSecurity
{
    /// <summary>
    ///     Value for the X-Frame-Options header, for example "SAMEORIGIN" or "DENY". Left empty by default because many
    ///     users embed the UI in a dashboard such as Organizr or Heimdall, which the header would break.
    /// </summary>
    public String? FrameOptions { get; set; }

    /// <summary>
    ///     Redirect plain HTTP to HTTPS. Off by default: the app is usually reached over HTTP behind a reverse proxy that
    ///     terminates TLS itself, and enabling this there causes a redirect loop.
    /// </summary>
    public Boolean UseHttpsRedirection { get; set; }

    /// <summary>
    ///     Send the Strict-Transport-Security header. Only enable when the app is served directly over HTTPS.
    /// </summary>
    public Boolean UseHsts { get; set; }
}

public class AppSettingsLogging
{
    public AppSettingsLoggingFile? File { get; set; }
}

public class AppSettingsLoggingFile
{
    public String? Path { get; set; }
    public Int64 FileSizeLimitBytes { get; set; }
    public Int32 MaxRollingFiles { get; set; }
}

public class AppSettingsDatabase
{
    public String? Path { get; set; }
}

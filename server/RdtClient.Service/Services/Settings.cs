using System.Diagnostics;
using Aria2NET;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services.Downloaders;
using Serilog.Core;
using Serilog.Events;

namespace RdtClient.Service.Services;

public class Settings
{
    public static readonly LoggingLevelSwitch LoggingLevelSwitch = new(LogEventLevel.Debug);

    private readonly SettingData _settingData;

    public Settings(SettingData settingData)
    {
        _settingData = settingData;
    }

    public static DbSettings Get => SettingData.Get;

    public IList<SettingProperty> GetAll()
    {
        return _settingData.GetAll();
    }

    public async Task Update(IList<SettingProperty> settings)
    {
        await _settingData.Update(settings);
    }

    public async Task Update(String settingId, Object value)
    {
        await _settingData.Update(settingId, value);
    }

    public async Task TestPath(String path)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            throw new Exception("Path is not set");
        }

        path = path.TrimEnd('/').TrimEnd('\\');

        if (!Directory.Exists(path))
        {
            throw new Exception($"Path {path} does not exist");
        }

        var testFile = $"{path}/test.txt";

        await File.WriteAllTextAsync(testFile, "RealDebridClient Test File, you can remove this file.");
            
        await FileHelper.Delete(testFile);
    }

    public async Task<Double> TestDownloadSpeed(CancellationToken cancellationToken)
    {
        var downloadPath = Get.DownloadClient.DownloadPath;

        var testFilePath = Path.Combine(downloadPath, "testDefault.rar");

        await FileHelper.Delete(testFilePath);

        var download = new Download
        {
            Link = "https://34.download.real-debrid.com/speedtest/testDefault.rar",
            Torrent = new Torrent
            {
                RdName = ""
            }
        };

        var downloadClient = new DownloadClient(download, download.Torrent, downloadPath);

        await downloadClient.Start(Get);

        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        while (!downloadClient.Finished)
        {
            await Task.Delay(1000, CancellationToken.None);

            if (cancellationToken.IsCancellationRequested)
            {
                await downloadClient.Cancel();
            }

            if (downloadClient.Downloader is Aria2cDownloader aria2Downloader)
            {
                var aria2NetClient = new Aria2NetClient(Settings.Get.DownloadClient.Aria2cUrl, Settings.Get.DownloadClient.Aria2cSecret, httpClient, 1);

                var allDownloads = await aria2NetClient.TellAll(cancellationToken);

                await aria2Downloader.Update(allDownloads);
            }
            
            if (downloadClient.BytesDone > 1024 * 1024 * 50)
            {
                await downloadClient.Cancel();

                break;
            }
        }

        await FileHelper.Delete(testFilePath);

        await Clean();

        return downloadClient.Speed;
    }

    public async Task<Double> TestWriteSpeed()
    {
        var downloadPath = Get.DownloadClient.DownloadPath;

        var testFilePath = Path.Combine(downloadPath, "test.tmp");

        await FileHelper.Delete(testFilePath);

        const Int32 testFileSize = 64 * 1024 * 1024;

        var watch = new Stopwatch();

        watch.Start();

        var rnd = new Random();

        await using var fileStream = new FileStream(testFilePath, FileMode.Create, FileAccess.Write, FileShare.Write);

        var buffer = new Byte[64 * 1024];

        while (fileStream.Length < testFileSize)
        {
            rnd.NextBytes(buffer);

            await fileStream.WriteAsync(buffer.AsMemory(0, buffer.Length));
        }
            
        watch.Stop();

        var writeSpeed = fileStream.Length / watch.Elapsed.TotalSeconds;
            
        fileStream.Close();

        await FileHelper.Delete(testFilePath);

        return writeSpeed;
    }

    public async Task<VersionResult> GetAria2cVersion(String url, String secret)
    {
        var client = new Aria2NetClient(url, secret);

        return await client.GetVersion();
    }

    public async Task Clean()
    {
        try
        {
            var tempPath = Get.DownloadClient.TempPath;

            if (!String.IsNullOrWhiteSpace(tempPath))
            {
                var files = Directory.GetFiles(tempPath, "*.dsc", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    await FileHelper.Delete(file);
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    public async Task Seed()
    {
        await _settingData.Seed();
    }

    public async Task ResetCache()
    {
        await _settingData.ResetCache();

        LoggingLevelSwitch.MinimumLevel = Settings.Get.General.LogLevel switch
        {
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Warning
        };
    }
}
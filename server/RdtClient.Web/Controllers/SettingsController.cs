using System.Diagnostics;
using Aria2NET;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;
using RdtClient.Service.Services.Downloaders;

namespace RdtClient.Web.Controllers;

[Authorize(Policy = "AuthSetting")]
[Route("Api/Settings")]
public class SettingsController(Settings settings, Torrents torrents) : Controller
{
    [HttpGet]
    [Route("")]
    public ActionResult Get()
    {
        var result = SettingData.GetAll();
        return Ok(result);
    }

    [HttpPut]
    [Route("")]
    public async Task<ActionResult> Update([FromBody] IList<SettingProperty>? settings1)
    {
        if (settings1 == null)
        {
            return BadRequest();
        }

        await settings.Update(settings1);
        
        return Ok();
    }

    [HttpGet]
    [Route("Profile")]
    public async Task<ActionResult<Profile>> Profile()
    {
        var profile = await torrents.GetProfile();
        return Ok(profile);
    }
        
    [HttpPost]
    [Route("TestPath")]
    public async Task<ActionResult> TestPath([FromBody] SettingsControllerTestPathRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.Path))
        {
            return BadRequest("Invalid path");
        }

        var path = request.Path.TrimEnd('/').TrimEnd('\\');

        if (!Directory.Exists(path))
        {
            throw new($"Path {path} does not exist");
        }

        var testFile = $"{path}/test.txt";

        await System.IO.File.WriteAllTextAsync(testFile, "RealDebridClient Test File, you can remove this file.");
            
        await FileHelper.Delete(testFile);

        return Ok();
    }
        
    [HttpGet]
    [Route("TestDownloadSpeed")]
    public async Task<ActionResult> TestDownloadSpeed(CancellationToken cancellationToken)
    {
        var downloadPath = Settings.Get.DownloadClient.DownloadPath;

        var testFilePath = Path.Combine(downloadPath, "testDefault.rar");

        await FileHelper.Delete(testFilePath);

        var download = new Download
        {
            Link = "https://34.download.real-debrid.com/speedtest/testDefault.rar",
            Torrent = new()
            {
                DownloadClient = Settings.Get.DownloadClient.Client == Data.Enums.DownloadClient.Symlink ? Data.Enums.DownloadClient.Internal : Settings.Get.DownloadClient.Client,
                RdName = "testDefault.rar"
            }
        };

        var downloadClient = new DownloadClient(download, download.Torrent, downloadPath, null);

        await downloadClient.Start();

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

                var allDownloads = await aria2NetClient.TellAllAsync(cancellationToken);

                await aria2Downloader.Update(allDownloads);
            }
            
            if (downloadClient.BytesDone > 1024 * 1024 * 50)
            {
                await downloadClient.Cancel();

                break;
            }
        }

        await FileHelper.Delete(testFilePath);

        return Ok(downloadClient.Speed);
    }
        
    [HttpGet]
    [Route("TestWriteSpeed")]
    public async Task<ActionResult> TestWriteSpeed()
    {
        var downloadPath = Settings.Get.DownloadClient.DownloadPath;

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
        
        return Ok(writeSpeed);
    }

    [HttpPost]
    [Route("TestAria2cConnection")]
    public async Task<ActionResult<String>> TestAria2cConnection([FromBody] SettingsControllerTestAria2cConnectionRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.Url))
        {
            return BadRequest("Invalid Url");
        }

        var client = new Aria2NetClient(request.Url, request.Secret);

        var version = await client.GetVersionAsync();

        return Ok(version);
    }
}

public class SettingsControllerTestPathRequest
{
    public String? Path { get; set; }
}

public class SettingsControllerTestAria2cConnectionRequest
{
    public String? Url { get; set; }
    public String? Secret { get; set; }
}
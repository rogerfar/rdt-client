using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
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

/// <summary>
/// Controller for managing application settings and performing system tests
/// </summary>
[Authorize(Policy = "AuthSetting")]
[Route("Api/Settings")]
public class SettingsController(Settings settings, Torrents torrents) : Controller
{
    /// <summary>
    /// Retrieves all application settings
    /// </summary>
    /// <returns>A collection of all configured settings</returns>
    /// <response code="200">Returns the list of settings</response>
    [HttpGet]
    [Route("")]
    [ProducesResponseType(typeof(IEnumerable<SettingProperty>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<SettingProperty>> Get()
    {
        var result = SettingData.GetAll();
        return Ok(result);
    }

    /// <summary>
    /// Updates multiple application settings
    /// </summary>
    /// <param name="settings1">List of setting properties to update</param>
    /// <returns>Success status</returns>
    /// <response code="200">Settings were successfully updated</response>
    /// <response code="400">Invalid settings data provided</response>
    [HttpPut]
    [Route("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Update([FromBody] IList<SettingKeyValuePair>? settings1)
    {
        if (settings1 == null)
        {
            return BadRequest();
        }

        await settings.Update(settings1);

        return Ok();
    }

    /// <summary>
    /// Retrieves the profile information from the currently configured debrid service
    /// </summary>
    /// <returns>The profile information</returns>
    /// <response code="200">The profile information</response>
    [HttpGet]
    [Route("Profile")]
    [ProducesResponseType(typeof(Profile), StatusCodes.Status200OK)]
    public async Task<ActionResult<Profile>> Profile()
    {
        var profile = await torrents.GetProfile();
        return Ok(profile);
    }

    /// <summary>
    /// Gets the version of rdt-client the server is running
    /// </summary>
    /// <returns>The Version Number</returns>
    [HttpGet]
    [Route("Version")]
    public ActionResult<Version> Version()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        return Ok(new SettingsControllerVersionResponse
        {
            Version = version.ToString()
        });
    }


    /// <summary>
    /// Tests if a specified path is writable by attempting to create and delete a test file
    /// </summary>
    /// <remarks>
    /// Creates a test file in the specified directory to verify write permissions.
    /// The test file is automatically deleted after the test completes.
    /// </remarks>
    /// <param name="request">The path testing request containing the directory to test</param>
    /// <returns>Success status if the path is writable</returns>
    /// <response code="200">The path is valid and writable</response>
    /// <response code="400">Invalid or empty path provided</response>
    /// <response code="500">Path does not exist or is not accessible</response>
    [HttpPost]
    [Route("TestPath")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(String), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(String), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Tests download speed by downloading a sample file and measuring throughput
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The measured download speed in bytes per second</returns>
    /// <response code="200">Returns the measured download speed</response>
    /// <remarks>
    /// The test downloads a file up to 50MB and measures the download speed.
    /// </remarks>
    [HttpGet]
    [Route("TestDownloadSpeed")]
    [ProducesResponseType(typeof(Int64), StatusCodes.Status200OK)]
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
        // ReSharper disable once SuggestVarOrType_BuiltInTypes
        return Ok(downloadClient.Speed);
    }

    /// <summary>
    /// Tests write speed to the configured download directory
    /// </summary>
    /// <returns>The measured write speed in bytes per second</returns>
    /// <response code="200">Returns the measured write speed</response>
    /// <remarks>
    /// Creates a 64MB test file with random data to measure disk write performance.
    /// The test file is automatically deleted after the test completes.
    /// </remarks>
    [HttpGet]
    [Route("TestWriteSpeed")]
    [ProducesResponseType(typeof(Double), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Tests the connection to an Aria2c instance
    /// </summary>
    /// <remarks>
    /// Attempts to connect to an Aria2c RPC endpoint and retrieve its version information.
    /// This verifies both connectivity and authentication with the Aria2c server.
    /// </remarks>
    /// <param name="request">The connection details for the Aria2c instance</param>
    /// <returns>The version information of the Aria2c server if connection is successful</returns>
    /// <response code="200">Returns the Aria2c version information</response>
    /// <response code="400">Invalid or missing connection details</response>
    /// <response code="500">Connection to Aria2c failed</response>
    [HttpPost]
    [Route("TestAria2cConnection")]
    [ProducesResponseType(typeof(String), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

/// <summary>
/// Request model for testing path accessibility
/// </summary>
public class SettingsControllerTestPathRequest
{
    /// <summary>
    /// The directory path to test for write access
    /// </summary>
    /// <example>/path/to/downloads</example>
    [Required] 
    public String? Path { get; set; }
}

/// <summary>
/// Request model for testing Aria2c connection
/// </summary>
public class SettingsControllerTestAria2cConnectionRequest
{
    /// <summary>
    /// The URL of the Aria2c RPC endpoint
    /// </summary>
 
    /// <example>http://localhost:6800/jsonrpc</example>
    [Required] 
    public String? Url { get; set; }

    /// <summary>
    /// The secret token for authenticating with the Aria2c server
    /// </summary>
    /// <example>your-secret-token</example>
    [Required] 
    public String? Secret { get; set; }
}

/// <summary>
/// Response model for the version information
/// </summary>
public class SettingsControllerVersionResponse
{
    /// <summary>
    /// The version number
    /// </summary>
    /// <example>v2.0.102</example>
    public required String? Version { get; set; }
}
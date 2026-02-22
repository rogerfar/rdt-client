using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonoTorrent;
using RdtClient.Data.Models.DebridClient;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.BackgroundServices;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Web.Controllers;

[Authorize(Policy = "AuthSetting")]
[Route("Api/Torrents")]
public class TorrentsController(ILogger<TorrentsController> logger, Torrents torrents, TorrentRunner torrentRunner) : Controller
{
    [HttpGet]
    [Route("")]
    public async Task<ActionResult<IList<TorrentDto>>> GetAll()
    {
        var results = await torrents.Get();

        var torrentDtos = results.Select(torrent => new TorrentDto
        {
            TorrentId = torrent.TorrentId,
            Hash = torrent.Hash,
            Category = torrent.Category,
            DownloadAction = torrent.DownloadAction,
            FinishedAction = torrent.FinishedAction,
            FinishedActionDelay = torrent.FinishedActionDelay,
            HostDownloadAction = torrent.HostDownloadAction,
            DownloadMinSize = torrent.DownloadMinSize,
            IncludeRegex = torrent.IncludeRegex,
            ExcludeRegex = torrent.ExcludeRegex,
            DownloadManualFiles = torrent.DownloadManualFiles,
            DownloadClient = torrent.DownloadClient,
            Added = torrent.Added,
            FilesSelected = torrent.FilesSelected,
            Completed = torrent.Completed,
            Type = torrent.Type,
            IsFile = torrent.IsFile,
            Priority = torrent.Priority,
            RetryCount = torrent.RetryCount,
            DownloadRetryAttempts = torrent.DownloadRetryAttempts,
            TorrentRetryAttempts = torrent.TorrentRetryAttempts,
            DeleteOnError = torrent.DeleteOnError,
            Lifetime = torrent.Lifetime,
            Error = torrent.Error,
            RdId = torrent.RdId,
            RdName = torrent.RdName,
            RdSize = torrent.RdSize,
            RdHost = torrent.RdHost,
            RdSplit = torrent.RdSplit,
            RdProgress = torrent.RdProgress,
            RdStatus = torrent.RdStatus,
            RdStatusRaw = torrent.RdStatusRaw,
            RdAdded = torrent.RdAdded,
            RdEnded = torrent.RdEnded,
            RdSpeed = torrent.RdSpeed,
            RdSeeders = torrent.RdSeeders,
            Files = torrent.Files,
            Downloads = torrent.Downloads.Select(download =>
            {
                var (speed, bytesTotal, bytesDone) = torrents.GetDownloadStats(download.DownloadId);

                return new DownloadDto
                {
                    DownloadId = download.DownloadId,
                    TorrentId = download.TorrentId,
                    Path = download.Path,
                    Link = download.Link,
                    Added = download.Added,
                    DownloadQueued = download.DownloadQueued,
                    DownloadStarted = download.DownloadStarted,
                    DownloadFinished = download.DownloadFinished,
                    UnpackingQueued = download.UnpackingQueued,
                    UnpackingStarted = download.UnpackingStarted,
                    UnpackingFinished = download.UnpackingFinished,
                    Completed = download.Completed,
                    RetryCount = download.RetryCount,
                    Error = download.Error,
                    BytesTotal = bytesTotal,
                    BytesDone = bytesDone,
                    Speed = speed
                };
            }).ToList()
        }).ToList();

        return Ok(torrentDtos);
    }

    [HttpGet]
    [Route("Get/{torrentId:guid}")]
    public async Task<ActionResult<TorrentDto>> GetById(Guid torrentId)
    {
        var torrent = await torrents.GetById(torrentId);

        if (torrent == null)
        {
            return NotFound();
        }

        foreach (var file in torrent.Downloads)
        {
            file.Torrent = null;
        }

        var torrentDto = new TorrentDto
        {
            TorrentId = torrent!.TorrentId,
            Hash = torrent.Hash,
            Category = torrent.Category,
            DownloadAction = torrent.DownloadAction,
            FinishedAction = torrent.FinishedAction,
            FinishedActionDelay = torrent.FinishedActionDelay,
            HostDownloadAction = torrent.HostDownloadAction,
            DownloadMinSize = torrent.DownloadMinSize,
            IncludeRegex = torrent.IncludeRegex,
            ExcludeRegex = torrent.ExcludeRegex,
            DownloadManualFiles = torrent.DownloadManualFiles,
            DownloadClient = torrent.DownloadClient,
            Added = torrent.Added,
            FilesSelected = torrent.FilesSelected,
            Completed = torrent.Completed,
            Type = torrent.Type,
            IsFile = torrent.IsFile,
            Priority = torrent.Priority,
            RetryCount = torrent.RetryCount,
            DownloadRetryAttempts = torrent.DownloadRetryAttempts,
            TorrentRetryAttempts = torrent.TorrentRetryAttempts,
            DeleteOnError = torrent.DeleteOnError,
            Lifetime = torrent.Lifetime,
            Error = torrent.Error,
            RdId = torrent.RdId,
            RdName = torrent.RdName,
            RdSize = torrent.RdSize,
            RdHost = torrent.RdHost,
            RdSplit = torrent.RdSplit,
            RdProgress = torrent.RdProgress,
            RdStatus = torrent.RdStatus,
            RdStatusRaw = torrent.RdStatusRaw,
            RdAdded = torrent.RdAdded,
            RdEnded = torrent.RdEnded,
            RdSpeed = torrent.RdSpeed,
            RdSeeders = torrent.RdSeeders,
            Files = torrent.Files,
            Downloads = torrent.Downloads.Select(download =>
            {
                var (speed, bytesTotal, bytesDone) = torrents.GetDownloadStats(download.DownloadId);

                return new DownloadDto
                {
                    DownloadId = download.DownloadId,
                    TorrentId = download.TorrentId,
                    Path = download.Path,
                    Link = download.Link,
                    Added = download.Added,
                    DownloadQueued = download.DownloadQueued,
                    DownloadStarted = download.DownloadStarted,
                    DownloadFinished = download.DownloadFinished,
                    UnpackingQueued = download.UnpackingQueued,
                    UnpackingStarted = download.UnpackingStarted,
                    UnpackingFinished = download.UnpackingFinished,
                    Completed = download.Completed,
                    RetryCount = download.RetryCount,
                    Error = download.Error,
                    BytesTotal = bytesTotal,
                    BytesDone = bytesDone,
                    Speed = speed
                };
            }).ToList()
        };

        return Ok(torrentDto);
    }

    [HttpGet]
    [Route("DiskSpaceStatus")]
    public ActionResult<DiskSpaceStatus?> GetDiskSpaceStatus()
    {
        var status = DiskSpaceMonitor.GetCurrentStatus();
        return Ok(status);
    }

    [HttpGet]
    [Route("RateLimitStatus")]
    public ActionResult<RateLimitStatus> GetRateLimitStatus()
    {
        var nextDequeueTime = TorrentRunner.NextDequeueTime;

        if (nextDequeueTime < DateTimeOffset.Now)
        {
            return Ok(new RateLimitStatus
            {
                NextDequeueTime = null,
                SecondsRemaining = 0
            });
        }

        return Ok(new RateLimitStatus
        {
            NextDequeueTime = nextDequeueTime,
            SecondsRemaining = (nextDequeueTime - DateTimeOffset.Now).TotalSeconds
        });
    }

    /// <summary>
    ///     Used for debugging only. Force a tick.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("Tick")]
    public async Task<ActionResult> Tick()
    {
        await torrentRunner.Tick();

        return Ok();
    }

    [HttpPost]
    [Route("UploadFile")]
    public async Task<ActionResult> UploadFile([FromForm] IFormFile? file,
                                               [ModelBinder(BinderType = typeof(JsonModelBinder))]
                                               TorrentControllerUploadFileRequest? formData)
    {
        if (file == null || file.Length <= 0)
        {
            return BadRequest("Invalid torrent file");
        }

        if (formData?.Torrent == null)
        {
            return BadRequest("Invalid Torrent");
        }

        logger.LogDebug($"Add file");

        var fileStream = file.OpenReadStream();

        await using var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();

        await torrents.AddFileToDebridQueue(bytes, formData.Torrent);

        return Ok();
    }

    [HttpPost]
    [Route("UploadMagnet")]
    public async Task<ActionResult> UploadMagnet([FromBody] TorrentControllerUploadMagnetRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.MagnetLink))
        {
            return BadRequest("Invalid magnet link");
        }

        if (request.Torrent == null)
        {
            return BadRequest("Invalid Torrent");
        }

        logger.LogDebug($"Add magnet");

        await torrents.AddMagnetToDebridQueue(request.MagnetLink, request.Torrent);

        return Ok();
    }

    [HttpPost]
    [Route("UploadNzbFile")]
    public async Task<ActionResult> UploadNzbFile([FromForm] IFormFile? file,
                                                  [ModelBinder(BinderType = typeof(JsonModelBinder))]
                                                  TorrentControllerUploadFileRequest? formData)
    {
        if (file == null || file.Length <= 0)
        {
            return BadRequest("Invalid nzb file");
        }

        if (formData?.Torrent == null)
        {
            return BadRequest("Invalid Torrent");
        }

        logger.LogDebug($"Add nzb file");

        if (String.IsNullOrWhiteSpace(formData.Torrent.RdName))
        {
            formData.Torrent.RdName = file.FileName;
        }

        var fileStream = file.OpenReadStream();

        await using var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();

        await torrents.AddNzbFileToDebridQueue(bytes, file.FileName, formData.Torrent);

        return Ok();
    }

    [HttpPost]
    [Route("UploadNzbLink")]
    public async Task<ActionResult> UploadNzbLink([FromBody] TorrentControllerUploadNzbLinkRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.NzbLink))
        {
            return BadRequest("Invalid nzb link");
        }

        if (request.Torrent == null)
        {
            return BadRequest("Invalid Torrent");
        }

        logger.LogDebug($"Add nzb link {request.NzbLink}");

        await torrents.AddNzbLinkToDebridQueue(request.NzbLink, request.Torrent);

        return Ok();
    }

    [HttpPost]
    [Route("CheckFiles")]
    public async Task<ActionResult> CheckFiles([FromForm] IFormFile? file)
    {
        if (file == null || file.Length <= 0)
        {
            return BadRequest("Invalid torrent file");
        }

        var fileStream = file.OpenReadStream();

        await using var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();

        var torrent = await MonoTorrent.Torrent.LoadAsync(bytes);

        var result = await torrents.GetAvailableFiles(torrent.InfoHashes.V1OrV2.ToHex());

        return Ok(result);
    }

    [HttpPost]
    [Route("CheckFilesMagnet")]
    public async Task<ActionResult> CheckFilesMagnet([FromBody] TorrentControllerCheckFilesRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.MagnetLink))
        {
            return BadRequest("MagnetLink cannot be null or empty");
        }

        var magnet = MagnetLink.Parse(request.MagnetLink);

        var result = await torrents.GetAvailableFiles(magnet.InfoHashes.V1OrV2.ToHex());

        return Ok(result);
    }

    [HttpPost]
    [Route("Delete/{torrentId:guid}")]
    public async Task<ActionResult> Delete(Guid torrentId, [FromBody] TorrentControllerDeleteRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        logger.LogDebug("Delete {torrentId}", torrentId);

        await torrents.Delete(torrentId, request.DeleteData, request.DeleteRdTorrent, request.DeleteLocalFiles);

        return Ok();
    }

    [HttpPost]
    [Route("Retry/{torrentId:guid}")]
    public async Task<ActionResult> Retry(Guid torrentId)
    {
        logger.LogDebug("Retry {torrentId}", torrentId);

        await torrents.UpdateRetry(torrentId, DateTimeOffset.UtcNow, 0);
        await torrents.RetryTorrent(torrentId, 0);

        return Ok();
    }

    [HttpPost]
    [Route("RetryDownload/{downloadId:guid}")]
    public async Task<ActionResult> RetryDownload(Guid downloadId)
    {
        logger.LogDebug("Retry download {downloadId}", downloadId);

        await torrents.RetryDownload(downloadId);

        return Ok();
    }

    [HttpPut]
    [Route("Update")]
    public async Task<ActionResult> Update([FromBody] Torrent? torrent)
    {
        if (torrent == null)
        {
            return BadRequest();
        }

        await torrents.Update(torrent);

        return Ok();
    }

    [HttpPost]
    [Route("VerifyRegex")]
    public async Task<ActionResult> VerifyRegex([FromForm] IFormFile? file, [FromBody] TorrentControllerVerifyRegexRequest? request)
    {
        if (request == null)
        {
            return Ok();
        }

        var includeError = "";
        var excludeError = "";

        IList<DebridClientAvailableFile> availableFiles;

        if (!String.IsNullOrWhiteSpace(request.MagnetLink))
        {
            var magnet = MagnetLink.Parse(request.MagnetLink);

            availableFiles = await torrents.GetAvailableFiles(magnet.InfoHashes.V1OrV2.ToHex());
        }
        else if (file != null)
        {
            var fileStream = file.OpenReadStream();

            await using var memoryStream = new MemoryStream();

            await fileStream.CopyToAsync(memoryStream);

            var bytes = memoryStream.ToArray();

            var torrent = await MonoTorrent.Torrent.LoadAsync(bytes);

            availableFiles = await torrents.GetAvailableFiles(torrent.InfoHashes.V1OrV2.ToHex());
        }
        else
        {
            return BadRequest();
        }

        var selectedFiles = new List<DebridClientAvailableFile>();

        if (!String.IsNullOrWhiteSpace(request.IncludeRegex))
        {
            foreach (var availableFile in availableFiles)
            {
                try
                {
                    if (Regex.IsMatch(availableFile.Filename, request.IncludeRegex))
                    {
                        selectedFiles.Add(availableFile);
                    }
                }
                catch (Exception ex)
                {
                    includeError = ex.Message;
                }
            }
        }
        else if (!String.IsNullOrWhiteSpace(request.ExcludeRegex))
        {
            foreach (var availableFile in availableFiles)
            {
                try
                {
                    if (!Regex.IsMatch(availableFile.Filename, request.ExcludeRegex))
                    {
                        selectedFiles.Add(availableFile);
                    }
                }
                catch (Exception ex)
                {
                    excludeError = ex.Message;
                }
            }
        }
        else
        {
            selectedFiles = [.. availableFiles];
        }

        return Ok(new
        {
            includeError,
            excludeError,
            selectedFiles
        });
    }
}

public class TorrentControllerUploadFileRequest
{
    public Torrent? Torrent { get; set; }
}

public class TorrentControllerUploadMagnetRequest
{
    public String? MagnetLink { get; set; }
    public Torrent? Torrent { get; set; }
}

public class TorrentControllerUploadNzbLinkRequest
{
    public String? NzbLink { get; set; }
    public Torrent? Torrent { get; set; }
}

public class TorrentControllerDeleteRequest
{
    public Boolean DeleteData { get; set; }
    public Boolean DeleteRdTorrent { get; set; }
    public Boolean DeleteLocalFiles { get; set; }
}

public class TorrentControllerCheckFilesRequest
{
    public String? MagnetLink { get; set; }
}

public class TorrentControllerVerifyRegexRequest
{
    public String? IncludeRegex { get; set; }
    public String? ExcludeRegex { get; set; }
    public String? MagnetLink { get; set; }
}

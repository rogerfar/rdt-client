using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonoTorrent;
using RdtClient.Data.Models.TorrentClient;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;
using Torrent = RdtClient.Data.Models.Data.Torrent;
using System.Text.Json.Serialization;
using NSwag.Annotations;

namespace RdtClient.Web.Controllers;

/// <summary>
/// Controller for managing torrents and their downloads
/// </summary>
[Authorize(Policy = "AuthSetting")]
[Route("Api/Torrents")]
public class TorrentsController(ILogger<TorrentsController> logger, Torrents torrents, TorrentRunner torrentRunner) : Controller
{
    /// <summary>
    /// Retrieves all torrents and their associated downloads
    /// </summary>
    /// <returns>List of all torrents with their download status</returns>
    /// <response code="200">Returns the list of torrents</response>
    [HttpGet]
    [Route("")]
    [ProducesResponseType(typeof(IList<Torrent>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IList<Torrent>>> GetAll()
    {
        var results = await torrents.Get();

        // Prevent infinite recursion when serializing
        foreach (var file in results.SelectMany(torrent => torrent.Downloads))
        {
            file.Torrent = null;
        }

        return Ok(results);
    }

    /// <summary>
    /// Retrieves a specific torrent by its ID
    /// </summary>
    /// <param name="torrentId">The unique identifier of the torrent</param>
    /// <returns>The requested torrent details</returns>
    /// <response code="200">Returns the requested torrent</response>
    /// <response code="404">Torrent not found</response>
    [HttpGet]
    [Route("Get/{torrentId:guid}")]
    [ProducesResponseType(typeof(Torrent), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Torrent>> GetById(Guid torrentId)
    {
        var torrent = await torrents.GetById(torrentId);

        if (torrent?.Downloads != null)
        {
            foreach (var file in torrent.Downloads)
            {
                file.Torrent = null;
            }
        }

        return Ok(torrent);
    }

    /// <summary>
    /// Forces an immediate processing cycle for debugging purposes
    /// </summary>
    /// <returns>Success status</returns>
    /// <response code="200">Processing cycle completed successfully</response>
    [HttpGet]
    [Route("Tick")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Tick()
    {
        await torrentRunner.Tick();

        return Ok();
    }

    /// <summary>
    /// Adds a new torrent file with configuration
    /// </summary>
    /// <param name="file">The .torrent file to add</param>
    /// <param name="formData">Configuration for the torrent download</param>
    /// <returns>Success status</returns>
    /// <response code="200">Torrent added successfully</response>
    /// <response code="400">Invalid file or configuration provided</response>
    [HttpPost]
    [Route("UploadFile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(String), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UploadFile([OpenApiFile] IFormFile? file,
                                               [ModelBinder(BinderType = typeof(JsonModelBinder))]
                                               [FromForm] TorrentControllerUploadFileRequest? formData)
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

    /// <summary>
    /// Adds a new torrent using a magnet link
    /// </summary>
    /// <param name="request">The magnet link and torrent configuration</param>
    /// <returns>Success status</returns>
    /// <response code="200">Magnet link processed successfully</response>
    /// <response code="400">Invalid magnet link or configuration</response>
    [HttpPost]
    [Route("UploadMagnet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(String), StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// Checks available files in a torrent file
    /// </summary>
    /// <param name="file">The .torrent file to analyze</param>
    /// <returns>List of available files in the torrent</returns>
    /// <response code="200">Returns the list of available files</response>
    /// <response code="400">Invalid torrent file provided</response>
    [HttpPost]
    [Route("CheckFiles")]
    [ProducesResponseType(typeof(IList<TorrentClientAvailableFile>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(String), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IList<TorrentClientAvailableFile>>> CheckFiles([FromForm] IFormFile? file)
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

    /// <summary>
    /// Checks available files from a magnet link
    /// </summary>
    /// <param name="request">The magnet link to analyze</param>
    /// <returns>List of available files in the torrent</returns>
    /// <response code="200">Returns the list of available files</response>
    /// <response code="400">Invalid magnet link provided</response>
    [HttpPost]
    [Route("CheckFilesMagnet")]
    [ProducesResponseType(typeof(IList<TorrentClientAvailableFile>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(String), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IList<TorrentClientAvailableFile>>> CheckFilesMagnet([FromBody] TorrentControllerCheckFilesRequest? request)
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

    /// <summary>
    /// Deletes a torrent and optionally its associated data
    /// </summary>
    /// <param name="torrentId">The unique identifier of the torrent to delete</param>
    /// <param name="request">Delete options specifying what should be removed</param>
    /// <returns>Success status</returns>
    /// <response code="200">Torrent deleted successfully</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpPost]
    [Route("Delete/{torrentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// Retries a failed torrent download
    /// </summary>
    /// <param name="torrentId">The unique identifier of the torrent to retry</param>
    /// <returns>Success status</returns>
    /// <response code="200">Retry initiated successfully</response>
    [HttpPost]
    [Route("Retry/{torrentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Retry(Guid torrentId)
    {
        logger.LogDebug("Retry {torrentId}", torrentId);

        await torrents.UpdateRetry(torrentId, DateTimeOffset.UtcNow, 0);
        await torrents.RetryTorrent(torrentId, 0);

        return Ok();
    }

    /// <summary>
    /// Retries a failed download within a torrent
    /// </summary>
    /// <param name="downloadId">The unique identifier of the download to retry</param>
    /// <returns>Success status</returns>
    /// <response code="200">Retry initiated successfully</response>
    [HttpPost]
    [Route("RetryDownload/{downloadId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> RetryDownload(Guid downloadId)
    {
        logger.LogDebug("Retry download {downloadId}", downloadId);

        await torrents.RetryDownload(downloadId);

        return Ok();
    }

    /// <summary>
    /// Updates torrent configuration
    /// </summary>
    /// <param name="torrent">The updated torrent configuration</param>
    /// <returns>Success status</returns>
    /// <response code="200">Torrent updated successfully</response>
    /// <response code="400">Invalid torrent configuration</response>
    [HttpPut]
    [Route("Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Update([FromBody] Torrent? torrent)
    {
        if (torrent == null)
        {
            return BadRequest();
        }

        await torrents.Update(torrent);

        return Ok();
    }

    /// <summary>
    /// Tests regex patterns against torrent files
    /// </summary>
    /// <param name="request">The regex patterns and magnet link to test</param>
    /// <returns>Matching files and any regex errors</returns>
    /// <response code="200">Returns the regex test results</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpPost]
    [Route("VerifyRegex")]
    [ProducesResponseType(typeof(RegexVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegexVerificationResult>> VerifyRegex([FromBody] TorrentControllerVerifyRegexRequest? request)
    {
        if (request == null)
        {
            return Ok();
        }

        var includeError = "";
        var excludeError = "";

        IList<TorrentClientAvailableFile> availableFiles;

        if (!String.IsNullOrWhiteSpace(request.MagnetLink))
        {
            var magnet = MagnetLink.Parse(request.MagnetLink);

            availableFiles = await torrents.GetAvailableFiles(magnet.InfoHashes.V1OrV2.ToHex());
        }
        else
        {
            return BadRequest();
        }

        var selectedFiles = new List<TorrentClientAvailableFile>();

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

        return Ok(new RegexVerificationResult
        {
            IncludeError = includeError,
            ExcludeError = excludeError,
            SelectedFiles = selectedFiles
        });
    }
}

/// <summary>
/// Request model for uploading a torrent file
/// </summary>
public class TorrentControllerUploadFileRequest
{
    /// <summary>
    /// Configuration for the torrent download
    /// </summary>
    [Required]
    public Torrent? Torrent { get; set; }
}

/// <summary>
/// Request model for adding a magnet link
/// </summary>
public class TorrentControllerUploadMagnetRequest
{
    /// <summary>
    /// The magnet URI to process
    /// </summary>
    [Required]
    public String? MagnetLink { get; set; }

    /// <summary>
    /// Configuration for the torrent download
    /// </summary>
    [Required]
    public Torrent? Torrent { get; set; }
}

/// <summary>
/// Request model for deleting a torrent
/// </summary>
public class TorrentControllerDeleteRequest
{
    /// <summary>
    /// Whether to delete the downloaded data
    /// </summary>
    public Boolean DeleteData { get; set; }

    /// <summary>
    /// Whether to remove the torrent from the Debrid service
    /// </summary>
    public Boolean DeleteRdTorrent { get; set; }

    /// <summary>
    /// Whether to delete local torrent files
    /// </summary>
    public Boolean DeleteLocalFiles { get; set; }
}

/// <summary>
/// Request model for checking files in a magnet link
/// </summary>
public class TorrentControllerCheckFilesRequest
{
    /// <summary>
    /// The magnet URI to analyze
    /// </summary>
    [Required]
    public String? MagnetLink { get; set; }
}

/// <summary>
/// Request model for verifying regex patterns
/// </summary>
public class TorrentControllerVerifyRegexRequest
{
    /// <summary>
    /// Pattern for including files
    /// </summary>
    public String? IncludeRegex { get; set; }

    /// <summary>
    /// Pattern for excluding files
    /// </summary>
    public String? ExcludeRegex { get; set; }

    /// <summary>
    /// Magnet link to test patterns against
    /// </summary>
    public String? MagnetLink { get; set; }
}

/// <summary>
/// Response model for regex verification results
/// </summary>
public class RegexVerificationResult
{
    /// <summary>
    /// Error message for the include regex pattern, if any
    /// </summary>
    [JsonPropertyName("includeError")]
    public String IncludeError { get; set; } = String.Empty;

    /// <summary>
    /// Error message for the exclude regex pattern, if any
    /// </summary>
    [JsonPropertyName("excludeError")]
    public String ExcludeError { get; set; } = String.Empty;

    /// <summary>
    /// Files that match the specified patterns
    /// </summary>
    [JsonPropertyName("selectedFiles")]
    public IList<TorrentClientAvailableFile> SelectedFiles { get; set; } = new List<TorrentClientAvailableFile>();
}
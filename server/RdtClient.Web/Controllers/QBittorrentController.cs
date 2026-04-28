using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.QBittorrent;
using RdtClient.Service.Services;
using RealDebridException = RDNET.RealDebridException;

namespace RdtClient.Web.Controllers;

/// <summary>
///     This API behaves as a regular QBittorrent 4+ API
///     Documentation is found here: https://github.com/qbittorrent/qBittorrent/wiki/WebUI-API-(qBittorrent-4.1)
/// </summary>
[ApiController]
[Route("api/v2")]
[Route("qbittorrent/api/v2")]
public class QBittorrentController(ILogger<QBittorrentController> logger, QBittorrent qBittorrent, IHttpClientFactory httpClientFactory) : Controller
{
    [AllowAnonymous]
    [Route("/version/api")]
    [HttpGet]
    [HttpPost]
    public ActionResult LegacyVersionApi()
    {
        // Returning 20 nudges older qB clients to use /api/v2 endpoints.
        return Content("20", "text/plain");
    }

    [AllowAnonymous]
    [Route("auth/login")]
    [HttpGet]
    public async Task<ActionResult> AuthLogin([FromQuery] QBAuthLoginRequest request)
    {
        logger.LogDebug($"Auth login");

        if (Settings.Get.General.AuthenticationType == AuthenticationType.None)
        {
            return Content("Ok.", "text/plain");
        }

        if (String.IsNullOrWhiteSpace(request.UserName) || String.IsNullOrEmpty(request.Password))
        {
            return Content("Fails.", "text/plain");
        }

        var result = await qBittorrent.AuthLogin(request.UserName, request.Password);

        if (result)
        {
            return Content("Ok.", "text/plain");
        }

        return Content("Fails.", "text/plain");
    }

    [AllowAnonymous]
    [Route("auth/login")]
    [HttpPost]
    public async Task<ActionResult> AuthLoginPost([FromForm] QBAuthLoginRequest request)
    {
        return await AuthLogin(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("auth/logout")]
    [HttpGet]
    [HttpPost]
    public async Task<ActionResult> AuthLogout()
    {
        logger.LogDebug($"Auth logout");

        await qBittorrent.AuthLogout();

        return Ok();
    }

    [AllowAnonymous]
    [Route("app/version")]
    [HttpGet]
    [HttpPost]
    public ActionResult AppVersion()
    {
        return Ok("v4.3.2");
    }

    [AllowAnonymous]
    [Route("app/webapiVersion")]
    [HttpGet]
    [HttpPost]
    public ActionResult AppWebVersion()
    {
        return Ok("2.7");
    }

    [AllowAnonymous]
    [Route("app/buildInfo")]
    [HttpGet]
    [HttpPost]
    public ActionResult AppBuildInfo()
    {
        var result = new AppBuildInfo
        {
            Bitness = 64,
            Boost = "1.75.0",
            Libtorrent = "1.2.11.0",
            Openssl = "1.1.1i",
            Qt = "5.15.2",
            Zlib = "1.2.11"
        };

        return Ok(result);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("app/shutdown")]
    [HttpGet]
    [HttpPost]
    public ActionResult AppShutdown()
    {
        return Ok();
    }

    [AllowAnonymous]
    [Route("app/preferences")]
    [HttpGet]
    [HttpPost]
    public async Task<ActionResult<AppPreferences>> AppPreferences()
    {
        var result = await qBittorrent.AppPreferences();

        return Ok(result);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("app/setPreferences")]
    [HttpGet]
    [HttpPost]
    public ActionResult AppSetPreferences()
    {
        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("app/defaultSavePath")]
    [HttpGet]
    [HttpPost]
    public ActionResult<AppPreferences> AppDefaultSavePath()
    {
        var result = Settings.AppDefaultSavePath;

        return Ok(result);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/info")]
    [HttpGet]
    public async Task<ActionResult<IList<TorrentInfo>>> TorrentsInfo([FromQuery] QBTorrentsInfoRequest request)
    {
        var results = await qBittorrent.TorrentInfo();

        results = results.Where(m => MatchesFilter(m, request.Filter)).ToList();

        if (!String.IsNullOrWhiteSpace(request.Category))
        {
            results = results.Where(m => m.Category == request.Category).ToList();
        }

        if (!String.IsNullOrWhiteSpace(request.Hashes))
        {
            var hashSet = new HashSet<String>(request.Hashes.Split('|', StringSplitOptions.RemoveEmptyEntries),
                                              StringComparer.OrdinalIgnoreCase);

            results = results.Where(m => hashSet.Contains(m.Hash)).ToList();
        }

        return Ok(results);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/info")]
    [HttpPost]
    public async Task<ActionResult<IList<TorrentInfo>>> TorrentsInfoPost([FromForm] QBTorrentsInfoRequest request)
    {
        return await TorrentsInfo(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/count")]
    [HttpGet]
    public async Task<ActionResult<Int32>> TorrentsCount([FromQuery] QBTorrentsCountRequest request)
    {
        var results = await qBittorrent.TorrentInfo();
        results = results.Where(m => MatchesFilter(m, request.Filter)).ToList();

        return results.Count;
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/count")]
    [HttpPost]
    public async Task<ActionResult<Int32>> TorrentsCountPost([FromForm] QBTorrentsCountRequest request)
    {
        return await TorrentsCount(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/files")]
    [HttpGet]
    public async Task<ActionResult<IList<TorrentFileItem>>> TorrentsFiles([FromQuery] QBTorrentsHashRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hash))
        {
            return BadRequest();
        }

        var result = await qBittorrent.TorrentFileContents(request.Hash);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/files")]
    [HttpPost]
    public async Task<ActionResult<IList<TorrentFileItem>>> TorrentsFilesPost([FromForm] QBTorrentsHashRequest request)
    {
        return await TorrentsFiles(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/properties")]
    [HttpGet]
    public async Task<ActionResult<IList<TorrentInfo>>> TorrentsProperties([FromQuery] QBTorrentsHashRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hash))
        {
            return BadRequest();
        }

        var result = await qBittorrent.TorrentProperties(request.Hash);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/properties")]
    [HttpPost]
    public async Task<ActionResult<IList<TorrentInfo>>> TorrentsPropertiesPost([FromForm] QBTorrentsHashRequest request)
    {
        return await TorrentsProperties(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/pause")]
    [HttpGet]
    public async Task<ActionResult> TorrentsPause([FromQuery] QBTorrentsHashesRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hashes))
        {
            return BadRequest();
        }

        var hashes = request.Hashes.Split("|");

        foreach (var hash in hashes)
        {
            await qBittorrent.TorrentPause(hash);
        }

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/pause")]
    [HttpPost]
    public async Task<ActionResult> TorrentsPausePost([FromForm] QBTorrentsHashesRequest request)
    {
        return await TorrentsPause(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/resume")]
    [HttpGet]
    public async Task<ActionResult> TorrentsResume([FromQuery] QBTorrentsHashesRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hashes))
        {
            return BadRequest();
        }

        var hashes = request.Hashes.Split("|");

        foreach (var hash in hashes)
        {
            await qBittorrent.TorrentResume(hash);
        }

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/resume")]
    [HttpPost]
    public async Task<ActionResult> TorrentsResumePost([FromForm] QBTorrentsHashesRequest request)
    {
        return await TorrentsResume(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/setShareLimits")]
    [HttpGet]
    [HttpPost]
    public ActionResult TorrentsSetShareLimits()
    {
        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/delete")]
    [HttpGet]
    public async Task<ActionResult> TorrentsDelete([FromQuery] QBTorrentsDeleteRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hashes))
        {
            return BadRequest();
        }

        logger.LogDebug("Delete {Hashes}", request.Hashes);

        var hashes = request.Hashes.Split("|");

        foreach (var hash in hashes)
        {
            await qBittorrent.TorrentsDelete(hash, request.DeleteFiles);
        }

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/delete")]
    [HttpPost]
    public async Task<ActionResult> TorrentsDeletePost([FromForm] QBTorrentsDeleteRequest request)
    {
        return await TorrentsDelete(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/add")]
    [HttpGet]
    public async Task<ActionResult> TorrentsAdd([FromQuery] QBTorrentsAddRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Urls))
        {
            return BadRequest();
        }

        var urls = request.Urls.Split("\n");

        foreach (var url in urls)
        {
            try
            {
                if (url.StartsWith("magnet"))
                {
                    await qBittorrent.TorrentsAddMagnet(url.Trim(), request.Category, null);
                }
                else if (url.StartsWith("http"))
                {
                    var httpClient = httpClientFactory.CreateClient();
                    var result = await httpClient.GetByteArrayAsync(url);
                    await qBittorrent.TorrentsAddFile(result, request.Category, null);
                }
                else
                {
                    return BadRequest($"Invalid torrent link format {url}");
                }
            }
            catch (RealDebridException ex)
            {
                // Infringing file.
                if (ex.ErrorCode == 35)
                {
                    return Ok("Fails.");
                }
            }
        }

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/add")]
    [HttpPost]
    public async Task<ActionResult> TorrentsAddPost([FromForm] QBTorrentsAddRequest request)
    {
        foreach (var file in Request.Form.Files)
        {
            if (file.Length > 0)
            {
                await using var target = new MemoryStream();

                await file.CopyToAsync(target);
                var fileBytes = target.ToArray();

                await qBittorrent.TorrentsAddFile(fileBytes, request.Category, request.Priority);
            }
        }

        if (request.Urls != null)
        {
            return await TorrentsAdd(request);
        }

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/filePrio")]
    [HttpGet]
    public async Task<ActionResult> TorrentsFilePrio([FromQuery] QBTorrentsFilePrioRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hash) || String.IsNullOrWhiteSpace(request.Id) || request.Priority == null)
        {
            return BadRequest();
        }

        var fileIds = request.Id
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => Int32.TryParse(value, out var parsedValue) ? parsedValue : (Int32?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        if (fileIds.Count == 0)
        {
            return BadRequest();
        }

        await qBittorrent.TorrentsFilePrio(request.Hash, fileIds, request.Priority.Value);

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/filePrio")]
    [HttpPost]
    public async Task<ActionResult> TorrentsFilePrioPost([FromForm] QBTorrentsFilePrioRequest request)
    {
        return await TorrentsFilePrio(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/setCategory")]
    [HttpGet]
    public async Task<ActionResult> TorrentsSetCategory([FromQuery] QBTorrentsSetCategoryRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hashes))
        {
            return BadRequest();
        }

        var hashes = request.Hashes.Split("|");

        foreach (var hash in hashes)
        {
            await qBittorrent.TorrentsSetCategory(hash, request.Category);
        }

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/setCategory")]
    [HttpPost]
    public async Task<ActionResult> TorrentsSetCategoryPost([FromForm] QBTorrentsSetCategoryRequest request)
    {
        return await TorrentsSetCategory(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/categories")]
    [HttpGet]
    [HttpPost]
    public async Task<ActionResult<IDictionary<String, TorrentCategory>>> TorrentsCategories()
    {
        var categories = await qBittorrent.TorrentsCategories();

        return Ok(categories);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/createCategory")]
    [HttpGet]
    [HttpPost]
    public async Task<ActionResult> TorrentsCreateCategory([FromForm] QBTorrentsCreateCategoryRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest("category name is empty");
        }

        await qBittorrent.CategoryCreate(request.Category.Trim());

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/createTags")]
    [HttpGet]
    [HttpPost]
    public ActionResult TorrentsCreateTags()
    {
        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/removeCategories")]
    [HttpGet]
    [HttpPost]
    public async Task<ActionResult> TorrentsRemoveCategories([FromForm] QBTorrentsRemoveCategoryRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Categories))
        {
            return Ok();
        }

        var categories = request.Categories.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var category in categories)
        {
            await qBittorrent.CategoryRemove(category.Trim());
        }

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/setForcestart")]
    [HttpGet]
    [HttpPost]
    public ActionResult TorrentsSetForceStart()
    {
        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/tags")]
    [HttpGet]
    [HttpPost]
    public ActionResult TorrentsTags()
    {
        return Ok(new List<String>());
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/topPrio")]
    [HttpGet]
    public async Task<ActionResult> TorrentsTopPrio([FromQuery] QBTorrentsHashesRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hashes))
        {
            return BadRequest();
        }

        var hashes = request.Hashes.Split("|");

        foreach (var hash in hashes)
        {
            await qBittorrent.TorrentsTopPrio(hash);
        }

        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/topPrio")]
    [HttpPost]
    public async Task<ActionResult> TorrentsTopPrioPost([FromForm] QBTorrentsHashesRequest request)
    {
        return await TorrentsTopPrio(request);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("sync/maindata")]
    [HttpGet]
    public async Task<ActionResult> SyncMainData()
    {
        var result = await qBittorrent.SyncMainData();

        return Ok(result);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("sync/maindata")]
    [HttpPost]
    public async Task<ActionResult> SyncMainDataPost()
    {
        return await SyncMainData();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("transfer/info")]
    [HttpGet]
    public ActionResult TransferInfo()
    {
        return Ok(QBittorrent.TransferInfo());
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("transfer/info")]
    [HttpPost]
    public ActionResult TransferInfoPost()
    {
        return TransferInfo();
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/trackers")]
    [HttpGet]
    public async Task<ActionResult<IList<TorrentInfo>>> TorrentsTrackers([FromQuery] QBTorrentsHashRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Hash))
        {
            return BadRequest();
        }

        var results = await qBittorrent.TorrentsTrackers(request.Hash);

        return Ok(results);
    }

    [Authorize(Policy = "AuthSetting")]
    [Route("torrents/trackers")]
    [HttpPost]
    public async Task<ActionResult<IList<TorrentInfo>>> TorrentsTrackersPost([FromForm] QBTorrentsHashRequest request)
    {
        return await TorrentsTrackers(request);
    }

    private static Boolean MatchesFilter(TorrentInfo torrent, String? filter)
    {
        if (String.IsNullOrWhiteSpace(filter) || filter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return filter.ToLowerInvariant() switch
        {
            "downloading" => torrent.Progress < 1f && !String.Equals(torrent.State, "error", StringComparison.OrdinalIgnoreCase),
            "completed" => torrent.Progress >= 1f || torrent.CompletionOn.HasValue,
            "paused" => torrent.State?.StartsWith("paused", StringComparison.OrdinalIgnoreCase) == true,
            "active" => (torrent.Dlspeed ?? 0) > 0 || (torrent.Upspeed ?? 0) > 0,
            "inactive" => (torrent.Dlspeed ?? 0) <= 0 && (torrent.Upspeed ?? 0) <= 0,
            "resumed" => torrent.State?.StartsWith("paused", StringComparison.OrdinalIgnoreCase) != true &&
                         !String.Equals(torrent.State, "error", StringComparison.OrdinalIgnoreCase),
            "stalled" => String.Equals(torrent.State, "stalledDL", StringComparison.OrdinalIgnoreCase) ||
                         String.Equals(torrent.State, "stalledUP", StringComparison.OrdinalIgnoreCase),
            "stalled_uploading" => String.Equals(torrent.State, "stalledUP", StringComparison.OrdinalIgnoreCase),
            "stalled_downloading" => String.Equals(torrent.State, "stalledDL", StringComparison.OrdinalIgnoreCase),
            "errored" => String.Equals(torrent.State, "error", StringComparison.OrdinalIgnoreCase),
            _ => String.Equals(torrent.State, filter, StringComparison.OrdinalIgnoreCase)
        };
    }
}

public class QBAuthLoginRequest
{
    public String? UserName { get; set; }
    public String? Password { get; set; }
}

public class QBTorrentsInfoRequest
{
    public String? Filter { get; set; }
    public String? Category { get; set; }
    public String? Hashes { get; set; }
}


public class QBTorrentsCountRequest
{
    public String? Filter { get; set; }
}

public class QBTorrentsHashRequest
{
    public String? Hash { get; set; }
}

public class QBTorrentsFilePrioRequest
{
    public String? Hash { get; set; }
    public String? Id { get; set; }
    public Int32? Priority { get; set; }
}

public class QBTorrentsDeleteRequest
{
    public String? Hashes { get; set; }
    public Boolean DeleteFiles { get; set; }
}

public class QBTorrentsAddRequest
{
    public String? Urls { get; set; }
    public String? Category { get; set; }
    public Int32? Priority { get; set; }
}

public class QBTorrentsSetCategoryRequest
{
    public String? Hashes { get; set; }
    public String? Category { get; set; }
}

public class QBTorrentsCreateCategoryRequest
{
    public String? Category { get; set; }
}

public class QBTorrentsRemoveCategoryRequest
{
    public String? Categories { get; set; }
}

public class QBTorrentsHashesRequest
{
    public String? Hashes { get; set; }
}

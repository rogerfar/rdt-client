using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonoTorrent;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;
using Torrent = RdtClient.Data.Models.Data.Torrent;

namespace RdtClient.Web.Controllers;

[Authorize]
[Route("Api/Torrents")]
public class TorrentsController : Controller
{
    private readonly TorrentRunner _torrentRunner;
    private readonly Torrents _torrents;

    public TorrentsController(Torrents torrents, TorrentRunner torrentRunner)
    {
        _torrents = torrents;
        _torrentRunner = torrentRunner;
    }

    [HttpGet]
    [Route("")]
    public async Task<ActionResult<IList<Torrent>>> GetAll()
    {
        var results = await _torrents.Get();

        // Prevent infinite recursion when serializing
        foreach (var file in results.SelectMany(torrent => torrent.Downloads))
        {
            file.Torrent = null;
        }

        return Ok(results);
    }

    [HttpGet]
    [Route("Get/{torrentId:guid}")]
    public async Task<ActionResult<Torrent>> GetById(Guid torrentId)
    {
        var torrent = await _torrents.GetById(torrentId);

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
    ///     Used for debugging only. Force a tick.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("Tick")]
    public async Task<ActionResult> Tick()
    {
        await _torrentRunner.Tick();

        return Ok();
    }

    [HttpPost]
    [Route("UploadFile")]
    public async Task<ActionResult> UploadFile([FromForm] IFormFile file,
                                               [ModelBinder(BinderType = typeof(JsonModelBinder))]
                                               TorrentControllerUploadFileRequest formData)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Select(x => x.Value.Errors)
                                   .Where(y => y.Count > 0)
                                   .ToList();

            return BadRequest(errors);
        }

        if (file == null || file.Length <= 0)
        {
            throw new Exception("Invalid torrent file");
        }

        var fileStream = file.OpenReadStream();

        await using var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();

        await _torrents.UploadFile(bytes, formData.Torrent);

        return Ok();
    }

    [HttpPost]
    [Route("UploadMagnet")]
    public async Task<ActionResult> UploadMagnet([FromBody] TorrentControllerUploadMagnetRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Select(x => x.Value.Errors)
                                   .Where(y => y.Count > 0)
                                   .ToList();

            return BadRequest(errors);
        }

        await _torrents.UploadMagnet(request.MagnetLink, request.Torrent);

        return Ok();
    }

    [HttpPost]
    [Route("CheckFiles")]
    public async Task<ActionResult> CheckFiles([FromForm] IFormFile file)
    {
        if (file == null || file.Length <= 0)
        {
            throw new Exception("Invalid torrent file");
        }

        var fileStream = file.OpenReadStream();

        await using var memoryStream = new MemoryStream();

        await fileStream.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();

        var torrent = await MonoTorrent.Torrent.LoadAsync(bytes);

        var result = await _torrents.GetAvailableFiles(torrent.InfoHash.ToHex());

        return Ok(result);
    }

    [HttpPost]
    [Route("CheckFilesMagnet")]
    public async Task<ActionResult> CheckFilesMagnet([FromBody] TorrentControllerCheckFilesRequest request)
    {
        var magnet = MagnetLink.Parse(request.MagnetLink);

        var result = await _torrents.GetAvailableFiles(magnet.InfoHash.ToHex());

        return Ok(result);
    }

    [HttpPost]
    [Route("Delete/{torrentId:guid}")]
    public async Task<ActionResult> Delete(Guid torrentId, [FromBody] TorrentControllerDeleteRequest request)
    {
        await _torrents.Delete(torrentId, request.DeleteData, request.DeleteRdTorrent, request.DeleteLocalFiles);

        return Ok();
    }

    [HttpPost]
    [Route("Retry/{torrentId:guid}")]
    public async Task<ActionResult> Retry(Guid torrentId)
    {
        await _torrents.UpdateRetry(torrentId, DateTimeOffset.UtcNow, 0);
        await _torrents.RetryTorrent(torrentId, 0);

        return Ok();
    }

    [HttpPost]
    [Route("RetryDownload/{downloadId:guid}")]
    public async Task<ActionResult> RetryDownload(Guid downloadId)
    {
        await _torrents.RetryDownload(downloadId);

        return Ok();
    }
        
    [HttpPut]
    [Route("Update")]
    public async Task<ActionResult> Update([FromBody] Torrent torrent)
    {
        await _torrents.Update(torrent);

        return Ok();
    }
}

public class TorrentControllerUploadFileRequest
{
    public Torrent Torrent { get; set; }
}

public class TorrentControllerUploadMagnetRequest
{
    public String MagnetLink { get; set; }
    public Torrent Torrent { get; set; }
}

public class TorrentControllerDeleteRequest
{
    public Boolean DeleteData { get; set; }
    public Boolean DeleteRdTorrent { get; set; }
    public Boolean DeleteLocalFiles { get; set; }
}

public class TorrentControllerCheckFilesRequest
{
    public String MagnetLink { get; set; }
}
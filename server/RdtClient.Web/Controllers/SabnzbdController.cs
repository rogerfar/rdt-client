using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Models.Sabnzbd;
using RdtClient.Service.Services;

namespace RdtClient.Web.Controllers;

[ApiController]
[Route("api")]
[Route("sabnzbd/api")]
[Authorize(Policy = "Sabnzbd")]
public class SabnzbdController(ILogger<SabnzbdController> logger, Sabnzbd sabnzbd) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    [HttpPost]
    public ActionResult Get([FromQuery] String? mode)
    {
        if (Request.HasFormContentType)
        {
            mode ??= Request.Form["mode"].ToString();
        }

        if (String.IsNullOrWhiteSpace(mode))
        {
            return BadRequest(new SabnzbdResponse { Error = "No mode specified" });
        }

        logger.LogWarning($"Sabnzbd API called (not implemented) - Method: {Request.Method}, Query: {Request.QueryString}");
        return NotFound(new SabnzbdResponse());
    }
    
    [AllowAnonymous]
    [HttpGet]
    [SabnzbdMode("version")]
    public ActionResult Version()
    {
        logger.LogDebug("Sabnzbd mode: version");
        return Ok(new SabnzbdResponse { Version = "4.4.0" });
    }

    [HttpGet]
    [SabnzbdMode("queue")]
    public async Task<ActionResult> Queue()
    {
        logger.LogDebug("Sabnzbd mode: queue");
        var name = GetParam("name");

        if (name == "delete")
        {
            var value = GetParam("value");

            if (String.IsNullOrWhiteSpace(value))
            {
                return BadRequest(new SabnzbdResponse
                {
                    Error = "No value specified for delete operation"
                });
            }
            await sabnzbd.Delete(value ?? "");
            return Ok(new SabnzbdResponse { Status = true });
        }

        return Ok(new SabnzbdResponse { Queue = await sabnzbd.GetQueue() });
    }

    [HttpGet]
    [SabnzbdMode("history")]
    public async Task<ActionResult> History()
    {
        logger.LogDebug("Sabnzbd mode: history");
        return Ok(new SabnzbdResponse { History = await sabnzbd.GetHistory() });
    }

    [HttpGet]
    [SabnzbdMode("get_config")]
    public ActionResult GetConfig()
    {
        logger.LogDebug("Sabnzbd mode: get_config");
        return Ok(new SabnzbdResponse { Config = sabnzbd.GetConfig() });
    }

    [HttpGet]
    [SabnzbdMode("get_cats")]
    public ActionResult GetCats()
    {
        logger.LogDebug("Sabnzbd mode: get_cats");
        return Ok(new SabnzbdResponse { Categories = sabnzbd.GetCategories() });
    }

    [HttpGet]
    [HttpPost]
    [SabnzbdMode("addurl")]
    public async Task<ActionResult> AddUrl()
    {
        logger.LogDebug("Sabnzbd mode: addurl");
        var url = GetParam("name");
        var category = GetParam("cat");
        var priorityStr = GetParam("priority");

        Int32? priority = Int32.TryParse(priorityStr, out var p) ? p : null;

        var result = await sabnzbd.AddUrl(url ?? "", category, priority);
        return Ok(new SabnzbdResponse { Status = true, NzoIds = [result] });
    }

    [HttpPost]
    [SabnzbdMode("addfile")]
    public async Task<ActionResult> AddFile()
    {
        logger.LogDebug("Sabnzbd mode: addfile");
        if (!Request.HasFormContentType)
        {
            return BadRequest("Expected multipart/form-data");
        }

        var file = Request.Form.Files.FirstOrDefault();
        if (file == null)
        {
            return BadRequest("No file uploaded");
        }

        var category = GetParam("cat");
        var priorityStr = GetParam("priority");
        Int32? priority = Int32.TryParse(priorityStr, out var p) ? p : null;

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var result = await sabnzbd.AddFile(ms.ToArray(), file.FileName, category, priority);
        
        return Ok(new SabnzbdResponse { Status = true, NzoIds = [result] });
    }

    [HttpGet]
    [SabnzbdMode("fullstatus")]
    public async Task<ActionResult> FullStatus()
    {
        logger.LogDebug("Sabnzbd mode: fullstatus");
        return Ok(new SabnzbdResponse { Version = "4.4.0", Queue = await sabnzbd.GetQueue() });
    }

    private String? GetParam(String name)
    {
        var value = Request.Query[name].ToString();
        if (String.IsNullOrWhiteSpace(value) && Request.HasFormContentType)
        {
            value = Request.Form[name].ToString();
        }
        return value;
    }
}

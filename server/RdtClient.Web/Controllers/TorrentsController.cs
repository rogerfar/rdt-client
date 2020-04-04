using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;

namespace RdtClient.Web.Controllers
{
    [Route("Api/Torrents")]
    public class TorrentsController : Controller
    {
        private readonly ITorrents _torrents;
        private readonly IScheduler _scheduler;

        public TorrentsController(ITorrents torrents, IScheduler scheduler)
        {
            _torrents = torrents;
            _scheduler = scheduler;
        }

        [HttpGet]
        [Route("")]
        public async Task<ActionResult<IList<Torrent>>> Get()
        {
            try
            {
                var result = await _torrents.Get();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<Torrent>> Get(Guid id)
        {
            try
            {
                var result = await _torrents.Get(id);

                if (result == null)
                {
                    throw new Exception("Torrent not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [Route("UploadFile")]
        public async Task<ActionResult> UploadFile([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length <= 0)
                {
                    throw new Exception("Invalid torrent file");
                }

                var fileStream = file.OpenReadStream();

                await using var memoryStream = new MemoryStream();

                fileStream.CopyTo(memoryStream);

                var bytes = memoryStream.ToArray();
                
                await _torrents.UploadFile(bytes);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("UploadMagnet")]
        public async Task<ActionResult> UploadMagnet([FromBody] TorrentControllerUploadMagnetRequest request)
        {
            try
            {
                await _torrents.UploadMagnet(request.MagnetLink);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                await _torrents.Delete(id);

                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpGet]
        [Route("Download/{id}")]
        public async Task<ActionResult> Download(Guid id)
        {
            try
            {
                await _torrents.Download(id);
                await _scheduler.Process();

                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class TorrentControllerUploadMagnetRequest
    {
        public String MagnetLink { get; set; }
    }
}

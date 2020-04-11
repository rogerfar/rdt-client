using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;

namespace RdtClient.Web.Controllers
{
    [Authorize]
    [Route("Api/Torrents")]
    public class TorrentsController : Controller
    {
        private readonly ITorrents _torrents;

        public TorrentsController(ITorrents torrents)
        {
            _torrents = torrents;
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
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<Torrent>> Get(Guid id)
        {
            try
            {
                var result = await _torrents.GetById(id);

                if (result == null)
                {
                    throw new Exception("Torrent not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("UploadFile")]
        public async Task<ActionResult> UploadFile([FromForm] IFormFile file,
                                                   [ModelBinder(BinderType = typeof(JsonModelBinder))]
                                                   TorrentControllerUploadFileRequest formData)
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

                await _torrents.UploadFile(bytes, formData.AutoDownload, formData.AutoDelete);

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
                await _torrents.UploadMagnet(request.MagnetLink, request.AutoDownload, request.AutoDelete);

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
            catch (Exception ex)
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

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class TorrentControllerUploadFileRequest
    {
        public Boolean AutoDownload { get; set; }
        public Boolean AutoDelete { get; set; }
    }

    public class TorrentControllerUploadMagnetRequest
    {
        public String MagnetLink { get; set; }
        public Boolean AutoDownload { get; set; }
        public Boolean AutoDelete { get; set; }
    }
}
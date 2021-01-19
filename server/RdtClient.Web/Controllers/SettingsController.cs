using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Models;
using RdtClient.Service.Services;

namespace RdtClient.Web.Controllers
{
    [Authorize]
    [Route("Api/Settings")]
    public class SettingsController : Controller
    {
        private readonly ISettings _settings;
        private readonly ITorrents _torrents;

        public SettingsController(ISettings settings, ITorrents torrents)
        {
            _settings = settings;
            _torrents = torrents;
        }

        [HttpGet]
        [Route("")]
        public async Task<ActionResult<IList<Setting>>> Get()
        {
            var result = await _settings.GetAll();
            return Ok(result);
        }

        [HttpPut]
        [Route("")]
        public async Task<ActionResult> Update([FromBody] SettingsControllerUpdateRequest request)
        {
            await _settings.Update(request.Settings);
            _torrents.Reset();

            return Ok();
        }

        [HttpGet]
        [Route("Profile")]
        public async Task<ActionResult<Profile>> Profile()
        {
            var profile = await _torrents.GetProfile();
            return Ok(profile);
        }
        
        [HttpPost]
        [Route("TestPath")]
        public async Task<ActionResult> TestPath([FromBody] SettingsControllerTestPathRequest request)
        {
            await _settings.TestPath(request.Path);

            return Ok();
        }
        
        [HttpGet]
        [Route("TestSpeed")]
        public async Task<ActionResult> TestSpeed()
        {
            var downloadPath = await _settings.GetString("DownloadPath");
            var mappedPath = await _settings.GetString("MappedPath");
            
            var downloadSpeed = await _settings.TestDownloadSpeed();
            var containerWriteSpeed = await _settings.TestWriteSpeed(downloadPath);
            var remoteWriteSpeed = await _settings.TestWriteSpeed(mappedPath);

            var result = new
            {
                downloadSpeed,
                containerWriteSpeed,
                remoteWriteSpeed
            };

            return Ok(result);
        }
    }

    public class SettingsControllerUpdateRequest
    {
        public IList<Setting> Settings { get; set; }
    }

    public class SettingsControllerTestPathRequest
    {
        public String Path { get; set; }
    }
}
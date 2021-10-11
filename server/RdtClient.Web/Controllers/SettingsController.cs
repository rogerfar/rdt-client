using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Models;
using RdtClient.Service.Services;
using Serilog.Events;

namespace RdtClient.Web.Controllers
{
    [Authorize]
    [Route("Api/Settings")]
    public class SettingsController : Controller
    {
        private readonly Settings _settings;
        private readonly Torrents _torrents;

        public SettingsController(Settings settings, Torrents torrents)
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
            
            if (!Enum.TryParse<LogEventLevel>(Settings.Get.LogLevel, out var logLevel))
            {
                logLevel = LogEventLevel.Information;
            }

            Program.LoggingLevelSwitch.MinimumLevel = logLevel;

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
        [Route("TestDownloadSpeed")]
        public async Task<ActionResult> TestDownloadSpeed(CancellationToken cancellationToken)
        {
            var downloadSpeed = await _settings.TestDownloadSpeed(cancellationToken);

            return Ok(downloadSpeed);
        }
        
        [HttpGet]
        [Route("TestWriteSpeed")]
        public async Task<ActionResult> TestWriteSpeed()
        {
            var writeSpeed = await _settings.TestWriteSpeed();
            
            return Ok(writeSpeed);
        }

        [HttpPost]
        [Route("TestAria2cConnection")]
        public async Task<ActionResult<String>> TestAria2cConnection([FromBody] SettingsControllerTestAria2cConnectionRequest request)
        {
            var version = await _settings.GetAria2cVersion(request.Url, request.Secret);

            return Ok(version);
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

    public class SettingsControllerTestAria2cConnectionRequest
    {
        public String Url { get; set; }
        public String Secret { get; set; }
    }
}
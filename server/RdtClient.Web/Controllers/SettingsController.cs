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
            try
            {
                var result = await _settings.GetAll();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("")]
        public async Task<ActionResult> Update([FromBody] SettingsControllerUpdateRequest request)
        {
            try
            {
                await _settings.Update(request.Settings);
                _torrents.Reset();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("Profile")]
        public async Task<ActionResult<Profile>> Profile()
        {
            try
            {
                var profile = await _torrents.GetProfile();
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class SettingsControllerUpdateRequest
    {
        public IList<Setting> Settings { get; set; }
    }
}
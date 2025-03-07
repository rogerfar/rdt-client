using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Enums;
using RdtClient.Service.Services;

namespace RdtClient.Web.Controllers;

[Route("Api/Authentication")]
public class AuthController(Authentication authentication, Settings settings) : Controller
{
    [AllowAnonymous]
    [Route("IsLoggedIn")]
    [HttpGet]
    public async Task<ActionResult> IsLoggedIn()
    {
        if (Settings.Get.General.AuthenticationType == AuthenticationType.None)
        {
            return Ok();
        }

        if (User.Identity?.IsAuthenticated == false)
        {
            var user = await authentication.GetUser();

            if (user == null)
            {
                return StatusCode(402, "Setup required");
            }
                
            return StatusCode(403);
        }
            
        return Ok();
    }

    [AllowAnonymous]
    [Route("Create")]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] AuthControllerLoginRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        var user = await authentication.GetUser();

        if (user != null)
        {
            return StatusCode(401);
        }
        
        if (String.IsNullOrEmpty(request.UserName) || String.IsNullOrEmpty(request.Password))
        {
            return BadRequest("Invalid UserName or Password");
        }

        var registerResult = await authentication.Register(request.UserName, request.Password);

        if (!registerResult.Succeeded)
        {
            return BadRequest(registerResult.Errors.First().Description);
        }
            
        await authentication.Login(request.UserName, request.Password);

        return Ok();
    }

    [AllowAnonymous]
    [Route("SetupProvider")]
    [HttpPost]
    public async Task<ActionResult> SetupProvider([FromBody] AuthControllerSetupProviderRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (!String.IsNullOrEmpty(Settings.Get.Provider.ApiKey))
        {
            return StatusCode(401);
        }

        var user = await authentication.GetUser();

        if (user != null)
        {
            return StatusCode(401);
        }

        await settings.Update("Provider:Provider", request.Provider);
        await settings.Update("Provider:ApiKey", request.Token);

        return Ok();
    }

    [AllowAnonymous]
    [Route("Login")]
    [HttpPost]
    public async Task<ActionResult> Login([FromBody] AuthControllerLoginRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        var user = await authentication.GetUser();

        if (user == null)
        {
            return StatusCode(402);
        }

        if (String.IsNullOrEmpty(request.UserName) || String.IsNullOrEmpty(request.Password))
        {
            return BadRequest("Invalid credentials");
        }

        var result = await authentication.Login(request.UserName, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest("Invalid credentials");
        }

        return Ok();
    }
        
    [Route("Logout")]
    [HttpPost]
    public async Task<ActionResult> Logout()
    {
        await authentication.Logout();
        return Ok();
    }
                
    [Route("Update")]
    [HttpPost]
    [Authorize(Policy = "AuthSetting")]
    public async Task<ActionResult> Update([FromBody] AuthControllerUpdateRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.UserName) || String.IsNullOrEmpty(request.Password))
        {
            return BadRequest("Invalid UserName or Password");
        }

        var updateResult = await authentication.Update(request.UserName, request.Password);

        if (!updateResult.Succeeded)
        {
            return BadRequest(updateResult.Errors.First().Description);
        }

        return Ok();
    }
}

public class AuthControllerLoginRequest
{
    public String? UserName { get; set; }
    public String? Password { get; set; }
}

public class AuthControllerSetupProviderRequest
{
    public Int32 Provider { get; set; }
    public String? Token { get; set; }
}

public class AuthControllerUpdateRequest
{
    public String? UserName { get; set; }
    public String? Password { get; set; }
}
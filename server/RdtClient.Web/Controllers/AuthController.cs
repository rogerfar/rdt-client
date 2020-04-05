using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Service.Services;

namespace RdtClient.Web.Controllers
{
    [Route("Api/Authentication")]
    public class AuthController : Controller
    {
        private readonly IAuthentication _authentication;

        public AuthController(IAuthentication authentication)
        {
            _authentication = authentication;
        }

        [AllowAnonymous]
        [Route("Login")]
        [HttpPost]
        public async Task<ActionResult> Login([FromBody] AuthControllerLoginRequest request)
        {
            try
            {
                var user = await _authentication.GetUser();

                if (user == null)
                {
                    var registerResult = await _authentication.Register(request.UserName, request.Password);

                    if (!registerResult.Succeeded)
                    {
                        return BadRequest(registerResult.Errors.First().Description);
                    }
                }

                var result = await _authentication.Login(request.UserName, request.Password);

                if (!result.Succeeded)
                {
                    return BadRequest("Invalid credentials");
                }

                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [Route("Logout")]
        [HttpPost]
        public async Task<ActionResult> Logout()
        {
            try
            {
                await _authentication.Logout();
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class AuthControllerLoginRequest
    {
        public String UserName { get; set; }
        public String Password { get; set; }
    }
}

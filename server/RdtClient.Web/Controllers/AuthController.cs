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
        private readonly Authentication _authentication;

        public AuthController(Authentication authentication)
        {
            _authentication = authentication;
        }
        
        [AllowAnonymous]
        [Route("IsLoggedIn")]
        [HttpGet]
        public async Task<ActionResult> IsLoggedIn()
        {
            if (User.Identity?.IsAuthenticated == false)
            {
                var user = await _authentication.GetUser();

                if (user == null)
                {
                    return StatusCode(402, "Setup required");
                }
                
                return Unauthorized();
            }
            
            return Ok();
        }
        
        [AllowAnonymous]
        [Route("Create")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AuthControllerLoginRequest request)
        {
            var user = await _authentication.GetUser();

            if (user != null)
            {
                return StatusCode(401);
            }

            var registerResult = await _authentication.Register(request.UserName, request.Password);

            if (!registerResult.Succeeded)
            {
                return BadRequest(registerResult.Errors.First().Description);
            }
            
            await _authentication.Login(request.UserName, request.Password);

            return Ok();
        }

        [AllowAnonymous]
        [Route("Login")]
        [HttpPost]
        public async Task<ActionResult> Login([FromBody] AuthControllerLoginRequest request)
        {
            var user = await _authentication.GetUser();

            if (user == null)
            {
                return StatusCode(402);
            }

            var result = await _authentication.Login(request.UserName, request.Password);

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
            await _authentication.Logout();
            return Ok();
        }
    }

    public class AuthControllerLoginRequest
    {
        public String UserName { get; set; }
        public String Password { get; set; }
    }
}

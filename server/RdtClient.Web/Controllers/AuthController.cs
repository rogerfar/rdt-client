using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Enums;
using RdtClient.Service.Services;
using System.ComponentModel.DataAnnotations;

namespace RdtClient.Web.Controllers;

/// <summary>
/// Handles authentication and user management operations
/// </summary>
[Route("Api/Authentication")]
public class AuthController(Authentication authentication, Settings settings) : Controller
{
    /// <summary>
    /// Checks if the current session is authenticated
    /// </summary>
    /// <response code="200">Session is valid and authenticated, or authentication is disabled</response>
    /// <response code="402">System requires initial setup - no users exist in the database</response>
    /// <response code="403">Session is not authenticated</response>
    [AllowAnonymous]
    [Route("IsLoggedIn")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
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
                return Problem(detail: "Setup required", statusCode: StatusCodes.Status402PaymentRequired);
            }

            return Problem(detail: "Login required", statusCode: StatusCodes.Status403Forbidden);
        }

        return Ok();
    }

    /// <summary>
    /// Creates the initial account for the system
    /// </summary>
    /// <remarks>
    /// This endpoint can only be used when no account exists.
    /// It creates the account and automatically logs in.
    /// </remarks>
    /// <param name="request">Registration details for the new account</param>
    /// <response code="200">Account created successfully and user is logged in</response>
    /// <response code="400">Invalid request - username/password missing or validation failed</response>
    /// <response code="401">Cannot create account - a user already exists in system</response>
    [AllowAnonymous]
    [Route("Create")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    public async Task<ActionResult> Create([FromBody] AuthControllerLoginRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        var user = await authentication.GetUser();

        if (user != null)
        {
            return Problem(detail: "User already exists - only one user allowed", statusCode: StatusCodes.Status401Unauthorized);
        }

        if (String.IsNullOrEmpty(request.UserName) || String.IsNullOrEmpty(request.Password))
        {
            return Problem(detail: "Empty UserName or Password", statusCode: StatusCodes.Status400BadRequest);
        }

        var registerResult = await authentication.Register(request.UserName, request.Password);

        if (!registerResult.Succeeded)
        {
            return Problem(detail: registerResult.Errors.First().Description, statusCode: StatusCodes.Status400BadRequest);
        }

        await authentication.Login(request.UserName, request.Password);

        return Ok();
    }

    /// <summary>
    /// Configures the provider settings for the application
    /// </summary>
    /// <remarks>
    /// This endpoint can only be used when no provider is configured.
    /// It sets up the initial provider configuration including the API key.
    /// </remarks>
    /// <param name="request">Provider configuration details including provider type and API token</param>
    /// <response code="200">Provider configured successfully</response>
    /// <response code="400">Invalid request - missing required fields</response>
    /// <response code="401">Provider already configured</response>
    [AllowAnonymous]
    [Route("SetupProvider")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Authenticates a user and creates a new session
    /// </summary>
    /// <remarks>
    /// Validates the provided credentials and creates an authenticated session if valid.
    /// </remarks>
    /// <param name="request">Login credentials</param>
    /// <response code="200">Authentication successful</response>
    /// <response code="400">Invalid credentials or missing required fields</response>
    /// <response code="402">System requires initial setup - no users exist</response>
    [AllowAnonymous]
    [Route("Login")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<String?>> Login([FromBody] AuthControllerLoginRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        var user = await authentication.GetUser();

        if (user == null)
        {
            return Problem(detail: "Setup required", statusCode: StatusCodes.Status402PaymentRequired);
        }

        if (String.IsNullOrEmpty(request.UserName) || String.IsNullOrEmpty(request.Password))
        {
            return Problem(detail: "Invalid credentials", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await authentication.Login(request.UserName, request.Password);

        if (!result.Succeeded)
        {
            return Problem(detail: "Invalid credentials", statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok();
    }

    /// <summary>
    /// Ends the current authenticated session
    /// </summary>
    /// <response code="200">Session terminated successfully</response>
    [Route("Logout")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Logout()
    {
        await authentication.Logout();

        return Ok();
    }

    /// <summary>
    /// Updates the authenticated user's password
    /// </summary>
    /// <remarks>
    /// Requires authentication. Updates the password for the currently logged in user.
    /// </remarks>
    /// <param name="request">Password update request containing the new password</param>
    /// <response code="200">Password updated successfully</response>
    /// <response code="400">Invalid request or password validation failed</response>
    /// <response code="401">Unauthorized - user must be authenticated</response>
    [Route("Update")]
    [HttpPost]
    [Authorize(Policy = "AuthSetting")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<String?>> Update([FromBody] AuthControllerUpdateRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.UserName) || String.IsNullOrEmpty(request.Password))
        {
            return Problem(detail: "Invalid UserName or Password", statusCode: StatusCodes.Status400BadRequest);
        }

        var updateResult = await authentication.Update(request.UserName, request.Password);

        if (!updateResult.Succeeded)
        {
            return Problem(detail: updateResult.Errors.First().Description, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok();
    }
}

/// <summary>
/// Request model for login and user creation operations
/// </summary>
public class AuthControllerLoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    /// <example>admin</example>
    [Required]
    public String? UserName { get; set; }

    /// <summary>
    /// Password for authentication
    /// </summary>
    /// <example>MySecurePassword123!</example>
    [Required]
    public String? Password { get; set; }
}

/// <summary>
/// Request model for configuring the provider settings
/// </summary>
public class AuthControllerSetupProviderRequest
{
    /// <summary>
    /// Provider type identifier
    /// </summary>
    /// <example>1</example>
    [Required]
    public Int32 Provider { get; set; }

    /// <summary>
    /// API token or key for the provider
    /// </summary>
    /// <example>sk-1234567890abcdef</example>
    [Required]
    public String? Token { get; set; }
}

/// <summary>
/// Request model for updating user password
/// </summary>
public class AuthControllerUpdateRequest
{
    /// <summary>
    /// Username for the account
    /// </summary>
    /// <example>myusername</example>
    [Required]
    public String? UserName { get; set; }

    /// <summary>
    /// New password for the user account
    /// </summary>
    /// <example>NewSecurePassword123!</example>
    [Required]
    public String? Password { get; set; }
}
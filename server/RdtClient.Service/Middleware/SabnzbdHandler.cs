using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RdtClient.Data.Enums;
using RdtClient.Service.Services;

namespace RdtClient.Service.Middleware;

public class SabnzbdRequirement : IAuthorizationRequirement
{
}

public class SabnzbdHandler(Authentication authentication, IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<SabnzbdRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SabnzbdRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
            return;
        }

        if (Settings.Get.General.AuthenticationType == AuthenticationType.None)
        {
            context.Succeed(requirement);
            return;
        }

        if (httpContext != null)
        {
            var request = httpContext.Request;

            String? GetParam(String name)
            {
                var value = request.Query[name].ToString();
                if (String.IsNullOrWhiteSpace(value) && request.HasFormContentType)
                {
                    value = request.Form[name].ToString();
                }

                return value;
            }

            var maUsername = GetParam("ma_username");
            var maPassword = GetParam("ma_password");

            if (!String.IsNullOrWhiteSpace(maUsername) && !String.IsNullOrWhiteSpace(maPassword))
            {
                var loginResult = await authentication.Login(maUsername, maPassword);
                if (loginResult.Succeeded)
                {
                    context.Succeed(requirement);
                    return;
                }

                // Invalid credentials provided
                context.Fail();
                return;
            }

            // Authentication required but missing credentials
            context.Fail();
            return;
        }
    }
}

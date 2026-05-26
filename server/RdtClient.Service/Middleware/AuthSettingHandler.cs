using Microsoft.AspNetCore.Authorization;
using RdtClient.Data.Enums;
using RdtClient.Service.Services;

namespace RdtClient.Service.Middleware;

public class AuthSettingRequirement : IAuthorizationRequirement
{
}

public class AuthSettingHandler(ISettings settings) : AuthorizationHandler<AuthSettingRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthSettingRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }

        if (settings.Current.General.AuthenticationType == AuthenticationType.None)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

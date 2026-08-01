using Microsoft.AspNetCore.Http;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Middleware;

/// <summary>
///     Adds the response headers that are safe to send unconditionally. Headers that can break existing deployments
///     (X-Frame-Options for dashboard embedding, HSTS for reverse-proxied HTTP) are opt-in through appsettings.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, AppSettings appSettings)
{
    public async Task Invoke(HttpContext context)
    {
        // Set before calling the rest of the pipeline: once the response has started the headers are immutable.
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";

        var frameOptions = appSettings.Security?.FrameOptions;

        if (!String.IsNullOrWhiteSpace(frameOptions))
        {
            headers["X-Frame-Options"] = frameOptions;
        }

        await next(context);
    }
}

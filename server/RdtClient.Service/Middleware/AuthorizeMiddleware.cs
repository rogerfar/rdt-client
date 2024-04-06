using System.Net;
using Microsoft.AspNetCore.Http;

namespace RdtClient.Service.Middleware;

public class AuthorizeMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Return a 403 instead of a 401, it's quirk that QBittorrent has.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        await next(context);

        if (context.Response.StatusCode == (Int32) HttpStatusCode.Unauthorized)
        {
            context.Response.StatusCode = (Int32) HttpStatusCode.Forbidden;
        }
    }
}
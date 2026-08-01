using Microsoft.AspNetCore.Http;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Middleware;

namespace RdtClient.Web.Test.Middleware;

public class SecurityHeadersMiddlewareTest
{
    private static async Task<HttpContext> InvokeAsync(AppSettings appSettings)
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, appSettings);

        await middleware.Invoke(context);

        return context;
    }

    [Fact]
    public async Task Invoke_SetsSafeHeadersUnconditionally()
    {
        var context = await InvokeAsync(new());

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"].ToString());
    }

    [Fact]
    public async Task Invoke_OmitsFrameOptionsByDefault()
    {
        // Sending X-Frame-Options by default would break users embedding the UI in Organizr or Heimdall.
        var context = await InvokeAsync(new());

        Assert.False(context.Response.Headers.ContainsKey("X-Frame-Options"));
    }

    [Fact]
    public async Task Invoke_SetsFrameOptionsWhenConfigured()
    {
        var context = await InvokeAsync(new()
        {
            Security = new()
            {
                FrameOptions = "SAMEORIGIN"
            }
        });

        Assert.Equal("SAMEORIGIN", context.Response.Headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task Invoke_OmitsFrameOptionsWhenConfiguredEmpty()
    {
        var context = await InvokeAsync(new()
        {
            Security = new()
            {
                FrameOptions = "   "
            }
        });

        Assert.False(context.Response.Headers.ContainsKey("X-Frame-Options"));
    }
}

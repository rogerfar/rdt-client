using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Service.Middleware;

public class BaseHrefMiddleware
{
    private readonly RequestDelegate _next;

    public BaseHrefMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppSettings appSettings)
    {
        var originalBody = context.Response.Body;

        try
        {
            using var newBody = new MemoryStream();

            context.Response.Body = newBody;

            await _next(context);

            context.Response.Body = originalBody;
            newBody.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(newBody).ReadToEndAsync();

            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            if (context.Response.ContentType?.Contains("text/html") == true)
            {
                var basePath = $"/{appSettings.BasePath!.TrimStart('/').TrimEnd('/')}/";
                
                responseBody = Regex.Replace(responseBody, @"<base href=""/""", @$"<base href=""{basePath}""");
                
                responseBody = Regex.Replace(responseBody, "(<script.*?src=\")(.*?)(\".*?</script>)", $"$1{basePath}$2$3");
                responseBody = Regex.Replace(responseBody, "(<link.*?href=\")(.*?)(\".*?>)", $"$1{basePath}$2$3");

                context.Response.Headers.Remove("Content-Length");
                await context.Response.WriteAsync(responseBody);
            }
            else
            {
                await context.Response.BodyWriter.WriteAsync(newBody.ToArray());
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
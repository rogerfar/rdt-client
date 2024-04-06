using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace RdtClient.Service.Middleware;

public class BaseHrefMiddleware(RequestDelegate next, String basePath)
{
    private readonly String _basePath = $"/{basePath.TrimStart('/').TrimEnd('/')}/";

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBody = context.Response.Body;

        try
        {
            using var newBody = new MemoryStream();

            context.Response.Body = newBody;

            await next(context);

            context.Response.Body = originalBody;
            newBody.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(newBody).ReadToEndAsync();

            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            if (context.Response.ContentType?.Contains("text/html") == true)
            {
                responseBody = Regex.Replace(responseBody, @"<base href=""/""", @$"<base href=""{_basePath}""");
                
                responseBody = Regex.Replace(responseBody, "(<script.*?src=\")(.*?)(\".*?</script>)", $"$1{_basePath}$2$3");
                responseBody = Regex.Replace(responseBody, "(<link.*?href=\")(.*?)(\".*?>)", $"$1{_basePath}$2$3");

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
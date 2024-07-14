using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace RdtClient.Service.Middleware;

public partial class BaseHrefMiddleware(RequestDelegate next, String basePath)
{
    [GeneratedRegex(@"<base href=""/""")]
    private partial Regex BodyRegex();

    [GeneratedRegex("(<script.*?src=\")(.*?)(\".*?</script>)")]
    private partial Regex ScriptRegex();

    [GeneratedRegex("(<link.*?href=\")(.*?)(\".*?>)")]
    private partial Regex LinkRegex();

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

            if (context.Response.StatusCode == 200)
            {
                if (context.Response.ContentType?.Contains("text/html") == true)
                {
                    responseBody = BodyRegex().Replace(responseBody, @$"<base href=""{_basePath}""");

                    responseBody = ScriptRegex().Replace(responseBody, $"$1{_basePath}$2$3");
                    responseBody = LinkRegex().Replace(responseBody, $"$1{_basePath}$2$3");

                    context.Response.Headers.Remove("Content-Length");
                    await context.Response.WriteAsync(responseBody);
                }
                else
                {
                    await context.Response.BodyWriter.WriteAsync(newBody.ToArray());
                }
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
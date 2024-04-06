using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace RdtClient.Service.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RequestLoggingMiddleware>();

    public async Task Invoke(HttpContext context)
    {
        if (!_logger.IsEnabled(LogLevel.Debug) || (!context.Request.Path.StartsWithSegments("/api/v2") && !context.Request.Path.StartsWithSegments("/api/torrents")))
        {
            await next(context);

            return;
        }

        var requestLog = $"Method: {context.Request.Method}, Path: {context.Request.Path}";

        if (context.Request.QueryString.HasValue)
        {
            requestLog += $", QueryString: {context.Request.QueryString}";
        }

        if (context.Request.HasFormContentType && context.Request.Form.Count > 0)
        {
            requestLog += $", Form: {String.Join(", ", context.Request.Form.Select(f => $"{f.Key}: {f.Value}"))}";
        }
        else if (context.Request.ContentType?.Contains("application/json", StringComparison.CurrentCultureIgnoreCase) == true)
        {
            var body = await ReadRequestBodyAsync(context.Request);
            requestLog += $", Body: {body}";
        }

        _logger.LogDebug(requestLog);

        await next(context);
    }

    private static async Task<String> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        return body;
    }
}

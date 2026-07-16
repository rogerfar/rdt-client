using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Services;
using RdtClient.Service.Services.DebridClients;

namespace RdtClient.Service.Test.Services.TorrentClients;

public class PremiumizeDebridClientTest
{
    private readonly Mock<IDownloadableFileFilter> _fileFilterMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<PremiumizeDebridClient>> _loggerMock;
    private readonly TestSettings _settings;

    public PremiumizeDebridClientTest()
    {
        _loggerMock = new();
        _httpClientFactoryMock = new();
        _fileFilterMock = new();
        _settings = new();
        _settings.Current.Provider.ApiKey = "test-api-key";
    }

    [Fact]
    public async Task AddNzbLink_UsesPremiumizeNetTransferCreate_ReturnsTransferId()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""{"status":"success","id":"transfer-id","name":"test.nzb"}"""));
        var client = CreateClient(handler);

        // Act
        var result = await client.AddNzbLink("http://example.com/test.nzb");

        // Assert
        Assert.Equal("transfer-id", result);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://www.premiumize.me/api/transfer/create?apikey=test-api-key", handler.Request.RequestUri!.ToString());
        Assert.Null(handler.Request.Headers.Authorization);
        Assert.Equal("application/x-www-form-urlencoded", handler.Request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("src=http%3A%2F%2Fexample.com%2Ftest.nzb&folder_id=", handler.RequestBody);
    }

    [Fact]
    public async Task AddNzbFile_SubmitsMultipartTransferCreateRequest_ReturnsTransferId()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""{"status":"success","id":"file-transfer-id","name":"test.nzb"}"""));
        var client = CreateClient(handler);

        // Act
        var result = await client.AddNzbFile(Encoding.UTF8.GetBytes("nzb body"), "test.nzb");

        // Assert
        Assert.Equal("file-transfer-id", result);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://www.premiumize.me/api/transfer/create", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("test-api-key", handler.Request.Headers.Authorization.Parameter);
        Assert.Equal("multipart/form-data", handler.Request.Content!.Headers.ContentType!.MediaType);
        Assert.Contains("Content-Disposition: form-data", handler.RequestBody);
        Assert.Contains("src", handler.RequestBody);
        Assert.Contains("test.nzb", handler.RequestBody);
        Assert.Contains("nzb body", handler.RequestBody);
    }

    [Fact]
    public async Task AddNzbFile_WhenNameIsMissing_UsesDefaultFilename()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""{"status":"success","id":"file-transfer-id","name":"upload.nzb"}"""));
        var client = CreateClient(handler);

        // Act
        await client.AddNzbFile(Encoding.UTF8.GetBytes("nzb body"), null);

        // Assert
        Assert.Contains("upload.nzb", handler.RequestBody);
    }

    [Fact]
    public async Task AddNzbFile_WhenNameHasNoNzbExtension_AppendsNzbExtension()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""{"status":"success","id":"file-transfer-id","name":"release-title.nzb"}"""));
        var client = CreateClient(handler);

        // Act
        await client.AddNzbFile(Encoding.UTF8.GetBytes("nzb body"), "release-title");

        // Assert
        Assert.Contains("release-title.nzb", handler.RequestBody);
    }

    [Fact]
    public async Task AddNzbLink_WhenSuccessResponseHasNoId_Throws()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""{"status":"success","name":"test.nzb"}"""));
        var client = CreateClient(handler);

        // Act
        var ex = await Assert.ThrowsAsync<Exception>(() => client.AddNzbLink("http://example.com/test.nzb"));

        // Assert
        Assert.Contains("Unable to add NZB link", ex.Message);
    }

    [Fact]
    public async Task AddNzbLink_WhenPremiumizeReturnsRateLimitError_ThrowsRateLimitException()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""{"status":"error","code":"slow_down","message":"rate limit exceeded"}"""));
        var client = CreateClient(handler);

        // Act
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.AddNzbLink("http://example.com/test.nzb"));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(2), ex.RetryAfter);
    }

    [Theory]
    [InlineData("You've made too many API requests too quickly.")]
    [InlineData("Your fair-use points, booster points, or active-job count is exhausted.")]
    [InlineData("This account's usage limit for this service has been reached.")]
    [InlineData("The target service is unreachable right now. Retry after a delay.")]
    public async Task AddNzbLink_WhenPremiumizeNetReturnsDocumentedRetryMessage_ThrowsRateLimitException(String message)
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse($$"""{"status":"error","message":"{{message}}","code":"rate_limit_reached"}"""));
        var client = CreateClient(handler);

        // Act
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.AddNzbLink("http://example.com/test.nzb"));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(2), ex.RetryAfter);
    }

    [Theory]
    [InlineData("rate_limit_reached")]
    [InlineData("account_limit_reached")]
    [InlineData("service_limit_reached")]
    [InlineData("service_down")]
    [InlineData("semi_permanent_error")]
    public async Task AddNzbFile_WhenPremiumizeReturnsDocumentedRetryCode_ThrowsRateLimitException(String code)
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse($$"""{"status":"error","code":"{{code}}","message":"retry later"}"""));
        var client = CreateClient(handler);

        // Act
        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.AddNzbFile(Encoding.UTF8.GetBytes("nzb body"), "test.nzb"));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(2), ex.RetryAfter);
    }

    [Fact]
    public async Task AddTorrentMagnet_UsesPremiumizeNetTransferCreate_ReturnsTransferId()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""{"status":"success","id":"magnet-transfer-id","name":"test"}"""));
        var client = CreateClient(handler);

        // Act
        var result = await client.AddTorrentMagnet("magnet:?xt=urn:btih:test");

        // Assert
        Assert.Equal("magnet-transfer-id", result);
        Assert.Equal("https://www.premiumize.me/api/transfer/create?apikey=test-api-key", handler.Request!.RequestUri!.ToString());
        Assert.Equal("src=magnet%3A%3Fxt%3Durn%3Abtih%3Atest&folder_id=", handler.RequestBody);
    }

    [Fact]
    public async Task AddTorrentFile_UsesPremiumizeNetFileUpload_ReturnsTransferId()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""{"status":"success","id":"torrent-transfer-id","name":"test"}"""));
        var client = CreateClient(handler);

        // Act
        var result = await client.AddTorrentFile(Encoding.UTF8.GetBytes("torrent body"));

        // Assert
        Assert.Equal("torrent-transfer-id", result);
        Assert.Equal("https://www.premiumize.me/api/transfer/create?apikey=test-api-key", handler.Request!.RequestUri!.ToString());
        Assert.Contains("name=file", handler.RequestBody);
        Assert.Contains("filename=1.torrent", handler.RequestBody);
        Assert.Contains("application/x-bittorrent", handler.RequestBody);
        Assert.Contains("torrent body", handler.RequestBody);
    }

    [Fact]
    public async Task GetAvailableFiles_WhenHashCached_ReturnsTorrentLevelEntry()
    {
        // Arrange - response shape verified live 2026-07-16: filesize is a JSON *string* on hits
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(
            """{"status":"success","response":[true],"transcoded":[true],"filename":["The Matrix (1999) [1080p]"],"filesize":["276445467"]}"""));
        var client = CreateClient(handler);

        // Act
        var result = await client.GetAvailableFiles("d7a46713eaee18c746b3254b7d1492a50fd9d6ce");

        // Assert - premiumize reports cache status per torrent, not per file: one entry on a hit
        Assert.Single(result);
        Assert.Equal("The Matrix (1999) [1080p]", result[0].Filename);
        Assert.Equal(276445467, result[0].Filesize);
        Assert.Contains("cache/check", handler.Request!.RequestUri!.ToString());
        var requestText = handler.Request.RequestUri.ToString() + handler.RequestBody;
        Assert.Contains("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", requestText);
    }

    [Fact]
    public async Task GetAvailableFiles_WhenHashNotCached_ReturnsEmpty()
    {
        // Arrange - on a miss filesize is the *number* 0 (verified live)
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(
            """{"status":"success","response":[false],"transcoded":[null],"filename":[null],"filesize":[0]}"""));
        var client = CreateClient(handler);

        // Act
        var result = await client.GetAvailableFiles("0000000000000000000000000000000000000000");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableFiles_WhenCachedWithoutFilename_ReturnsPlaceholderEntry()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(
            """{"status":"success","response":[true],"transcoded":[true],"filename":[""],"filesize":[0]}"""));
        var client = CreateClient(handler);

        // Act
        var result = await client.GetAvailableFiles("d7a46713eaee18c746b3254b7d1492a50fd9d6ce");

        // Assert - cached is cached even without detail; report availability under the hash
        Assert.Single(result);
        Assert.Equal("d7a46713eaee18c746b3254b7d1492a50fd9d6ce", result[0].Filename);
    }

    private PremiumizeDebridClient CreateClient(RecordingHttpMessageHandler handler)
    {
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<String>())).Returns(new HttpClient(handler));

        return new(_loggerMock.Object, _httpClientFactoryMock.Object, _fileFilterMock.Object, _settings);
    }

    private static HttpResponseMessage JsonResponse(String json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public String? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return handler(request);
        }
    }
}

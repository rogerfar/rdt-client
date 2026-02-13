using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Models.Sabnzbd;
using RdtClient.Service.Services;
using RdtClient.Web.Controllers;

namespace RdtClient.Web.Test.Controllers;

public class SabnzbdControllerTest
{
    private readonly Mock<Sabnzbd> _sabnzbdMock;
    private readonly Mock<Authentication> _authenticationMock;
    private readonly SabnzbdController _controller;

    public SabnzbdControllerTest()
    {
        Data.Data.SettingData.Get.General.AuthenticationType = Data.Enums.AuthenticationType.None;
        Data.Data.SettingData.Get.Provider.ApiKey = "test-api-key";

        var torrentsMock = new Mock<Torrents>(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        var sabnzbdLoggerMock = new Mock<ILogger<Sabnzbd>>();
        _sabnzbdMock = new Mock<Sabnzbd>(sabnzbdLoggerMock.Object, torrentsMock.Object, null!);
        var loggerMock = new Mock<ILogger<SabnzbdController>>();
        _authenticationMock = new Mock<Authentication>(null!, null!, null!);
        
        _controller = new SabnzbdController(loggerMock.Object, _sabnzbdMock.Object);
        
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetQueue_ReturnsOk()
    {
        // Arrange
        var queue = new SabnzbdQueue { NoOfSlots = 1 };
        _sabnzbdMock.Setup(s => s.GetQueue()).ReturnsAsync(queue);

        // Act
        var result = await _controller.Queue();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SabnzbdResponse>(okResult.Value);
        Assert.NotNull(response.Queue);
        Assert.Equal(1, response.Queue.NoOfSlots);
    }

    [Fact]
    public async Task GetQueue_Unauthorized_ReturnsOk_BecauseFilterIsSkippedInUnitTests()
    {
        // Arrange
        Data.Data.SettingData.Get.General.AuthenticationType = Data.Enums.AuthenticationType.UserNamePassword;
        var queue = new SabnzbdQueue { NoOfSlots = 1 };
        _sabnzbdMock.Setup(s => s.GetQueue()).ReturnsAsync(queue);
        
        // Act
        var result = await _controller.Queue();

        // Assert
        // In unit tests, filters are not executed. 
        // We test the filter separately in SabnzbdAuthFilterTest.cs
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetQueue_WithMaAuth_ReturnsOk()
    {
        // Arrange
        Data.Data.SettingData.Get.General.AuthenticationType = Data.Enums.AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?ma_username=user&ma_password=pass");
        _controller.ControllerContext.HttpContext = httpContext;
        
        _authenticationMock.Setup(a => a.Login("user", "pass")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        
        var queue = new SabnzbdQueue { NoOfSlots = 1 };
        _sabnzbdMock.Setup(s => s.GetQueue()).ReturnsAsync(queue);

        // Act
        var result = await _controller.Queue();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetHistory_ReturnsOk()
    {
        // Arrange
        var history = new SabnzbdHistory { NoOfSlots = 1 };
        _sabnzbdMock.Setup(s => s.GetHistory()).ReturnsAsync(history);

        // Act
        var result = await _controller.History();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SabnzbdResponse>(okResult.Value);
        Assert.NotNull(response.History);
        Assert.Equal(1, response.History.NoOfSlots);
    }

    [Fact]
    public void GetVersion_HasAllowAnonymousAttribute()
    {
        // Arrange
        var type = typeof(SabnzbdController);
        var method = type.GetMethod(nameof(SabnzbdController.Version));

        // Act
        var attribute = method?.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void GetVersion_ReturnsOk()
    {
        // Arrange
        Data.Data.SettingData.Get.General.AuthenticationType = Data.Enums.AuthenticationType.UserNamePassword;

        // Act
        var result = _controller.Version();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SabnzbdResponse>(okResult.Value);
        Assert.Equal("4.4.0", response.Version);
    }

    [Fact]
    public void GetConfig_ReturnsOk()
    {
        // Arrange
        var config = new SabnzbdConfig { Misc = new SabnzbdMisc { Port = "6500" } };
        _sabnzbdMock.Setup(s => s.GetConfig()).Returns(config);

        // Act
        var result = _controller.GetConfig();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SabnzbdResponse>(okResult.Value);
        Assert.NotNull(response.Config);
        Assert.Equal("6500", response.Config.Misc.Port);
    }

    [Fact]
    public void GetCategories_ReturnsOk()
    {
        // Arrange
        var categories = new List<String> { "*", "Default" };
        _sabnzbdMock.Setup(s => s.GetCategories()).Returns(categories);

        // Act
        var result = _controller.GetCats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SabnzbdResponse>(okResult.Value);
        Assert.NotNull(response.Categories);
        Assert.Equal(2, response.Categories.Count);
        Assert.Equal("*", response.Categories[0]);
    }

    [Fact]
    public void Get_NoMode_ReturnsBadRequest()
    {
        // Act
        var result = _controller.Get(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<SabnzbdResponse>(badRequestResult.Value);
        Assert.Equal("No mode specified", response.Error);
    }

    [Fact]
    public void Get_UnknownMode_ReturnsNotFound()
    {
        // Act
        var result = _controller.Get("unknown");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Get_ModeInForm_ReturnsNotFound()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<String, Microsoft.Extensions.Primitives.StringValues>
        {
            { "mode", "unknown_form" }
        });
        _controller.ControllerContext.HttpContext = httpContext;

        // Act
        var result = _controller.Get(null);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddFile_WithQueryParameters_SetsCategoryAndPriority()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.QueryString = new QueryString("?cat=radarr&priority=-100");
        
        // Mocking multipart form data
        var fileMock = new Mock<IFormFile>();
        var content = "test content";
        var fileName = "test.nzb";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;
        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((s, _) => ms.CopyTo(s))
                .Returns(Task.CompletedTask);

        httpContext.Request.ContentType = "multipart/form-data; boundary=something";
        httpContext.Request.Form = new FormCollection(new Dictionary<String, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection { fileMock.Object });
        
        _controller.ControllerContext.HttpContext = httpContext;
        _sabnzbdMock.Setup(s => s.AddFile(It.IsAny<Byte[]>(), fileName, "radarr", -100)).ReturnsAsync("nzo_id_123");

        // Act
        var result = await _controller.AddFile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SabnzbdResponse>(okResult.Value);
        Assert.True(response.Status);
        Assert.Contains("nzo_id_123", response.NzoIds!);
        _sabnzbdMock.Verify(s => s.AddFile(It.IsAny<Byte[]>(), fileName, "radarr", -100), Times.Once);
    }
}

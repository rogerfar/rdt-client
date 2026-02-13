using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using RdtClient.Data.Enums;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;

namespace RdtClient.Web.Test.Controllers;

public class SabnzbdHandlerTest
{
    private readonly Mock<Authentication> _authenticationMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly SabnzbdHandler _handler;

    public SabnzbdHandlerTest()
    {
        _authenticationMock = new Mock<Authentication>(null!, null!, null!);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _handler = new SabnzbdHandler(_authenticationMock.Object, _httpContextAccessorMock.Object);
        Data.Data.SettingData.Get.General.AuthenticationType = AuthenticationType.UserNamePassword;
    }

    [Fact]
    public async Task HandleAsync_AuthNone_Succeeds()
    {
        // Arrange
        Data.Data.SettingData.Get.General.AuthenticationType = AuthenticationType.None;
        var context = CreateContext();

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded, "HasSucceeded should be true because AuthenticationType is None");
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_Succeeds()
    {
        // Arrange
        Settings.Get.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?ma_username=user&ma_password=pass");
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        
        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "pass")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded, "HasSucceeded should be true for valid credentials");
    }

    [Fact]
    public async Task HandleAsync_AlreadyAuthenticated_Succeeds()
    {
        // Arrange
        Settings.Get.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext();
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity("TestAuth"));
        httpContext.User = claimsPrincipal;
        
        var context = CreateContext(httpContext);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded, "HasSucceeded should be true for already authenticated user");
        _authenticationMock.Verify(a => a.Login(It.IsAny<String>(), It.IsAny<String>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidCredentials_DoesNotSucceed()
    {
        // Arrange
        Settings.Get.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?ma_username=user&ma_password=wrong");
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        
        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "wrong")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded, "HasSucceeded should be false for invalid credentials");
    }

    [Fact]
    public async Task HandleAsync_MissingCredentials_DoesNotSucceed()
    {
        // Arrange
        Settings.Get.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        
        var context = CreateContext(httpContext);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded, "HasSucceeded should be false when credentials are missing");
    }

    private AuthorizationHandlerContext CreateContext(HttpContext? httpContext = null)
    {
        var requirement = new SabnzbdRequirement();
        var user = httpContext?.User ?? new System.Security.Claims.ClaimsPrincipal();
        return new AuthorizationHandlerContext([requirement], user, null);
    }
}

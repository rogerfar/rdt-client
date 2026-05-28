using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using RdtClient.Data.Enums;
using RdtClient.Service.Middleware;
using RdtClient.Service.Services;

namespace RdtClient.Web.Test.Controllers;

public class SabnzbdHandlerTest
{
    private readonly Mock<Authentication> _authenticationMock;
    private readonly SabnzbdHandler _handler;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly TestSettings _settings;

    public SabnzbdHandlerTest()
    {
        _authenticationMock = new(null!, null!, null!);
        _httpContextAccessorMock = new();
        _settings = new();
        _handler = new(_authenticationMock.Object, _httpContextAccessorMock.Object, _settings);
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
    }

    [Fact]
    public async Task HandleAsync_AuthNone_Succeeds()
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.None;
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
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new("?ma_username=user&ma_password=pass")
            }
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "pass")).ReturnsAsync(SignInResult.Success);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded, "HasSucceeded should be true for valid credentials");
    }

    [Fact]
    public async Task HandleAsync_ValidApiKeyCredentials_Succeeds()
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new("?apikey=user:pass")
            }
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "pass")).ReturnsAsync(SignInResult.Success);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded, "HasSucceeded should be true for valid api key credentials");
    }

    [Fact]
    public async Task HandleAsync_ValidApiKeyCredentialsFromForm_Succeeds()
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new()
        {
            {
                "apikey", "user:pass"
            }
        });

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "pass")).ReturnsAsync(SignInResult.Success);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded, "HasSucceeded should be true for valid form api key credentials");
    }

    [Fact]
    public async Task HandleAsync_ApiKeyPasswordContainingColon_Succeeds()
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new("?apikey=user:pass:with:colons")
            }
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "pass:with:colons")).ReturnsAsync(SignInResult.Success);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded, "HasSucceeded should be true when the password contains colons");
    }

    [Fact]
    public async Task HandleAsync_MaCredentialsTakePrecedenceOverApiKey()
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new("?ma_username=user&ma_password=wrong&apikey=user:pass")
            }
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "wrong")).ReturnsAsync(SignInResult.Failed);
        _authenticationMock.Setup(a => a.Login("user", "pass")).ReturnsAsync(SignInResult.Success);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded, "HasSucceeded should be false because ma_username/ma_password take precedence");
        _authenticationMock.Verify(a => a.Login("user", "wrong"), Times.Once);
        _authenticationMock.Verify(a => a.Login("user", "pass"), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyAuthenticated_Succeeds()
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity("TestAuth"));
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
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new("?ma_username=user&ma_password=wrong")
            }
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "wrong")).ReturnsAsync(SignInResult.Failed);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded, "HasSucceeded should be false for invalid credentials");
    }

    [Fact]
    public async Task HandleAsync_InvalidApiKeyCredentials_DoesNotSucceed()
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new("?apikey=user:wrong")
            }
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = CreateContext(httpContext);
        _authenticationMock.Setup(a => a.Login("user", "wrong")).ReturnsAsync(SignInResult.Failed);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded, "HasSucceeded should be false for invalid api key credentials");
    }

    [Theory]
    [InlineData("missingdelimiter")]
    [InlineData(":pass")]
    [InlineData("user:")]
    public async Task HandleAsync_MalformedApiKey_DoesNotSucceed(String apiKey)
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new($"?apikey={apiKey}")
            }
        };

        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = CreateContext(httpContext);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded, "HasSucceeded should be false for malformed api keys");
        _authenticationMock.Verify(a => a.Login(It.IsAny<String>(), It.IsAny<String>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MissingCredentials_DoesNotSucceed()
    {
        // Arrange
        _settings.Current.General.AuthenticationType = AuthenticationType.UserNamePassword;
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
        var user = httpContext?.User ?? new ClaimsPrincipal();

        return new([requirement], user, null);
    }
}

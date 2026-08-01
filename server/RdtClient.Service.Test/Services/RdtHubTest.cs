using Microsoft.AspNetCore.Authorization;
using RdtClient.Service.Services;

namespace RdtClient.Service.Test.Services;

public class RdtHubTest
{
    [Fact]
    public void RdtHub_RequiresTheAuthSettingPolicy()
    {
        // The hub broadcasts the full torrent list to every connected client. Without this attribute anyone able to reach
        // the port can subscribe to /hub anonymously and receive it, bypassing authentication entirely.
        var attribute = typeof(RdtHub).GetCustomAttributes(typeof(AuthorizeAttribute), true)
                                      .Cast<AuthorizeAttribute>()
                                      .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("AuthSetting", attribute.Policy);
    }
}

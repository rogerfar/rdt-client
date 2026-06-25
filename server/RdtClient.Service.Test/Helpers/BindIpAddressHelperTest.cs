using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class BindIpAddressHelperTest
{
    [Fact]
    public void TryResolve_WhenDisabled_ReturnsNull()
    {
        // Act
        var result = BindIpAddressHelper.TryResolve(false, "127.0.0.1");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-ip")]
    public void TryResolve_WhenEnabledWithEmptyOrInvalid_ReturnsNull(String? configuredAddress)
    {
        // Act
        var result = BindIpAddressHelper.TryResolve(true, configuredAddress);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryResolve_WhenEnabledWithNonLocalIp_ReturnsNull()
    {
        // Act
        var result = BindIpAddressHelper.TryResolve(true, "8.8.8.8");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryResolve_WhenEnabledWithLoopback_ReturnsLoopback()
    {
        // Act
        var result = BindIpAddressHelper.TryResolve(true, "127.0.0.1");

        // Assert
        Assert.Equal(IPAddress.Loopback, result);
    }

    [Fact]
    public void TryResolve_WhenEnabledWithLocalInterfaceIp_ReturnsAddress()
    {
        // Arrange
        var localIpAddress = GetFirstLocalIpv4Address();

        if (localIpAddress == null)
        {
            return;
        }

        // Act
        var result = BindIpAddressHelper.TryResolve(true, localIpAddress.ToString());

        // Assert
        Assert.Equal(localIpAddress, result);
    }

    [Fact]
    public void IsLocalUnicastAddress_WithPublicIp_ReturnsFalse()
    {
        // Act
        var result = BindIpAddressHelper.IsLocalUnicastAddress(IPAddress.Parse("8.8.8.8"));

        // Assert
        Assert.False(result);
    }

    private static IPAddress? GetFirstLocalIpv4Address()
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            foreach (var unicast in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicast.Address))
                {
                    return unicast.Address;
                }
            }
        }

        return null;
    }
}

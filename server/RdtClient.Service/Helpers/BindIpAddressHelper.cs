using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RdtClient.Service.Helpers;

public static class BindIpAddressHelper
{
    public static IPAddress? TryResolve(Boolean bindToSpecificIp, String? configuredAddress)
    {
        if (!bindToSpecificIp)
        {
            return null;
        }

        if (String.IsNullOrWhiteSpace(configuredAddress))
        {
            return null;
        }

        if (!IPAddress.TryParse(configuredAddress, out var bindIpAddress))
        {
            return null;
        }

        if (!IsLocalUnicastAddress(bindIpAddress))
        {
            return null;
        }

        return bindIpAddress;
    }

    public static Boolean IsLocalUnicastAddress(IPAddress address)
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            foreach (var unicast in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.Equals(address))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

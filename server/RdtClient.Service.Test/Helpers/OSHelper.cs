using System.Runtime.InteropServices;

namespace RdtClient.Service.Test.Helpers;

public static class OSHelper
{
    public static Boolean IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}

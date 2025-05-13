using System;
using System.Runtime.InteropServices;

namespace CliInvoke.Specializations.Tests.Invokers;

#if NET472_OR_GREATER
using OperatingSystem = OperatingSystemPolyfill;
#endif

public class CmdInvokerTests
{
    
    [Fact]
    public void Invoke_Echo()
    {
        if (OperatingSystem.IsWindows())
        {
            RuntimeInformation.IsOSPlatform()
        }
    }
}
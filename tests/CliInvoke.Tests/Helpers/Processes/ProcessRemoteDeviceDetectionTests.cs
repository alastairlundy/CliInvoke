using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Tests.Internal.Constants;
using AlastairLundy.CliInvoke.Tests.Internal.Helpers;
using AlastairLundy.CliInvoke.Helpers.Processes;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Helpers.Processes;

public class ProcessRemoteDeviceDetectionTests
{
    
    [Fact]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public async Task LocalProcess_Detected_Successfully()
    {
        string filePath = ProcessTestHelper.GetTargetFilePath();
        Process process = ProcessTestHelper.CreateProcess(filePath, "");

        //Act
        process.Start();
        
        bool actual = process.IsRunningOnRemoteDevice();
        
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        
        process.Dispose();
        
        //Assert
        Assert.False(actual);
    }
}
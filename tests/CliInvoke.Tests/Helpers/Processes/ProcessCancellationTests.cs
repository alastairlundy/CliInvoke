using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Helpers.Processes;
using AlastairLundy.CliInvoke.Tests.Internal.Constants;
using AlastairLundy.CliInvoke.Tests.Internal.Helpers;
using Xunit;

namespace AlastairLundy.CliInvoke.Tests.Helpers.Processes;

public class ProcessCancellationTests
{
    
    [Fact]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task ProcessCancelled_TimeSpanOnlyOverload_Delay_Success()
    {
        //Arrange 
        string filePath = ProcessTestHelper.GetTargetFilePath();
        Process process = ProcessTestHelper.CreateProcess(filePath, "");
        
        //Act
       
       process.Start();
       
       int processId = process.Id;
       
       await process.WaitForExitOrTimeoutAsync(TimeSpan.FromSeconds(2));

      await Task.Delay(1000, TestContext.Current.CancellationToken);

       bool actual = Process.GetProcesses().Any(x => x.Id == processId);

        //Assert
        Assert.False(actual);
    }
    
    [Fact]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task ProcessCancelled_Normal_Delay_DefaultTimeOutPolicy_Success()
    {
        //Arrange 
        string filePath = ProcessTestHelper.GetTargetFilePath();
        Process process = ProcessTestHelper.CreateProcess(filePath, "");
        
        //Act
       
        process.Start();
       
        int processId = process.Id;

        await process.WaitForExitOrTimeoutAsync(ProcessTimeoutPolicy.Default, CancellationToken.None);

        await Task.Delay(1000, TestContext.Current.CancellationToken);

        bool actual = Process.GetProcesses().Any(x => x.Id == processId);

        //Assert
        Assert.False(actual);
    }
    
    [Fact]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task ProcessCancelled_Normal_Delay_CancelAfter30Seconds_Success()
    {
        //Arrange 
        string filePath = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ? "/usr/bin/sleep" : "timeout";
        string args = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ? "120" : "/T 120 /NOBREAK";
        Process process = ProcessTestHelper.CreateProcess(filePath, args);
        
        //Act
        process.Start();
       
        int processId = process.Id;
        try
        {
            await process.WaitForExitOrTimeoutAsync(new ProcessTimeoutPolicy(TimeSpan.FromSeconds(30),
                ProcessCancellationMode.Graceful), CancellationToken.None);
        
            await Task.Delay(1000, TestContext.Current.CancellationToken);
            
            bool actual = Process.GetProcesses().Any(x => x.Id == processId);
            //Assert
            Assert.False(actual);
        }
        finally
        {
            process.Dispose();
        }
    }
}
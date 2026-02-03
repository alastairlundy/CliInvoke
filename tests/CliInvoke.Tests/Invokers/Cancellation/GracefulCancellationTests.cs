using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CliInvoke.Core;
using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes.Cancellation;
using CliInvoke.Tests.Internal.Helpers;
using Xunit;

namespace CliInvoke.Tests.Invokers.Cancellation;

public class GracefulCancellationTests
{
    [Fact]
    public async Task GracefulCancel_InterruptSignals_Success()
    {
        ProcessCancellationExceptionBehavior exceptionBehavior = ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected;

        int sleepTimeSeconds = 500;

        int gracefulTimeoutSeconds = 10;
        
        ProcessWrapper process;
        
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsAndroid() || OperatingSystem.IsFreeBSD())
        {
            process = ProcessTestHelper.CreateProcess("sleep", $"{sleepTimeSeconds}");
        }
        else if (OperatingSystem.IsWindows())
        {
            process = ProcessTestHelper.CreateProcess("timeout", $"/t {sleepTimeSeconds} /nobreak");
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
        
        Stopwatch stopwatch = new();
        process.Start();
        stopwatch.Start();
            
        await process.WaitForExitOrGracefulTimeoutAsync(TimeSpan.FromSeconds(gracefulTimeoutSeconds),
            exceptionBehavior, CancellationToken.None, false);
            
        stopwatch.Stop();

        long elapsedTimeSeconds = stopwatch.ElapsedMilliseconds / 1000;

        Assert.InRange(elapsedTimeSeconds, 0, Math.Min(gracefulTimeoutSeconds * 3, 60));
        
        Assert.True(process.HasExited);

        if (!process.HasExited) 
            process.Kill();
        
        process.Dispose();
    }
}
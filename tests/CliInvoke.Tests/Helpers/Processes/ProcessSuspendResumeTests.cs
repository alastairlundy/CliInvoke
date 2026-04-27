using System.Runtime.Versioning;
using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes;

namespace CliInvoke.Tests.Helpers.Processes;

public class ProcessSuspendResumeTests
{
    [Test]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task SuspendResume_Process_ShouldExitAfterResume()
    {
        int sleepTimeSeconds = 10;
        ProcessWrapper process;

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsAndroid() ||
            OperatingSystem.IsFreeBSD())
            process = ProcessTestHelper.CreateProcess("sleep", $"{sleepTimeSeconds}");
        else if (OperatingSystem.IsWindows())
            process = ProcessTestHelper.CreateProcess("timeout", $"/t {sleepTimeSeconds} /nobreak");
        else
            throw new PlatformNotSupportedException();

        try
        {
            process.Start();

            await Assert.That(process.HasStarted).IsTrue();

            process.SuspendProcess();
            // Allow suspend to take effect
            await Task.Delay(500);

            await Assert.That(process.HasExited).IsFalse();

            process.ResumeProcess();

            Task waitForExit = process.WaitForExitAsync(CancellationToken.None);
            Task completed = await Task.WhenAny(waitForExit, Task.Delay(TimeSpan.FromSeconds(sleepTimeSeconds + 20)));

            await Assert.That(process.HasExited).IsTrue();
        }
        finally
        {
            if (!process.HasExited)
            {
                try { process.Kill(true); } catch { process.Kill(); }
            }

            process.Dispose();
        }
    }
}

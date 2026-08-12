using System.Runtime.Versioning;
using CliInvoke.Processes.Internal;

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
        {
            // Windows 'timeout' exits immediately when any standard stream is redirected.
            // Disable output redirection so stdout/stderr stay connected to the console.
            ProcessConfiguration configuration = new ProcessConfiguration("timeout",
                $"/t {sleepTimeSeconds} /nobreak", outputRedirection: false);
            process = new ProcessWrapper(configuration, configuration.ResourcePolicy);
        }
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
            await Task.WhenAny(waitForExit, Task.Delay(TimeSpan.FromSeconds(sleepTimeSeconds + 20)));

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

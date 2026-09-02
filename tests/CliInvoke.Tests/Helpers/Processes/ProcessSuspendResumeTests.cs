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
            FilePathResolver resolver = new();
            FileInfo resolvedCmd = resolver.ResolveFilePath("cmd.exe");
            ProcessConfiguration configuration = new ProcessConfiguration(resolvedCmd.FullName,
                $"/c timeout /t {sleepTimeSeconds} /nobreak", outputRedirection: false);
            process = new ProcessWrapper(configuration, resolvedCmd);
        }
        else
            throw new PlatformNotSupportedException();

        try
        {
            process.Start();

            await Assert.That(process.HasStarted).IsTrue();

            process.SuspendProcess();

            await ProcessTestHelper.WaitForConditionAsync(
                () => !process.HasExited,
                TimeSpan.FromSeconds(3),
                failureMessage: "Process exited before suspend could take effect");

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

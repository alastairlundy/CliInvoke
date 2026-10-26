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
            // Windows 'timeout' exits immediately whenever stdin is redirected (which
            // the test runner does in CI), so the test would race a process that dies
            // on startup and lose. ping ignores stdin entirely and keeps running for
            // the full sleep window no matter how the runner wires its streams; cmd's
            // own >nul keeps its output out of the CI log.
            FilePathResolver resolver = new();
            FileInfo resolvedCmd = resolver.ResolveFilePath("cmd.exe");
            ProcessConfiguration configuration = new ProcessConfiguration(resolvedCmd.FullName,
                $"/c ping -n {sleepTimeSeconds + 1} 127.0.0.1 >nul", outputRedirection: false);
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
                failureMessage: "Process exited before suspend could take effect").ConfigureAwait(true);

            process.ResumeProcess();

            Task waitForExit = process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAny(waitForExit, Task.Delay(TimeSpan.FromSeconds(sleepTimeSeconds + 20))).ConfigureAwait(true);

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

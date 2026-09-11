using System.Linq;
using System.Runtime.Versioning;
using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Helpers.Processes;

public class ProcessCancellationTests
{
    [Test]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task ProcessCancelled_TimeSpanOnlyOverload_Delay_Graceful_Success()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-process-cancel-timespan-{Guid.NewGuid():N}.marker");

        const int timeoutSeconds = 3;
        const int ceilingSeconds = timeoutSeconds * 2;

        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 10);

        ProcessExitConfiguration processExitConfiguration = new(
            ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(timeoutSeconds)));

        process.Start();

        int processId = process.Id;

        try
        {
            Task waitTask = process.WaitForExitOrTimeoutAsync(processExitConfiguration, CancellationToken.None);
            Task ceiling = Task.Delay(TimeSpan.FromSeconds(ceilingSeconds));
            Task completed = await Task.WhenAny(waitTask, ceiling);

            if (completed == ceiling)
            {
                string diagnostic = GetCeilingDiagnostic(markerPath, process, timeoutSeconds);
                Assert.Fail(diagnostic);
            }

            await ProcessTestHelper.WaitForConditionAsync(
                () => !Process.GetProcesses().Any(x => x.Id == processId),
                TimeSpan.FromSeconds(5),
                failureMessage: $"Process {processId} still running 5s after cancellation");
        }
        finally
        {
            if (!process.HasExited)
            {
                try { process.Kill(true); } catch { process.Kill(); }
            }

            process.Dispose();

            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }

    [Test]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task ProcessCancelled_Normal_Delay_CancelAfter30Seconds_Success()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-process-cancel-normal-{Guid.NewGuid():N}.marker");

        const int timeoutSeconds = 3;
        const int ceilingSeconds = timeoutSeconds * 2;

        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 10);

        ProcessExitConfiguration processExitConfiguration = new(
            ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(timeoutSeconds)),
            cancellationThrowsException: false);

        process.Start();

        int processId = process.Id;

        try
        {
            Task waitTask = process.WaitForExitOrTimeoutAsync(processExitConfiguration, CancellationToken.None);
            Task ceiling = Task.Delay(TimeSpan.FromSeconds(ceilingSeconds));
            Task completed = await Task.WhenAny(waitTask, ceiling);

            if (completed == ceiling)
            {
                string diagnostic = GetCeilingDiagnostic(markerPath, process, timeoutSeconds);
                Assert.Fail(diagnostic);
            }

            await ProcessTestHelper.WaitForConditionAsync(
                () => !Process.GetProcesses().Any(x => x.Id == processId),
                TimeSpan.FromSeconds(5),
                failureMessage: $"Process {processId} still running 5s after cancellation");
        }
        finally
        {
            if (!process.HasExited)
            {
                try { process.Kill(true); } catch { process.Kill(); }
            }

            process.Dispose();

            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }

    private static string GetCeilingDiagnostic(string markerPath, ProcessWrapper process, int timeoutSeconds)
    {
        bool markerExists = File.Exists(markerPath);

        if (!markerExists)
        {
            return $"Process did not receive the interrupt signal within {timeoutSeconds}s; " +
                   $"marker file {markerPath} is missing.";
        }

        if (!process.HasExited)
        {
            return $"Process received the interrupt signal but did not exit within " +
                   $"{timeoutSeconds}s after cancellation.";
        }

        return $"Process exited and marker file exists (signal was delivered), but " +
               $"WaitForExitOrTimeoutAsync did not complete within the ceiling. " +
               $"Possible race in wait-task completion after process exit.";
    }
}
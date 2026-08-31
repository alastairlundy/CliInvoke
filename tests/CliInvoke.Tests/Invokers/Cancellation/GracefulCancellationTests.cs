using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Processes;
using CliInvoke.Processes.Internal;
using CliInvoke.Tests.Internal.Helpers;

namespace CliInvoke.Tests.Invokers.Cancellation;

public class GracefulCancellationTests
{
    [Test]
    public async Task GracefulCancel_InterruptSignals_Success()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-graceful-cancel-{Guid.NewGuid():N}.marker");

        int gracefulTimeoutSeconds = 2;

        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 5);

        process.Start();

        ProcessExitConfiguration exitConfiguration = new ProcessExitConfiguration(
            ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(gracefulTimeoutSeconds)),
            cancellationThrowsException: false);

        try
        {
            await process.WaitForExitOrGracefulTimeoutAsync(exitConfiguration, CancellationToken.None, false);

            await Assert.That(process.HasExited).IsTrue();

            // On Windows, the signal-trapping helper process has no console or is in a separate
            // process group, so GenerateConsoleCtrlEvent cannot deliver the interrupt.
            // The process is killed by the graceful fallback instead. Skip the marker assertion.
            // On Unix (Linux, macOS), SIGINT is delivered directly via Process.Kill() and the
            // Console.CancelKeyPress handler fires, writing the marker before the process exits.
            if (!OperatingSystem.IsWindows())
            {
                await Assert.That(File.Exists(markerPath)).IsTrue();
            }
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
    public async Task GracefulTimeout_Canceled_IsTrue()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-graceful-canceled-{Guid.NewGuid():N}.marker");

        const int gracefulTimeoutSeconds = 2;

        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 5);

        process.Start();

        ProcessExitConfiguration exitConfiguration = new ProcessExitConfiguration(
            ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(gracefulTimeoutSeconds)),
            cancellationThrowsException: false);

        try
        {
            await process.WaitForExitOrGracefulTimeoutAsync(exitConfiguration, CancellationToken.None);

            await Assert.That(process.HasExited).IsTrue();
            await Assert.That(process.Canceled).IsTrue();
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
    public async Task GracefulTimeout_ProcessResult_Canceled_IsTrue()
    {
        // Regression test: the ProcessResult returned to the caller must report Canceled as true
        // after a graceful timeout cancellation. This guards against a race where the result was
        // built before ProcessWrapper.Canceled reflected the persisted cancellation reason.
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-graceful-result-canceled-{Guid.NewGuid():N}.marker");

        const int gracefulTimeoutSeconds = 2;

        string helperPath = ProcessTestHelper.GetSignalTrappingHelperPath();
        ProcessConfiguration configuration = new(helperPath, $"\"{markerPath}\" 5");

        ProcessExitConfiguration exitConfiguration = new(
            ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(gracefulTimeoutSeconds)),
            cancellationThrowsException: false);

        try
        {
            using ExternalProcess process = new(new FilePathResolver(), configuration, exitConfiguration);
            process.Start();

            ProcessResult result = await process.WaitForExitOrTimeoutAsync(CancellationToken.None);

            await Assert.That(process.HasExited).IsTrue();
            await Assert.That(result.Canceled).IsTrue();
        }
        finally
        {
            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }
}

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
            process.Dispose();

            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }
}
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
            await Assert.That(File.Exists(markerPath)).IsTrue();
        }
        finally
        {
            process.Dispose();

            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }
}
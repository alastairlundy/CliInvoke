using CliInvoke.Factories;
using CliInvoke.Processes;
using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Invokers.Cancellation;

public class GracefulCancellationTests
{
    [Test]
    public async Task GracefulCancel_InterruptSignals_Success()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-graceful-cancel-{Guid.NewGuid():N}.marker");

        int gracefulTimeoutSeconds = 2;

        // The child must comfortably outlive the graceful timeout. On loaded CI runners the
        // timeout continuation can be delayed by several seconds; if the child reaches its
        // natural exit first, no cancellation is ever performed and the machinery correctly
        // reports not-canceled — so a short sleep races the timeout and fails intermittently.
        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 30);

        process.Start();

        ProcessExitConfiguration exitConfiguration = new ProcessExitConfiguration(
            ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(gracefulTimeoutSeconds)),
            cancellationThrowsException: false);

        try
        {
            await process.WaitForExitOrGracefulTimeoutAsync(exitConfiguration, CancellationToken.None, false).ConfigureAwait(true);

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

        // Sleep well past the 2s graceful timeout (rationale in
        // GracefulCancel_InterruptSignals_Success): a child that exits naturally before the
        // timeout fires is correctly reported as not canceled, which fails the assertion below.
        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 30);

        process.Start();

        ProcessExitConfiguration exitConfiguration = new ProcessExitConfiguration(
            ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(gracefulTimeoutSeconds)),
            cancellationThrowsException: false);

        try
        {
            await process.WaitForExitOrGracefulTimeoutAsync(exitConfiguration, CancellationToken.None).ConfigureAwait(true);

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
    public async Task ShellDetector_Cancellation_Propagates()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
        {
            // ShellDetector Unix flow only runs on Unix-like systems
            return;
        }

        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(true); // Pre-cancel the token

        ShellDetector detector = new ShellDetector(
            new ProcessInvoker(new ExternalProcessFactory()),
            new FilePathResolver());

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await detector.ResolveDefaultShellAsync(cts.Token).ConfigureAwait(true));
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
        // 30s sleep instead of 5s: the helper must outlive the 2s graceful timeout by a wide
        // margin so a delayed timeout continuation on loaded CI runners cannot let the child
        // exit naturally first — that path correctly reports Canceled as false (regression
        // intent: verify the timeout machinery's result, not a natural exit).
        ProcessConfiguration configuration = new(helperPath, $"\"{markerPath}\" 30");

        ProcessExitConfiguration exitConfiguration = new(
            ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(gracefulTimeoutSeconds)),
            cancellationThrowsException: false);

        try
        {
            using ExternalProcess process = new(new FilePathResolver(), configuration, exitConfiguration);
            process.Start();

            ProcessResult result = await process.WaitForExitOrTimeoutAsync(CancellationToken.None).ConfigureAwait(true);

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

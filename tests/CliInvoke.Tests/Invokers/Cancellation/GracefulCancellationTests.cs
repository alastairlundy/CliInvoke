using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes;

namespace CliInvoke.Tests.Invokers.Cancellation;

public class GracefulCancellationTests
{
    [Test]
    public async Task GracefulCancel_InterruptSignals_Success()
    {
        int sleepTimeSeconds = 500;

        int gracefulTimeoutSeconds = 10;

        ProcessWrapper process;

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsAndroid() ||
            OperatingSystem.IsFreeBSD())
            process = ProcessTestHelper.CreateProcess("sleep", $"{sleepTimeSeconds}");
        else if (OperatingSystem.IsWindows())
            process = ProcessTestHelper.CreateProcess("timeout", $"/t {sleepTimeSeconds} /nobreak");
        else
            throw new PlatformNotSupportedException();

        Stopwatch stopwatch = new();
        process.Start();
        stopwatch.Start();

        ProcessExitConfiguration exitConfiguration = new ProcessExitConfiguration(ProcessTimeoutPolicy.FromTimeSpan
            (TimeSpan.FromSeconds(gracefulTimeoutSeconds)), cancellationThrowsException: false);

        await process.WaitForExitOrGracefulTimeoutAsync(exitConfiguration, CancellationToken.None, false);

        stopwatch.Stop();

        long elapsedTimeSeconds = stopwatch.ElapsedMilliseconds / 1000;

        await Assert.That(elapsedTimeSeconds).IsBetween(0, Math.Min(gracefulTimeoutSeconds * 3, 60));

        await Assert.That(process.HasExited).IsTrue();

        if (!process.HasExited)
            process.Kill();

        process.Dispose();
    }
}
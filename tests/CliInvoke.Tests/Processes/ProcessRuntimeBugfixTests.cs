/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Text;

using CliInvoke.Processes;
using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Regression tests for the process runtime: configuration-overload stdin no-op,
///     deterministic cancellation-exception propagation, disabled timeout policies, and
///     ExitTime publication before a wait returns.
/// </summary>
public class ProcessRuntimeBugfixTests
{
    /// <summary>
    ///     <c>StartAsync(configuration, ct)</c> must honour
    ///     <c>RedirectStandardInput = false</c>: a supplied stdin writer stays a no-op and
    ///     the source stream is never read, matching the documented instance-overload
    ///     behaviour.
    /// </summary>
    [Test]
    public async Task StartAsync_WithConfiguration_RedirectDisabled_DoesNotConsumeStandardInput()
    {
        string targetFilePath = ProcessTestHelper.GetTargetFilePath();
        string arguments = OperatingSystem.IsWindows() ? "/c exit 0" : string.Empty;

        // Not flushed on purpose: the pipe (when it runs) reads BaseStream directly, so a
        // consumed source is observable as an advanced position while a no-op leaves it at 0.
        using MemoryStream source = new(Encoding.ASCII.GetBytes("stdin payload"));
        using StreamWriter writer = new(source);

        ProcessConfiguration configuration = new(targetFilePath, arguments, outputRedirection: false)
        {
            RedirectStandardInput = false,
            StandardInput = writer,
        };

        using ExternalProcess process = new(new FilePathResolver(), configuration);

        try
        {
            await process.StartAsync(configuration, CancellationToken.None);

            await Assert.That(process.HasStarted).IsTrue();
            await Assert.That(source.Position).IsEqualTo(0);
        }
        finally
        {
            await process.Kill();
        }
    }

    /// <summary>
    ///     A timeout policy with <c>Enabled = false</c> must not enforce its threshold.
    ///     The wait outlives the 1ms threshold by seconds while the child sleeps, and ends
    ///     only when the process itself ends.
    /// </summary>
    [Test]
    public async Task WaitForExitOrTimeoutAsync_DisabledTimeoutPolicy_DoesNotEnforceThreshold()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-disabled-timeout-{Guid.NewGuid():N}.marker");

        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 10);

        ProcessExitConfiguration exitConfiguration = new(
            new ProcessTimeoutPolicy(TimeSpan.FromMilliseconds(1), enabled: false));

        process.Start();

        try
        {
            Task waitTask = process.WaitForExitOrTimeoutAsync(exitConfiguration, CancellationToken.None);

            // Well past the disabled 1ms threshold the wait must still be pending: the
            // child sleeps 10s and a disabled policy never arms the timeout machinery.
            await Task.Delay(TimeSpan.FromSeconds(2));
            await Assert.That(waitTask.IsCompleted).IsFalse();

            // The wait still ends when the process does.
            process.ForcefulExit();
            await waitTask.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.That(process.HasExited).IsTrue();
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

    /// <summary>
    ///     Cancelling the caller's token during a graceful wait surfaces the
    ///     cancellation to the caller when <c>CancellationThrowsException</c> is set. The
    ///     interrupt task's fault is observed on every path, so propagation does not
    ///     depend on timing.
    /// </summary>
    [Test]
    public async Task GracefulWait_CallerCancellation_WithCancellationThrowsException_PropagatesOperationCanceled()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-cancel-throws-{Guid.NewGuid():N}.marker");

        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 10);

        using CancellationTokenSource cancellationTokenSource = new();
        ProcessExitConfiguration exitConfiguration = new(
            new ProcessTimeoutPolicy(TimeSpan.FromSeconds(30), enabled: true),
            cancellationThrowsException: true);

        process.Start();

        try
        {
            // The wait runs synchronously up to its first pending await, so by the time this
            // call returns the interrupt task is armed on the 30s delay and cancellation
            // below deterministically interrupts it.
            Task waitTask = process.WaitForExitOrGracefulTimeoutAsync(exitConfiguration,
                cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();

            await Assert.That(async () => await waitTask).Throws<OperationCanceledException>();

            await ProcessTestHelper.WaitForConditionAsync(
                () => process.HasExited,
                TimeSpan.FromSeconds(5),
                failureMessage: "Process was not terminated by the cancellation machinery within 5s.");
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

    /// <summary>
    ///     With the default configuration (<c>CancellationThrowsException = false</c>), the
    ///     same caller cancellation must not throw: the wait completes normally after the
    ///     machinery terminates the child.
    /// </summary>
    [Test]
    public async Task GracefulWait_CallerCancellation_DefaultConfiguration_DoesNotThrow()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-cancel-swallows-{Guid.NewGuid():N}.marker");

        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 10);

        using CancellationTokenSource cancellationTokenSource = new();
        ProcessExitConfiguration exitConfiguration = new(
            new ProcessTimeoutPolicy(TimeSpan.FromSeconds(30), enabled: true),
            cancellationThrowsException: false);

        process.Start();

        try
        {
            Task waitTask = process.WaitForExitOrGracefulTimeoutAsync(exitConfiguration,
                cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();

            await waitTask.WaitAsync(TimeSpan.FromSeconds(15));

            await ProcessTestHelper.WaitForConditionAsync(
                () => process.HasExited,
                TimeSpan.FromSeconds(5),
                failureMessage: "Process was not terminated by the cancellation machinery within 5s.");
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

    /// <summary>
    ///     The result's exit time must be published by the time the wait returns. Readers
    ///     must never see <c>default(DateTime)</c> (which produces a bogus negative
    ///     RuntimeDuration) when the Exited callback lags the HasExited flip.
    /// </summary>
    [Test]
    public async Task WaitForExitOrTimeoutAsync_ExitTimeIsPopulated()
    {
        string targetFilePath = ProcessTestHelper.GetTargetFilePath();
        string arguments = OperatingSystem.IsWindows() ? "/c exit 0" : string.Empty;

        ProcessConfiguration configuration = new(targetFilePath, arguments, outputRedirection: false);
        using ExternalProcess process = new(new FilePathResolver(), configuration);

        try
        {
            process.Start();

            ProcessResult result = await process.WaitForExitOrTimeoutAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(30));

            await Assert.That(result.ExitTime).IsNotEqualTo(default(DateTime));
            await Assert.That(process.HasExited).IsTrue();
        }
        finally
        {
            await process.Kill();
        }
    }
}

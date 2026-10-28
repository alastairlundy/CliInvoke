/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Processes;

namespace CliInvoke.Tests.Invokers.Cancellation;

/// <summary>
///     Contract tests for buffered-mode cancellation: when
///     <c>CancellationThrowsException</c> is false (the default), a cancelled buffered run
///     returns a normal <see cref="BufferedProcessResult"/> with <c>Canceled</c> set instead
///     of throwing an <see cref="OperationCanceledException"/>; when it is true, the OCE
///     propagates. Raw mode's already-correct behaviour is covered as a regression guard.
/// </summary>
public class BufferedCancellationContractTests
{
    /// <summary>
    ///     Buffered mode with the default exit configuration
    ///     (<c>CancellationThrowsException = false</c>): cancelling the caller's token
    ///     mid-run must complete normally with <c>Canceled</c> set and empty captured
    ///     output, not throw an <see cref="OperationCanceledException"/>.
    /// </summary>
    [Test]
    public async Task Buffered_CallerCancellation_DefaultExitConfiguration_ReturnsCanceledResultWithoutThrowing()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-buffered-cancel-default-{Guid.NewGuid():N}.marker");

        using CancellationTokenSource cancellationTokenSource = new();

        ProcessConfiguration configuration = new(
            ProcessTestHelper.GetSignalTrappingHelperPath(),
            $"\"{markerPath}\" 10", outputRedirection: true);

        ProcessExitConfiguration exitConfiguration = new(
            new ProcessTimeoutPolicy(TimeSpan.FromSeconds(30), enabled: true),
            cancellationThrowsException: false);

        ExternalProcess process = new(new FilePathResolver(), configuration, exitConfiguration);

        try
        {
            process.Start();

            // The capture runs synchronously up to its first pending await, so by the
            // time this call returns the wait and drain tasks are armed on the child
            // and the cancellation below deterministically cancels them.
            Task<BufferedProcessResult> captureTask =
                process.CaptureBufferedResultAsync(cancellationTokenSource.Token);
            await cancellationTokenSource.CancelAsync().ConfigureAwait(true);

            BufferedProcessResult result =
                await captureTask.WaitAsync(TimeSpan.FromSeconds(15), cancellationTokenSource.Token).ConfigureAwait(true);

            await Assert.That(result.Canceled).IsTrue();
            await Assert.That(result.StandardOutput).IsEqualTo(string.Empty);
            await Assert.That(result.StandardError).IsEqualTo(string.Empty);
        }
        finally
        {
            if (!process.HasExited)
            {
                await process.Kill().ConfigureAwait(true);
            }

            process.Dispose();

            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }

    /// <summary>
    ///     Buffered mode with <c>CancellationThrowsException = true</c>: the same caller
    ///     cancellation must propagate an <see cref="OperationCanceledException"/> to the
    ///     caller instead of being absorbed into a result.
    /// </summary>
    [Test]
    public async Task Buffered_CallerCancellation_WithCancellationThrowsException_PropagatesOperationCanceled()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-buffered-cancel-throws-{Guid.NewGuid():N}.marker");

        using CancellationTokenSource cancellationTokenSource = new();

        ProcessConfiguration configuration = new(
            ProcessTestHelper.GetSignalTrappingHelperPath(),
            $"\"{markerPath}\" 10", outputRedirection: true);

        ProcessExitConfiguration exitConfiguration = new(
            new ProcessTimeoutPolicy(TimeSpan.FromSeconds(30), enabled: true),
            cancellationThrowsException: true);

        ExternalProcess process = new(new FilePathResolver(), configuration, exitConfiguration);

        try
        {
            process.Start();

            Task<BufferedProcessResult> captureTask =
                process.CaptureBufferedResultAsync(cancellationTokenSource.Token);
            await cancellationTokenSource.CancelAsync().ConfigureAwait(true);

            OperationCanceledException? propagated = null;
            try
            {
                await captureTask.WaitAsync(TimeSpan.FromSeconds(15), cancellationTokenSource.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException exception)
            {
                propagated = exception;
            }

            await Assert.That(propagated).IsNotNull();
        }
        finally
        {
            if (!process.HasExited)
            {
                await process.Kill().ConfigureAwait(true);
            }

            process.Dispose();

            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }

    /// <summary>
    ///     Regression guard for the already-correct raw-mode path: cancelling the caller's
    ///     token mid-run returns a normal <see cref="ProcessResult"/> with <c>Canceled</c>
    ///     set instead of throwing.
    /// </summary>
    [Test]
    public async Task Raw_CallerCancellation_DefaultExitConfiguration_ReturnsCanceledResult()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-raw-cancel-default-{Guid.NewGuid():N}.marker");

        using CancellationTokenSource cancellationTokenSource = new();

        ProcessConfiguration configuration = new(
            ProcessTestHelper.GetSignalTrappingHelperPath(),
            $"\"{markerPath}\" 10", outputRedirection: false);

        ProcessExitConfiguration exitConfiguration = new(
            new ProcessTimeoutPolicy(TimeSpan.FromSeconds(30), enabled: true),
            cancellationThrowsException: false);

        ExternalProcess process = new(new FilePathResolver(), configuration, exitConfiguration);

        try
        {
            process.Start();

            Task<ProcessResult> waitTask =
                process.WaitForExitOrTimeoutAsync(cancellationTokenSource.Token);
            await cancellationTokenSource.CancelAsync().ConfigureAwait(true);

            ProcessResult result = await waitTask.WaitAsync(TimeSpan.FromSeconds(15), cancellationTokenSource.Token).ConfigureAwait(true);

            await Assert.That(result.Canceled).IsTrue();
        }
        finally
        {
            if (!process.HasExited)
            {
                await process.Kill().ConfigureAwait(true);
            }

            process.Dispose();

            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }
}

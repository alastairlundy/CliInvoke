/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Processes;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Pins the abandoned-invocation cleanup: an exception path that ends an invocation
///     after the child has started (a token canceled before the wait began is rejected
///     before any cancellation machinery is armed) must terminate the child rather than
///     orphan a process the caller holds no handle to. Each test runs a real child that
///     would otherwise sleep for 30 seconds and fails with a stated reason if it is still
///     running when the guards expire.
/// </summary>
public class AbandonedChildCleanupTests
{
    /// <summary>
    ///     Buffered mode: the capture rejects a pre-canceled token before arming the kill
    ///     machinery, so the started child must be terminated by the library's cleanup
    ///     before the <see cref="OperationCanceledException"/> escapes.
    /// </summary>
    [Test]
    public async Task CaptureBufferedResultAsync_PreCancelledToken_TerminatesStartedChild()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-abandoned-buffered-{Guid.NewGuid():N}.marker");

        ProcessConfiguration configuration = new(
            ProcessTestHelper.GetSignalTrappingHelperPath(),
            $"\"{markerPath}\" 30", outputRedirection: true);

        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync().ConfigureAwait(true);

        using ExternalProcess process = new(new FilePathResolver(), configuration,
            ProcessExitConfiguration.CreateGraceful());

        process.Start();

        await Assert.That(async () => await process
                .CaptureBufferedResultAsync(cancellationTokenSource.Token).ConfigureAwait(true))
            .Throws<OperationCanceledException>();

        await ProcessTestHelper.WaitForConditionAsync(
                () => process.HasExited,
                TimeSpan.FromSeconds(10),
                failureMessage:
                "The started child was still running after a pre-canceled buffered invocation threw.")
            .ConfigureAwait(true);

        if (File.Exists(markerPath))
            File.Delete(markerPath);
    }

    /// <summary>
    ///     Raw mode: the wait rejects a pre-canceled token at the same point the
    ///     cancellation machinery would have been armed, so the started child must be
    ///     terminated by the same cleanup before the exception escapes.
    /// </summary>
    [Test]
    public async Task WaitForExitOrTimeoutAsync_PreCancelledToken_TerminatesStartedChild()
    {
        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-abandoned-raw-{Guid.NewGuid():N}.marker");

        ProcessConfiguration configuration = new(
            ProcessTestHelper.GetSignalTrappingHelperPath(),
            $"\"{markerPath}\" 30", outputRedirection: true);

        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync().ConfigureAwait(true);

        using ExternalProcess process = new(new FilePathResolver(), configuration,
            ProcessExitConfiguration.CreateGraceful());

        process.Start();

        await Assert.That(async () => await process
                .WaitForExitOrTimeoutAsync(cancellationTokenSource.Token).ConfigureAwait(true))
            .Throws<OperationCanceledException>();

        await ProcessTestHelper.WaitForConditionAsync(
                () => process.HasExited,
                TimeSpan.FromSeconds(10),
                failureMessage:
                "The started child was still running after a pre-canceled raw wait threw.")
            .ConfigureAwait(true);

        if (File.Exists(markerPath))
            File.Delete(markerPath);
    }
}

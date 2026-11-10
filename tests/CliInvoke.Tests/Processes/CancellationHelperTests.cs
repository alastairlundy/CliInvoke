/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Processes.Internal.Cancellation;

namespace CliInvoke.Tests.Processes;

public class CancellationHelperTests
{
    private static ProcessExitConfiguration Config(
        ProcessExceptionBehaviour exceptionBehaviour)
        => new(ProcessTimeoutPolicy.Default, ProcessExitBehaviour.GracefulExit,
            exceptionBehaviour, cancellationThrowsException: false);

    [Test]
    public async Task HandleCancellationExceptions_TimeoutOceWithinWindow_Swallows()
    {
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new OperationCanceledException()))
            .ThrowsNothing();
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutOceBeyondWindow_Throws()
    {
        // Beyond the 1-second on-time window the cancellation is unexpected, so the
        // exception propagates instead of being swallowed by the timeout heuristic.
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64 - 10_000, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new OperationCanceledException()))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task GetCancellationReason_CallerTokenCancelled_ReturnsRequestedCancellation()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync().ConfigureAwait(true);

        CancellationReason reason = CancellationHelper.GetCancellationReason(
            Environment.TickCount64 + 60_000, cts.Token);

        await Assert.That(reason).IsEqualTo(CancellationReason.RequestedCancellation);
    }

    [Test]
    public async Task GetCancellationReason_DeadlineReached_ReturnsTimeout()
    {
        // Monotonic reads never go backwards, so a deadline read at or before the
        // observation can never be classified as anything but a timeout.
        CancellationReason reason = CancellationHelper.GetCancellationReason(
            Environment.TickCount64, CancellationToken.None);

        await Assert.That(reason).IsEqualTo(CancellationReason.Timeout);
    }

    [Test]
    public async Task GetCancellationReason_DeadlineInFuture_ReturnsNotKnown()
    {
        CancellationReason reason = CancellationHelper.GetCancellationReason(
            Environment.TickCount64 + 60_000, CancellationToken.None);

        await Assert.That(reason).IsEqualTo(CancellationReason.NotKnown);
    }

    [Test]
    public async Task GetCancellationReason_AfterArmedTimerFires_ReturnsTimeout()
    {
        // Regression (Ubuntu CI on main, latent here): the kill timer can fire marginally
        // before a DateTime.UtcNow-based deadline elapses (dotnet/runtime#100455), which
        // made an on-time timeout classify as NotKnown and left Canceled reporting false
        // for a process the cancellation machinery terminated. The deadline must be
        // derived from the same monotonic clock the timer fires against, exactly as the
        // wait does.
        long expectedExitTick = CancellationHelper.CalculateExpectedExitTick(
            TimeSpan.FromMilliseconds(50));

        using CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        OperationCanceledException? caught = null;
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();

        CancellationReason reason = CancellationHelper.GetCancellationReason(
            expectedExitTick, CancellationToken.None);

        await Assert.That(reason).IsEqualTo(CancellationReason.Timeout);
    }
}

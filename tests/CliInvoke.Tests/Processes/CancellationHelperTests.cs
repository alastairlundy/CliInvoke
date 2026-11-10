/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Exceptions;
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
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64 - 10_000, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new OperationCanceledException()))
            .Throws<CliInvokeTimeoutException>();
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutNonOceWithinWindow_Throws()
    {
        // L7b: a genuine failure near the timeout must never be eaten by the
        // wall-clock window — only cancellation exceptions are time-discriminated.
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new IOException("transient read failure")))
            .Throws<IOException>();
    }

    [Test]
    public async Task HandleCancellationExceptions_NotKnownNonOceWithinWindow_Throws()
    {
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64, CancellationReason.NotKnown,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new IOException("transient read failure")))
            .Throws<IOException>();
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutNonOceWithinWindow_SuppressExceptions_Swallows()
    {
        // SuppressExceptions still swallows everything, genuine failures included.
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.SuppressExceptions),
                new IOException("transient read failure")))
            .ThrowsNothing();
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutOceWithinWindow_AllowExceptions_Throws()
    {
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptions),
                new OperationCanceledException()))
            .Throws<CliInvokeTimeoutException>();
    }

    [Test]
    public async Task HandleCancellationExceptions_RequestedCancellationNonOce_Swallows()
    {
        // Scope regression: the L7b type gate applies only to the Timeout/NotKnown
        // window; the requested-cancellation branch keeps its existing behaviour.
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64, CancellationReason.RequestedCancellation,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new IOException("teardown read failure")))
            .ThrowsNothing();
    }

    [Test]
    public async Task HandleCancellationExceptions_RequestedCancellationOce_AllowExceptions_PreservesCancellationToken()
    {
        // The wrapped CliInvokeCanceledException must carry the original exception's
        // token so callers can still match the cancellation against their own token.
        using CancellationTokenSource cts = new();
        CliInvokeCanceledException? caught = null;
        try
        {
            CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64, CancellationReason.RequestedCancellation,
                Config(ProcessExceptionBehaviour.AllowExceptions),
                new OperationCanceledException(cts.Token));
        }
        catch (CliInvokeCanceledException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.CancellationToken).IsEqualTo(cts.Token);
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutOce_CancellationThrowsException_CatchableAsOperationCanceled()
    {
        // Contract (troubleshooting guide): with CancellationThrowsException enabled, a
        // timer-driven timeout must surface as an OperationCanceledException so existing
        // catch (OperationCanceledException) handlers around CaptureBufferedResultAsync
        // and the buffered run APIs keep working; the concrete type still distinguishes
        // it from a caller-requested cancellation, and the original token survives.
        using CancellationTokenSource cts = new();
        ProcessExitConfiguration configuration = new(ProcessTimeoutPolicy.Default,
            ProcessExitBehaviour.GracefulExit,
            ProcessExceptionBehaviour.AllowExceptionsIfUnexpected, cancellationThrowsException: true);

        OperationCanceledException? caught = null;
        try
        {
            CancellationHelper.HandleCancellationExceptions(
                Environment.TickCount64, CancellationReason.Timeout, configuration,
                new OperationCanceledException(cts.Token));
        }
        catch (OperationCanceledException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught).IsTypeOf<CliInvokeTimeoutException>();
        await Assert.That(caught!.CancellationToken).IsEqualTo(cts.Token);
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
        // Regression (Ubuntu CI): the kill timer can fire marginally before a
        // DateTime.UtcNow-based deadline elapses (dotnet/runtime#100455), which made an
        // on-time timeout classify as NotKnown and surface as CliInvokeCanceledException
        // instead of CliInvokeTimeoutException. The deadline must be derived from the
        // same monotonic clock the timer fires against, exactly as the wait does.
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

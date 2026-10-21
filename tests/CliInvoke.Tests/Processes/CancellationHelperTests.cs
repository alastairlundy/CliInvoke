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
                DateTime.UtcNow, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new OperationCanceledException()))
            .ThrowsNothing();
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutOceBeyondWindow_Throws()
    {
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                DateTime.UtcNow.AddSeconds(-10), CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new OperationCanceledException()))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutNonOceWithinWindow_Throws()
    {
        // L7b: a genuine failure near the timeout must never be eaten by the
        // wall-clock window — only cancellation exceptions are time-discriminated.
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                DateTime.UtcNow, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new IOException("transient read failure")))
            .Throws<IOException>();
    }

    [Test]
    public async Task HandleCancellationExceptions_NotKnownNonOceWithinWindow_Throws()
    {
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                DateTime.UtcNow, CancellationReason.NotKnown,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new IOException("transient read failure")))
            .Throws<IOException>();
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutNonOceWithinWindow_SuppressExceptions_Swallows()
    {
        // SuppressExceptions still swallows everything, genuine failures included.
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                DateTime.UtcNow, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.SuppressExceptions),
                new IOException("transient read failure")))
            .ThrowsNothing();
    }

    [Test]
    public async Task HandleCancellationExceptions_TimeoutOceWithinWindow_AllowExceptions_Throws()
    {
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                DateTime.UtcNow, CancellationReason.Timeout,
                Config(ProcessExceptionBehaviour.AllowExceptions),
                new OperationCanceledException()))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task HandleCancellationExceptions_RequestedCancellationNonOce_Swallows()
    {
        // Scope regression: the L7b type gate applies only to the Timeout/NotKnown
        // window; the requested-cancellation branch keeps its existing behaviour.
        await Assert.That(() => CancellationHelper.HandleCancellationExceptions(
                DateTime.UtcNow, CancellationReason.RequestedCancellation,
                Config(ProcessExceptionBehaviour.AllowExceptionsIfUnexpected),
                new IOException("teardown read failure")))
            .ThrowsNothing();
    }
}

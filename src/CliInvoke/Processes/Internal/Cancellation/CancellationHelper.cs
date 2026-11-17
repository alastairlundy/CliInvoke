/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core;
using CliInvoke.Core.Exceptions;

namespace CliInvoke.Processes.Internal.Cancellation;

internal static class CancellationHelper
{
    /// <summary>
    ///     Slack, in milliseconds, subtracted from the deadline when classifying a cancellation.
    ///     An observation up to this far before the expected exit tick still classifies as
    ///     <see cref="CancellationReason.Timeout" />.
    /// </summary>
    /// <remarks>
    ///     The OS may deliver a <c>CancelAfter</c> timer marginally before the
    ///     <see cref="Environment.TickCount64" /> deadline it was armed against
    ///     (dotnet/runtime#100455, closed as by-design). The window covers the early delivery
    ///     reported in that issue plus a full Windows timer tick (~15.6 ms) of extra skew
    ///     observed on Windows CI runners (commit 6f633f32's regression test documents a
    ///     25 ms envelope), where delivery beyond the original 10 ms grace still flaked an
    ///     on-time timeout into <see cref="CancellationReason.NotKnown"/>. It stays far below
    ///     the 1 second window
    ///     <see cref="HandleCancellationExceptions" /> applies, so a genuine failure that
    ///     precedes a timeout by more than this still classifies as
    ///     <see cref="CancellationReason.NotKnown" />.
    /// </remarks>
    internal const long TimerDeliveryGraceMilliseconds = 25;

    /// <summary>
    ///     Determines the reason for the cancellation of a process.
    /// </summary>
    /// <param name="expectedExitTick">
    ///     The monotonic deadline (<see cref="Environment.TickCount64" /> basis) by which the
    ///     process was expected to exit, captured when the wait began.
    /// </param>
    /// <param name="cancellationToken">
    ///     The token associated with the cancellation, used to determine the
    ///     reason.
    /// </param>
    /// <returns>The reason for the cancellation.</returns>
    /// <remarks>
    ///     Both the deadline and the observation are read from <see cref="Environment.TickCount64" />,
    ///     the same monotonic clock the runtime's timer queue fires <c>CancelAfter</c> and
    ///     <c>Task.Delay</c> against, so a kill timer that fires marginally ahead of the wall clock
    ///     can never be misclassified as <see cref="CancellationReason.NotKnown" />. Classifying on
    ///     <c>DateTime.UtcNow</c> instead mixed clock domains and did exactly that
    ///     (dotnet/runtime#100455), which surfaced an on-time timeout as a cancellation. The
    ///     comparison also allows for <see cref="TimerDeliveryGraceMilliseconds" /> of early timer
    ///     delivery, because the OS may deliver the kill timer shortly before the deadline it was
    ///     armed against (the same issue, closed as by-design). The forceful timer wait
    ///     classifies by token ownership instead of clock timing — see
    ///     <see cref="GetCancellationReasonFromTokens" /> — because delivery skew beyond
    ///     the grace window kept misclassifying on-time timeouts on Windows CI.
    /// </remarks>
    internal static CancellationReason GetCancellationReason(long expectedExitTick,
        CancellationToken cancellationToken)
    {
        // Make the check atomic by capturing both values before making the decision
        bool isCancellationRequested = cancellationToken.IsCancellationRequested;
        long cancellationTick = Environment.TickCount64;

        if (isCancellationRequested) return CancellationReason.RequestedCancellation;

        return cancellationTick >= expectedExitTick - TimerDeliveryGraceMilliseconds
            ? CancellationReason.Timeout
            : CancellationReason.NotKnown;
    }

    /// <summary>
    ///     Classifies the reason a forceful-timer wait's cancellation exception was raised,
    ///     from token ownership rather than clock timing.
    /// </summary>
    /// <param name="callerToken">
    ///     The caller's cancellation token passed to the wait.
    /// </param>
    /// <param name="timerToken">
    ///     The token of the linked <see cref="CancellationTokenSource"/> whose
    ///     <c>CancelAfter</c> arms the wait's kill timer. It can only be cancelled by that
    ///     timer, or by the caller's token cancelling first — which the
    ///     <paramref name="callerToken"/> check rules out — so "timer cancelled, caller not"
    ///     identifies a timer-driven timeout unambiguously.
    /// </param>
    /// <returns>
    ///     <see cref="CancellationReason.RequestedCancellation"/> when the caller's token is
    ///     cancelled, <see cref="CancellationReason.Timeout"/> when only the kill timer's
    ///     token is, and <see cref="CancellationReason.NotKnown"/> when neither is (genuine
    ///     non-cancellation failures reach the same catch block).
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="GetCancellationReason"/>, this classification never reads
    ///         the clock, so timer delivery skew cannot flip an on-time timeout into
    ///         <see cref="CancellationReason.NotKnown"/>: the recurring Windows CI flake had
    ///         an on-time kill delivered more than
    ///         <see cref="TimerDeliveryGraceMilliseconds"/> early, which clock-based
    ///         classification surfaced as <c>CliInvokeCanceledException</c> instead of
    ///         <c>CliInvokeTimeoutException</c> under
    ///         <c>CancellationThrowsException</c>.
    ///     </para>
    ///     <para>
    ///         The flag reads are safe at the catch point: the caught cancellation exception
    ///         can only have been raised after the owning token's cancellation flag was set,
    ///         so the flag cannot still be unset when the token that raised the exception is
    ///         inspected.
    ///     </para>
    /// </remarks>
    internal static CancellationReason GetCancellationReasonFromTokens(
        CancellationToken callerToken, CancellationToken timerToken)
    {
        if (callerToken.IsCancellationRequested)
            return CancellationReason.RequestedCancellation;

        return timerToken.IsCancellationRequested
            ? CancellationReason.Timeout
            : CancellationReason.NotKnown;
    }

    /// <summary>
    ///     Calculates the monotonic deadline by which a process using the specified
    ///     <see cref="ProcessExitConfiguration" /> object is expected to exit.
    /// </summary>
    /// <param name="exitConfiguration">The exit configuration to use.</param>
    /// <returns>
    ///     The calculated expected exit deadline, on the <see cref="Environment.TickCount64" /> basis.
    /// </returns>
    internal static long CalculateExpectedExitTick(ProcessExitConfiguration exitConfiguration)
        => CalculateExpectedExitTick(exitConfiguration.TimeoutPolicy.TimeoutThreshold);

    /// <summary>
    ///     Calculates the monotonic deadline by which a process is expected to exit after
    ///     <paramref name="timeoutThreshold" />.
    /// </summary>
    /// <remarks>
    ///     The threshold is truncated to whole milliseconds, the same conversion the runtime's
    ///     timer queue applies to its due times. Callers capture this deadline before arming
    ///     their timer, so the armed timer's nominal due time can never precede it. The OS may
    ///     still deliver the timer marginally early (dotnet/runtime#100455, closed as by-design),
    ///     which is why <see cref="GetCancellationReason" /> allows
    ///     <see cref="TimerDeliveryGraceMilliseconds" /> of slack against this deadline.
    /// </remarks>
    /// <param name="timeoutThreshold">The timeout threshold to apply.</param>
    /// <returns>
    ///     The expected exit deadline, on the <see cref="Environment.TickCount64" /> basis.
    /// </returns>
    internal static long CalculateExpectedExitTick(TimeSpan timeoutThreshold)
        => Environment.TickCount64 + (long)timeoutThreshold.TotalMilliseconds;


    /// <summary>
    ///     Handles exceptions thrown during process cancellation by deciding
    ///     whether to re-throw or silently swallow them.
    /// </summary>
    /// <param name="expectedExitTick">
    ///     The monotonic deadline (<see cref="Environment.TickCount64" /> basis) by which the
    ///     process was expected to exit, captured when the wait began.
    ///     Callers must not re-derive this value inside an exception handler; doing so makes the
    ///     difference below measure the configured timeout threshold instead of the actual skew.
    /// </param>
    /// <param name="cancellationReason">The determined reason for the cancellation.</param>
    /// <param name="exitConfiguration">The exit configuration controlling exception behaviour.</param>
    /// <param name="exception">The exception to evaluate.</param>
    /// <param name="childState">
    ///     Optional snapshot of the child process state captured when the cancellation occurred,
    ///     attached to the thrown timeout/cancellation exception for diagnostics.
    /// </param>
    /// <remarks>
    ///     When <see cref="ProcessExitConfiguration.ExceptionBehaviour" /> is
    ///     <see cref="ProcessExceptionBehaviour.AllowExceptionsIfUnexpected" /> and the reason is a
    ///     timeout or an unknown reason, only an <see cref="OperationCanceledException" /> raised
    ///     within 1 second of <paramref name="expectedExitTick" />, an on-time timeout raised by
    ///     CliInvoke's own cancellation machinery, is swallowed. A cancellation exception outside
    ///     that window propagates, and so does every non-cancellation exception (I/O errors, access
    ///     violations, etc.) regardless of timing: the wall-clock window discriminates on-time
    ///     timeouts, never genuine failures.
    /// </remarks>
    /// <exception cref="Exception">Rethrown when the exception behaviour configuration permits it.</exception>
    internal static void HandleCancellationExceptions(long expectedExitTick,
        CancellationReason cancellationReason, ProcessExitConfiguration exitConfiguration,
        Exception exception, ProcessDiagnostic? childState = null)
    {
        // The caller-configured cancellation knob overrides the default
        // ExceptionBehaviour heuristics: when CancellationThrowsException is set, any
        // OperationCanceledException raised by the cancellation machinery propagates to
        // the caller, regardless of whether the cancellation was user-requested or
        // timer-driven (see troubleshooting docs). Genuine (non-cancellation) failures
        // remain governed by ExceptionBehaviour below.
        if (exitConfiguration.CancellationThrowsException && exception is OperationCanceledException oceThrow)
        {
            int pid = childState?.ProcessId ?? 0;
            TimeSpan elapsed = childState?.Elapsed ?? TimeSpan.Zero;
            if (cancellationReason == CancellationReason.Timeout)
                throw new CliInvokeTimeoutException(oceThrow.Message, oceThrow, oceThrow.CancellationToken, pid, childState, elapsed);
            throw new CliInvokeCanceledException(oceThrow.Message, oceThrow, oceThrow.CancellationToken, pid, childState, elapsed);
        }

        TimeSpan difference = TimeSpan.FromMilliseconds(
            Math.Abs(expectedExitTick - Environment.TickCount64));

        switch (cancellationReason)
        {
            case CancellationReason.RequestedCancellation:
            {
                if (exitConfiguration.ExceptionBehaviour is ProcessExceptionBehaviour
                        .AllowExceptions)
                {
                    if (exception is OperationCanceledException oceReq)
                    {
                        int pid = childState?.ProcessId ?? 0;
                        TimeSpan elapsed = childState?.Elapsed ?? TimeSpan.Zero;
                        throw new CliInvokeCanceledException(oceReq.Message, oceReq, oceReq.CancellationToken, pid, childState, elapsed);
                    }
                    throw exception;
                }

                break;
            }
            case CancellationReason.Timeout or CancellationReason.NotKnown:
            {
                if (exitConfiguration.ExceptionBehaviour
                    == ProcessExceptionBehaviour.AllowExceptions)
                {
                    if (exception is OperationCanceledException oceAllow)
                    {
                        int pid = childState?.ProcessId ?? 0;
                        TimeSpan elapsed = childState?.Elapsed ?? TimeSpan.Zero;
                        if (cancellationReason == CancellationReason.Timeout)
                            throw new CliInvokeTimeoutException(oceAllow.Message, oceAllow, oceAllow.CancellationToken, pid, childState, elapsed);
                        throw new CliInvokeCanceledException(oceAllow.Message, oceAllow, oceAllow.CancellationToken, pid, childState, elapsed);
                    }
                    throw exception;
                }

                // AllowExceptionsIfUnexpected swallows only cancellation exceptions raised
                // on time (the expected kill timer or caller token firing at the threshold).
                // Non-cancellation exceptions are genuine failures and always propagate;
                // never let a wall-clock coincidence silently eat them (L7b).
                if (exitConfiguration.ExceptionBehaviour
                        == ProcessExceptionBehaviour.AllowExceptionsIfUnexpected
                    && (exception is not OperationCanceledException
                        || difference > TimeSpan.FromSeconds(1)))
                {
                    if (exception is OperationCanceledException oceUnexpected)
                    {
                        int pid = childState?.ProcessId ?? 0;
                        TimeSpan elapsed = childState?.Elapsed ?? TimeSpan.Zero;
                        if (cancellationReason == CancellationReason.Timeout)
                            throw new CliInvokeTimeoutException(oceUnexpected.Message, oceUnexpected, oceUnexpected.CancellationToken, pid, childState, elapsed);
                        throw new CliInvokeCanceledException(oceUnexpected.Message, oceUnexpected, oceUnexpected.CancellationToken, pid, childState, elapsed);
                    }
                    throw exception;
                }

                break;
            }
        }
    }
}
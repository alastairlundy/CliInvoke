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
    ///     Both the deadline and the observation are read from <see cref="Environment.TickCount64" /> —
    ///     the same monotonic clock the runtime's timer queue fires <c>CancelAfter</c> and
    ///     <c>Task.Delay</c> against — so a kill timer that fires marginally ahead of the wall clock
    ///     can never be misclassified as <see cref="CancellationReason.NotKnown" />. Classifying on
    ///     <c>DateTime.UtcNow</c> instead mixed clock domains and did exactly that
    ///     (dotnet/runtime#100455), which surfaced an on-time timeout as a cancellation.
    /// </remarks>
    internal static CancellationReason GetCancellationReason(long expectedExitTick,
        CancellationToken cancellationToken)
    {
        // Make the check atomic by capturing both values before making the decision
        bool isCancellationRequested = cancellationToken.IsCancellationRequested;
        long cancellationTick = Environment.TickCount64;

        if (isCancellationRequested) return CancellationReason.RequestedCancellation;

        return cancellationTick >= expectedExitTick
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
    ///     The threshold is truncated to whole milliseconds — the same conversion the runtime's
    ///     timer queue applies to its due times — and callers capture this deadline before arming
    ///     their timer, so on the shared monotonic clock the armed timer can never fire before it.
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
    ///     within 1 second of <paramref name="expectedExitTick" /> — an on-time timeout raised by
    ///     CliInvoke's own cancellation machinery — is swallowed. A cancellation exception outside
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
                // Non-cancellation exceptions are genuine failures and always propagate —
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
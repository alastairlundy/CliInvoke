/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Processes.Internal.Cancellation;

/// <summary>
/// 
/// </summary>
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
    ///     (dotnet/runtime#100455), which left a timer kill reporting <c>Canceled=false</c>.
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
    /// <remarks>
    ///     When <see cref="ProcessExitConfiguration.ExceptionBehaviour" /> is
    ///     <see cref="ProcessExceptionBehaviour.AllowExceptionsIfUnexpected" /> and the reason is a
    ///     timeout or an unknown reason, the exception is re-thrown only if the cancellation
    ///     resolved more than 1 second before or after <paramref name="expectedExitTick" />.
    ///     Exceptions raised by an on-time timeout cancellation resolve within milliseconds of the
    ///     expected exit time and are swallowed; genuine failures (I/O errors, access violations,
    ///     etc.) resolve outside that window and propagate to the caller.
    /// </remarks>
    /// <exception cref="Exception">Rethrown when the exception behaviour configuration permits it.</exception>
    internal static void HandleCancellationExceptions(long expectedExitTick,
        CancellationReason cancellationReason, ProcessExitConfiguration exitConfiguration,
        Exception exception)
    {
        // The caller-configured cancellation knob overrides the default
        // ExceptionBehaviour heuristics: when CancellationThrowsException is set, any
        // OperationCanceledException raised by the cancellation machinery propagates to
        // the caller, regardless of whether the cancellation was user-requested or
        // timer-driven (see troubleshooting docs). Genuine (non-cancellation) failures
        // remain governed by ExceptionBehaviour below.
        if (exitConfiguration.CancellationThrowsException && exception is OperationCanceledException)
            throw exception;

        TimeSpan difference = TimeSpan.FromMilliseconds(
            Math.Abs(expectedExitTick - Environment.TickCount64));

        switch (cancellationReason)
        {
            case CancellationReason.RequestedCancellation:
            {
                if (exitConfiguration.ExceptionBehaviour is ProcessExceptionBehaviour
                        .AllowExceptions)
                    throw exception;

                break;
            }
            case CancellationReason.Timeout or CancellationReason.NotKnown:
            {
                if (exitConfiguration.ExceptionBehaviour
                    == ProcessExceptionBehaviour.AllowExceptions || (exitConfiguration
                            .ExceptionBehaviour
                        == ProcessExceptionBehaviour.AllowExceptionsIfUnexpected &&
                        difference > TimeSpan.FromSeconds(1)))
                    throw exception;

                break;
            }
        }
    }
}
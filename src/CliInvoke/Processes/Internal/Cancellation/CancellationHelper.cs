/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Processes.Internal.Cancellation;

internal static class CancellationHelper
{
    /// <summary>
    ///     Determines the reason for the cancellation of a process.
    /// </summary>
    /// <param name="expectedExitTime">The expected exit time of the process.</param>
    /// <param name="cancellationToken">
    ///     The token associated with the cancellation, used to determine the
    ///     reason.
    /// </param>
    /// <returns>A string describing the reason for the cancellation.</returns>
    internal static CancellationReason GetCancellationReason(DateTime expectedExitTime,
        CancellationToken cancellationToken)
    {
        // Make the check atomic by capturing both values before making the decision
        bool isCancellationRequested = cancellationToken.IsCancellationRequested;
        DateTime cancellationTime = DateTime.UtcNow;

        if (isCancellationRequested) return CancellationReason.RequestedCancellation;

        return cancellationTime >= expectedExitTime
            ? CancellationReason.Timeout
            : CancellationReason.NotKnown;
    }

    /// <summary>
    ///     Calculates the expected exit time of a process using its specified
    ///     <see cref="ProcessExitConfiguration" /> object.
    /// </summary>
    /// <param name="exitConfiguration">The exit configuration to use.</param>
    /// <returns>The calculated expected exit time for a process.</returns>
    internal static DateTime CalculateExpectedExitTime(ProcessExitConfiguration exitConfiguration)
        => DateTime.UtcNow.Add(exitConfiguration.TimeoutPolicy.TimeoutThreshold);


    /// <summary>
    ///     Handles exceptions thrown during process cancellation by deciding
    ///     whether to re-throw or silently swallow them.
    /// </summary>
    /// <param name="expectedExitTime">
    ///     The time at which the process was expected to exit, captured when the wait began.
    ///     Callers must not re-derive this value inside an exception handler; doing so makes the
    ///     difference below measure the configured timeout threshold instead of the actual skew.
    /// </param>
    /// <param name="cancellationReason">The determined reason for the cancellation.</param>
    /// <param name="exitConfiguration">The exit configuration controlling exception behaviour.</param>
    /// <param name="exception">The exception to evaluate.</param>
    /// <remarks>
    ///     When <see cref="ProcessExitConfiguration.ExceptionBehaviour" /> is
    ///     <see cref="ProcessExceptionBehaviour.AllowExceptionsIfUnexpected" /> and the reason is a
    ///     timeout or an unknown reason, only an <see cref="OperationCanceledException" /> raised
    ///     within 1 second of <paramref name="expectedExitTime" /> — an on-time timeout raised by
    ///     CliInvoke's own cancellation machinery — is swallowed. A cancellation exception outside
    ///     that window propagates, and so does every non-cancellation exception (I/O errors, access
    ///     violations, etc.) regardless of timing: the wall-clock window discriminates on-time
    ///     timeouts, never genuine failures.
    /// </remarks>
    /// <exception cref="Exception">Rethrown when the exception behaviour configuration permits it.</exception>
    internal static void HandleCancellationExceptions(DateTime expectedExitTime,
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

        DateTime actualExitTime = DateTime.UtcNow;
        TimeSpan difference = TimeSpan.FromTicks(Math.Abs(expectedExitTime.Ticks - actualExitTime.Ticks));

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
                    == ProcessExceptionBehaviour.AllowExceptions)
                    throw exception;

                // AllowExceptionsIfUnexpected swallows only cancellation exceptions raised
                // on time (the expected kill timer or caller token firing at the threshold).
                // Non-cancellation exceptions are genuine failures and always propagate —
                // never let a wall-clock coincidence silently eat them (L7b).
                if (exitConfiguration.ExceptionBehaviour
                        == ProcessExceptionBehaviour.AllowExceptionsIfUnexpected
                    && (exception is not OperationCanceledException
                        || difference > TimeSpan.FromSeconds(1)))
                    throw exception;

                break;
            }
        }
    }
}
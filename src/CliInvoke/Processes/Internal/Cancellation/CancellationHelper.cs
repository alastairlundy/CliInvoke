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
    ///     timeout or an unknown reason, the exception is re-thrown only if the cancellation
    ///     resolved more than 1 second before or after <paramref name="expectedExitTime" />.
    ///     Exceptions raised by an on-time timeout cancellation resolve within milliseconds of the
    ///     expected exit time and are swallowed; genuine failures (I/O errors, access violations,
    ///     etc.) resolve outside that window and propagate to the caller.
    /// </remarks>
    /// <exception cref="Exception">Rethrown when the exception behaviour configuration permits it.</exception>
    internal static void HandleCancellationExceptions(DateTime expectedExitTime,
        CancellationReason cancellationReason, ProcessExitConfiguration exitConfiguration,
        Exception exception)
    {
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
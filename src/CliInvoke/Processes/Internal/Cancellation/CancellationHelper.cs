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
    /// <param name="expectedExitTime">The time at which the process was expected to exit.</param>
    /// <param name="cancellationReason">The determined reason for the cancellation.</param>
    /// <param name="exitConfiguration">The exit configuration controlling exception behaviour.</param>
    /// <param name="exception">The exception to evaluate.</param>
    /// <remarks>
    ///     When <see cref="ProcessExitConfiguration.ExceptionBehaviour" /> is
    ///     <see cref="ProcessExceptionBehaviour.AllowExceptionsIfUnexpected" />, the exception is
    ///     only re-thrown if the process exited more than 1 second after its expected exit time.
    ///     This 1-second heuristic accommodates minor clock skew while ensuring that genuine
    ///     errors (I/O errors, access violations, etc.) that occur within a tight window around
    ///     the expected exit time are not silently swallowed.
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
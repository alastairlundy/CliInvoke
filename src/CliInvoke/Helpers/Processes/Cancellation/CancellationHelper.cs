/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using DotExtensions.Dates;

namespace CliInvoke.Helpers.Processes.Cancellation;

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
    {
        return DateTime.UtcNow.Add(exitConfiguration.TimeoutPolicy.TimeoutThreshold);
    }


    /// <summary>
    /// </summary>
    /// <param name="expectedExitTime"></param>
    /// <param name="cancellationReason"></param>
    /// <param name="exitConfiguration"></param>
    /// <param name="exception"></param>
    /// <exception cref="Exception"></exception>
    internal static void HandleCancellationExceptions(DateTime expectedExitTime,
        CancellationReason cancellationReason, ProcessExitConfiguration exitConfiguration,
        Exception exception)
    {
        DateTime actualExitTime = DateTime.UtcNow;
        TimeSpan difference = expectedExitTime.Difference(actualExitTime);

        switch (cancellationReason)
        {
            case CancellationReason.RequestedCancellation:
            {
                if (exitConfiguration.ExceptionBehaviour
                    == ProcessExceptionBehaviour.AllowExceptions || (exitConfiguration
                            .ExceptionBehaviour
                        == ProcessExceptionBehaviour.AllowExceptionsIfUnexpected &&
                        difference > TimeSpan.FromSeconds(10)))
                    throw exception;

                break;
            }
            case CancellationReason.Timeout or CancellationReason.NotKnown:
            {
                if (exitConfiguration.ExceptionBehaviour
                    == ProcessExceptionBehaviour.AllowExceptions || (exitConfiguration
                            .ExceptionBehaviour
                        == ProcessExceptionBehaviour.AllowExceptionsIfUnexpected &&
                        difference > TimeSpan.FromSeconds(10)))
                    throw exception;

                break;
            }
        }
    }
}
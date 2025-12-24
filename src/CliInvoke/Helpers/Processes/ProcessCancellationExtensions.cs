/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading;

using CliInvoke.Helpers.Processes.Cancellation;

using DotExtensions.Dates;

namespace CliInvoke.Helpers.Processes;

/// <summary>
///
/// </summary>
internal static class ProcessCancellationExtensions
{
    /// <param name="process">The process to cancel.</param>
    extension(Process process)
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("android")]
        internal async Task WaitForExitOrTimeoutAsync(ProcessExitConfiguration processExitConfiguration,
            CancellationToken cancellationToken = default
        )
        {
            switch (processExitConfiguration.TimeoutPolicy.CancellationMode)
            {
                case ProcessCancellationMode.None:
                {
                    await process.WaitForExitAsync(cancellationToken);
                    return;
                }
                case ProcessCancellationMode.Graceful:
                {
                    await WaitForExitOrGracefulTimeoutAsync(
                        process,
                        processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                        processExitConfiguration.CancellationExceptionBehavior
                    );
                    return;
                }
                case ProcessCancellationMode.Forceful:
                    await WaitForExitOrForcefulTimeoutAsync(
                        process,
                        processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                        processExitConfiguration.CancellationExceptionBehavior
                    );
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Asynchronously waits for the process to exit or for the <paramref name="timeoutThreshold"/> to be exceeded, whichever is sooner.
        /// </summary>
        /// <param name="timeoutThreshold">The delay to wait before requesting cancellation.</param>
        /// <param name="cancellationExceptionBehavior"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the timeout threshold is less than 0.</exception>
        /// <exception cref="NotSupportedException">Thrown if run on a remote computer or device.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("android")]
        private async Task WaitForExitOrGracefulTimeoutAsync(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior
        )
        {
            if (timeoutThreshold < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException();

            DateTime expectedExitTime = DateTime.UtcNow.Add(timeoutThreshold);

            CancellationTokenSource cts = new();

            cts.CancelAfter(timeoutThreshold);

            if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowException)
            {
                await process.WaitForExitAsync(cts.Token);
                return;
            }

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (TaskCanceledException)
            {
                DateTime actualExitTime = DateTime.UtcNow;
                TimeSpan difference = expectedExitTime.Difference(actualExitTime);

                if (
                    cancellationExceptionBehavior
                    == ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected
                )
                {
                    if (difference > TimeSpan.FromSeconds(10))
                    {
                        throw;
                    }
                }
            }
            finally
            {
                if (!process.HasExited)
                    process.Kill();
            }
        }

    }
}

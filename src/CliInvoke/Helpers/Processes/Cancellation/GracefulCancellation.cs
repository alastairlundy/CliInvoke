/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using DotExtensions.Dates;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static partial class GracefulCancellation
{
    internal const int GracefulTimeoutWaitSeconds = 30;
    
    extension(ProcessWrapper process)
    {
        /// <summary>
        /// Asynchronously waits for the process to exit or for the <paramref name="timeoutThreshold"/> to be exceeded, whichever is sooner.
        /// </summary>
        /// <param name="exitConfiguration"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="fallbackToForceful"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the timeout threshold is less than 0.</exception>
        /// <exception cref="NotSupportedException">Thrown if run on a remote computer or device.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal async Task WaitForExitOrGracefulTimeoutAsync(ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken,
            bool fallbackToForceful = true)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(exitConfiguration.TimeoutPolicy.TimeoutThreshold, TimeSpan.Zero);

            DateTime expectedExitTime = DateTime.UtcNow.Add(exitConfiguration.TimeoutPolicy.TimeoutThreshold);
            
            Task<bool> gracefulInterruptCancellation = process.GracefulInterruptCancellation(timeoutThreshold, 
                cancellationExceptionBehavior, cancellationToken);
            
            await Task.WhenAny([
                process.WaitForExitAsync(cancellationToken),
                gracefulInterruptCancellation,
                process.GracefulCancellationWithCancelToken(timeoutThreshold + TimeSpan.FromSeconds(GracefulTimeoutWaitSeconds),
                    cancellationExceptionBehavior, expectedExitTime)
            ]);

            await Task.WhenAny([Task.Delay(500, cancellationToken), process.WaitForExitAsync(cancellationToken)]);
            
            if (!process.HasExited && fallbackToForceful)
            {
                process.ForcefulExit();
            }
        }

        private Task<bool> GracefulInterruptCancellation(TimeSpan timeoutThreshold,
            ProcessExceptionBehaviour cancellationExceptionBehavior, CancellationToken cancellationToken)
        {
            Task<bool> gracefulInterruptCancellation = !OperatingSystem.IsWindows()
                ? process.CancelWithInterruptOnUnix(timeoutThreshold, cancellationExceptionBehavior, cancellationToken)
                : process.CancelWithInterruptOnWindows(timeoutThreshold, cancellationExceptionBehavior, cancellationToken);
            return gracefulInterruptCancellation;
        }

        private async Task GracefulCancellationWithCancelToken(TimeSpan timeoutThreshold,
            ProcessExceptionBehaviour cancellationExceptionBehavior, DateTime expectedExitTime)
        {
            CancellationTokenSource cts = new();

            cts.CancelAfter(timeoutThreshold);

            if (cancellationExceptionBehavior == ProcessExceptionBehaviour.AllowException)
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

                if (cancellationExceptionBehavior
                    == ProcessExceptionBehaviour.AllowExceptionIfUnexpected)
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
                    process.ForcefulExit();
            }
        }
    }
}
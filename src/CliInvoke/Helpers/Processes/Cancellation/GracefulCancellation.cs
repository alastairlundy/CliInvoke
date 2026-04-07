/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using DotExtensions.Dates;
using System.Threading;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static partial class GracefulCancellation
{
    internal const int GracefulTimeoutWaitSeconds = 30;
    
    extension(ProcessWrapper process)
    {
        /// <summary>
        /// Asynchronously waits for the process to exit or for the <paramref name="timeoutThreshold"/> to be exceeded, whichever is sooner.
        /// </summary>
        /// <param name="timeoutThreshold">The delay to wait before requesting cancellation.</param>
        /// <param name="cancellationExceptionBehavior"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="fallbackToForceful"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the timeout threshold is less than 0.</exception>
        /// <exception cref="NotSupportedException">Thrown if run on a remote computer or device.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal async Task WaitForExitOrGracefulTimeoutAsync(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior, CancellationToken cancellationToken,
            bool fallbackToForceful = true)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(timeoutThreshold, TimeSpan.Zero);

            // Reset forceful exit attempt flag for this operation
            process.ForcefulExitAttempted = false;
            
            // Acquire synchronization lock to prevent concurrent cancellation operations
            await process.CancellationSemaphore.WaitAsync(cancellationToken);
            try
            {
                DateTime expectedExitTime = DateTime.UtcNow.AddTicks(timeoutThreshold.Ticks);
                
                Task<bool> gracefulInterruptCancellation = process.GracefulInterruptCancellation(timeoutThreshold, 
                    cancellationExceptionBehavior, cancellationToken);
                
                // Wait for any of the three tasks to complete
                Task[] tasks =
                [
                    process.WaitForExitAsync(cancellationToken),
                    gracefulInterruptCancellation,
                    process.GracefulCancellationWithCancelToken(
                        timeoutThreshold + TimeSpan.FromSeconds(GracefulTimeoutWaitSeconds),
                        cancellationExceptionBehavior, expectedExitTime)
                ];
                
                // Wait for any task to complete and get its index
                Task completedTask = await Task.WhenAny(tasks);
                int completedTaskIndex = Array.IndexOf(tasks, completedTask);
                
                // Only proceed with forceful exit if:
                // 1. The completed task was the timeout task (index 2)
                // 2. The process hasn't exited yet
                // 3. Fallback to forceful is enabled
                if (completedTaskIndex == 2 && !process.HasExited && fallbackToForceful)
                {
                    await ProcessWrapper.SafeForcefulExit(process, cancellationExceptionBehavior).ConfigureAwait(false);
                }
            }
            finally
            {
                process.CancellationSemaphore.Release();
            }
        }

        private Task<bool> GracefulInterruptCancellation(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior, CancellationToken cancellationToken)
        {
            Task<bool> gracefulInterruptCancellation = !OperatingSystem.IsWindows()
                ? process.CancelWithInterruptOnUnix(timeoutThreshold, cancellationExceptionBehavior, cancellationToken)
                : process.CancelWithInterruptOnWindows(timeoutThreshold, cancellationExceptionBehavior, cancellationToken);
            return gracefulInterruptCancellation;
        }

        private async Task GracefulCancellationWithCancelToken(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior, DateTime expectedExitTime)
        {
            // Use using statement to ensure proper disposal of CancellationTokenSource
            using CancellationTokenSource cts = new();

            cts.CancelAfter(timeoutThreshold);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (TaskCanceledException)
            {
                DateTime actualExitTime = DateTime.UtcNow;
                TimeSpan difference = expectedExitTime.Difference(actualExitTime);

                if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowException || (cancellationExceptionBehavior
                        == ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected && (difference > TimeSpan.FromSeconds(10))))
                {
                    throw;
                }
            }
            // Note: We don't call ForcefulExit here to prevent double invocation
            // It's handled in the calling method after checking which task completed
        }
        
        /// <summary>
        /// Safely attempts forceful exit, ensuring it's only called once per process lifecycle.
        /// </summary>
        /// <param name="proc">The process to forcefully exit.</param>
        /// <param name="cancellationExceptionBehavior">Behavior for handling cancellation exceptions.</param>
        private static async Task SafeForcefulExit(ProcessWrapper proc, ProcessCancellationExceptionBehavior cancellationExceptionBehavior)
        {
            // Only attempt forceful exit if it hasn't been attempted before and process hasn't exited
            await proc.ForcefulExitLock.WaitAsync(CancellationToken.None);
            
            try
            {
                if (!proc.ForcefulExitAttempted)
                {
                    if (!proc.HasExited)
                    {
                        proc.ForcefulExitAttempted = true;
                        proc.ForcefulExit(cancellationExceptionBehavior);
                    }
                }
            }
            finally
            {
                proc.ForcefulExitLock.Release();
            }
        }
    }
}
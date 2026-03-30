/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static class GracefulCancellation
{
    internal const int GracefulTimeoutWaitSeconds = 30;
    
    extension(ProcessWrapper process)
    {
        /// <summary>
        /// Asynchronously waits for the process to exit or for the exit configuration's timeout policy threshold to be exceeded, whichever is sooner.
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
            
            Task<bool> gracefulInterruptCancellation = process.GracefulInterruptCancellation(exitConfiguration.TimeoutPolicy.TimeoutThreshold, 
                exitConfiguration, cancellationToken);
            
            await Task.WhenAny([
                process.WaitForExitAsync(cancellationToken),
                gracefulInterruptCancellation,
            ]);

            await Task.WhenAny([Task.Delay(500, cancellationToken), process.WaitForExitAsync(cancellationToken)]);
            
            if (!process.HasExited && fallbackToForceful)
            {
                process.ForcefulExit();
            }
        }

        internal Task<bool> GracefulInterruptCancellation(TimeSpan timeoutThreshold,
            ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
        {
            Task<bool> gracefulInterruptCancellation = !OperatingSystem.IsWindows()
                ? process.CancelWithInterruptOnUnix(timeoutThreshold, exitConfiguration, cancellationToken)
                : process.CancelWithInterruptOnWindows(timeoutThreshold, exitConfiguration, cancellationToken);
            return gracefulInterruptCancellation;
        }
    }
}
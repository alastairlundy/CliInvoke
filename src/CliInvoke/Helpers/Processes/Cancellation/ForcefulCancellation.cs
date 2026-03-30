/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using DotExtensions.Dates;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static class ForcefulCancellation
{
    extension(ProcessWrapper process)
    {
        internal void ForcefulExit()
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
                process.Kill();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exitConfiguration"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal async Task WaitForExitOrForcefulTimeoutAsync(
            ProcessExitConfiguration exitConfiguration,
            CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(exitConfiguration.TimeoutPolicy.TimeoutThreshold, TimeSpan.Zero);

            DateTime expectedExitTime = DateTime.UtcNow.Add(exitConfiguration.TimeoutPolicy.TimeoutThreshold);

            CancellationTokenSource cts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            if(exitConfiguration.TimeoutPolicy.TimeoutThreshold > TimeSpan.Zero)
                cts.CancelAfter(exitConfiguration.TimeoutPolicy.TimeoutThreshold);

            CancellationToken actualCancellationToken = cts.Token;

            CancellationReason cancellationReason = CancellationReason.NotKnown;

            actualCancellationToken.Register(() =>
            {
                cancellationReason = CancellationHelper.GetCancellationReason(expectedExitTime, cancellationToken);
            });
            
            try
            {
                await process.WaitForExitAsync(actualCancellationToken);
            }
            catch (Exception exception)
            {
                CancellationHelper.HandleCancellationExceptions(expectedExitTime, cancellationReason, exitConfiguration, exception);
            }
            finally
            {
                process.ForcefulExit();
            }
        }
    }
}
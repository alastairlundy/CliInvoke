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
        internal void ForcefulExit(ProcessCancellationExceptionBehavior cancellationExceptionBehavior)
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
                process.Kill(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutThreshold"></param>
        /// <param name="cancellationExceptionBehavior"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal async Task WaitForExitOrForcefulTimeoutAsync(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior,
            CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(timeoutThreshold, TimeSpan.Zero);

            DateTime expectedExitTime = DateTime.UtcNow.Add(timeoutThreshold);

            try
            {
                Task waitForExit = process.WaitForExitAsync(cancellationToken);
                Task delay = Task.Delay(timeoutThreshold, cancellationToken);

                await Task.WhenAny(delay, waitForExit);
            }
            catch (TaskCanceledException)
            {
                DateTime actualExitTime = DateTime.UtcNow;
                TimeSpan difference = expectedExitTime.Difference(actualExitTime);

                if (cancellationExceptionBehavior ==
                    ProcessCancellationExceptionBehavior.AllowException)
                {
                    throw;
                }

                if (cancellationExceptionBehavior ==
                    ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected && difference > TimeSpan.FromSeconds(30))
                {
                    throw;
                }
            }
            catch (Exception)
            {
                if (cancellationExceptionBehavior ==
                    ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected ||
                    cancellationExceptionBehavior ==
                    ProcessCancellationExceptionBehavior.AllowException)
                {
                    throw;
                }
            }
            finally
            {
                process.ForcefulExit(cancellationExceptionBehavior);
            }
        }
    }
}
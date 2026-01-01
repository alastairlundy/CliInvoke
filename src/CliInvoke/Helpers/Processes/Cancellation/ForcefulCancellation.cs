/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading;

using DotExtensions.Dates;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static class ForcefulCancellation
{
    /// <param name="process"></param>
    extension(Process process)
    {
        internal void ForcefulExit(ProcessCancellationExceptionBehavior cancellationExceptionBehavior,
            DateTime expectedExitTime)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch (Exception)
            {
                DateTime actualExitTime = DateTime.UtcNow;
                TimeSpan difference = expectedExitTime.Difference(actualExitTime);

                if (cancellationExceptionBehavior
                    == ProcessCancellationExceptionBehavior.SuppressException)
                {
                    return;
                }
                if (cancellationExceptionBehavior
                    == ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected
                    || cancellationExceptionBehavior
                    == ProcessCancellationExceptionBehavior.AllowException)
                {
                    if (
                        difference > TimeSpan.FromSeconds(10)
                        || cancellationExceptionBehavior
                        == ProcessCancellationExceptionBehavior.AllowException
                    )
                    {
                        throw;
                    }
                }
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
        [SupportedOSPlatform("maccatalyst")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("android")]
        internal async Task WaitForExitOrForcefulTimeoutAsync(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior, CancellationToken cancellationToken)
        {
            if (timeoutThreshold < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException();

            DateTime expectedExitTime = DateTime.UtcNow.Add(timeoutThreshold);

            Task waitForExit = process.WaitForExitAsync(cancellationToken);

            Task delay = Task.Delay(timeoutThreshold, cancellationToken);

            await Task.WhenAny(delay, waitForExit);

            process.ForcefulExit(cancellationExceptionBehavior, expectedExitTime);
        }
    }
}
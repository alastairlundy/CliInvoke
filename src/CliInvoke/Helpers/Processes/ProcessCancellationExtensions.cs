/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

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
            if (processExitConfiguration.TimeoutPolicy.TimeoutThreshold <= TimeSpan.Zero)
            {
                await process.WaitForExitNoTimeoutAsync(processExitConfiguration.CancellationExceptionBehavior, cancellationToken);
                return;
            }
            
            switch (processExitConfiguration.TimeoutPolicy.CancellationMode)
            {
                case ProcessCancellationMode.None:
                {
                    await process.WaitForExitNoTimeoutAsync(processExitConfiguration.CancellationExceptionBehavior, cancellationToken);
                    return;
                }
                case ProcessCancellationMode.Graceful:
                {
                    await process.WaitForExitOrGracefulTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                        processExitConfiguration.CancellationExceptionBehavior, cancellationToken
                    );
                    return;
                }
                case ProcessCancellationMode.Forceful:
                    await process.WaitForExitOrForcefulTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                        processExitConfiguration.CancellationExceptionBehavior,
                        cancellationToken
                    );
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        private async Task WaitForExitNoTimeoutAsync(ProcessCancellationExceptionBehavior cancellationExceptionBehavior, CancellationToken cancellationToken = default)
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowException)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected)
                {
                    throw;
                }
            }
            finally
            {
                //Add graceful SIGINT/SIGTERM signal sending here.
            }
        }

        /// <summary>
        /// Asynchronously waits for the process to exit or for the <paramref name="timeoutThreshold"/> to be exceeded, whichever is sooner.
        /// </summary>
        /// <param name="timeoutThreshold">The delay to wait before requesting cancellation.</param>
        /// <param name="cancellationExceptionBehavior"></param>
        /// <param name="cancellationToken"></param>
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
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior, CancellationToken cancellationToken)
        {
            DateTime expectedExitTime = DateTime.UtcNow.Add(timeoutThreshold);

            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            cts.CancelAfter(timeoutThreshold);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (TaskCanceledException)
            {
                DateTime actualExitTime = DateTime.UtcNow;
                TimeSpan difference = expectedExitTime.Difference(actualExitTime);

                if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowException)
                {
                    throw;
                }
                if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected)
                {
                    if (difference > TimeSpan.FromSeconds(10))
                    {
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                if (cancellationExceptionBehavior ==
                    ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected)
                {
                    throw;
                }
            }
            finally
            {
                if (!process.HasExited)
                    process.Kill();
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
        private async Task WaitForExitOrForcefulTimeoutAsync(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior,
            CancellationToken cancellationToken)
        {
            try
            {
                Task waitForExit = process.WaitForExitAsync(cancellationToken);

                Task delay = Task.Delay(timeoutThreshold, cancellationToken);

                await Task.WhenAny(delay, waitForExit);

                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch (TaskCanceledException)
            {
                if (cancellationExceptionBehavior ==
                    ProcessCancellationExceptionBehavior.AllowException)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected
                    || cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowException)
                {
                    throw;
                }
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
        }
    }
}

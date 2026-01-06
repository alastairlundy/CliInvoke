/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading;

using CliInvoke.Helpers.Processes.Cancellation;

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
            CancellationToken cancellationToken = default)
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
                        processExitConfiguration.CancellationExceptionBehavior, cancellationToken);
                    return;
                }
                case ProcessCancellationMode.Forceful:
                {
                    await process.WaitForExitOrForcefulTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                        processExitConfiguration.CancellationExceptionBehavior, cancellationToken);
                    return;
                }
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
                // Graceful SIGINT/SIGTERM signal sending here.

                if (!OperatingSystem.IsWindows())
                {
                    Task cancelTask = process.CancelWithInterruptOnUnix(TimeSpan.Zero, cancellationExceptionBehavior, cancellationToken);
                
                    await Task.WhenAny([cancelTask, Task.Delay(5000, cancellationToken)]);
                
                    if (!process.HasExited)
                    {
                        await Task.Delay(100, cancellationToken);

                        await process.WaitForExitOrForcefulTimeoutAsync(TimeSpan.Zero,cancellationExceptionBehavior, cancellationToken);   
                    }
                }
            }
        }
    }
}

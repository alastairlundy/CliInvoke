/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Helpers.Processes.Cancellation;

namespace CliInvoke.Helpers.Processes;

/// <summary>
///
/// </summary>
internal static class ProcessCancellationExtensions
{
    /// <param name="process">The process to cancel.</param>
    extension(ProcessWrapper process)
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal async Task WaitForExitOrTimeoutAsync(ProcessExitConfiguration processExitConfiguration,
            CancellationToken cancellationToken = default)
        {
            if (processExitConfiguration.TimeoutPolicy.TimeoutThreshold <= TimeSpan.Zero ||
                !processExitConfiguration.TimeoutPolicy.Enabled)
            {
                await process.WaitForExitNoTimeoutAsync(processExitConfiguration, cancellationToken);
                return;
            }
            
            switch (processExitConfiguration.TimeoutCancellationPolicy.CancellationMode)
            {
                case ProcessCancellationMode.None:
                {
                    await process.WaitForExitNoTimeoutAsync(processExitConfiguration, cancellationToken);
                    return;
                }
                case ProcessCancellationMode.Graceful:
                {
                    await process.WaitForExitOrGracefulTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                        processExitConfiguration, cancellationToken);
                    return;
                }
                case ProcessCancellationMode.Forceful:
                {
                    await process.WaitForExitOrForcefulTimeoutAsync(processExitConfiguration, cancellationToken);
                    return;
                }
            }
        }

        private async Task WaitForExitNoTimeoutAsync(ProcessExitConfiguration processExitConfiguration,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                await process.WaitForExitOrGracefulTimeoutAsync(processExitConfiguration,
                    cancellationToken, fallbackToForceful:true);
            }
            catch (Exception)
            {
                if (processExitConfiguration.RequestedCancellationPolicy.CancellationExceptionBehaviour
                    == ProcessExceptionBehaviour.AllowExceptionIfUnexpected)
                {
                    throw;
                }
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.ForcefulExit();
                }
            }
        }
    }
}

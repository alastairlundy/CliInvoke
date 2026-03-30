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
            if (processExitConfiguration.TimeoutPolicy.TimeoutThreshold <= TimeSpan.Zero)
            {
                await process.WaitForExitOrCancellationAsync(processExitConfiguration, cancellationToken);
                return;
            }
            
            switch (processExitConfiguration.TimeoutCancellationPolicy.CancellationMode)
            {
                case ProcessCancellationMode.None:
                {
                    await process.WaitForExitOrCancellationAsync(processExitConfiguration, cancellationToken);
                    return;
                }
                case ProcessCancellationMode.Graceful:
                default:
                {
                    await process.WaitForExitOrGracefulTimeoutAsync(processExitConfiguration, cancellationToken);
                    return;
                }
                case ProcessCancellationMode.Forceful:
                {
                    await process.WaitForExitOrForcefulTimeoutAsync(processExitConfiguration, cancellationToken);
                    return;
                }
            }
        }

        private async Task WaitForExitOrCancellationAsync(ProcessExitConfiguration processExitConfiguration,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await process.GracefulInterruptCancellation(TimeSpan.Zero,
                    processExitConfiguration, cancellationToken);
            }
            catch (Exception exception)
            {
                CancellationHelper.HandleCancellationExceptions(
                    CancellationHelper.CalculateExpectedExitTime(processExitConfiguration)
                    , CancellationReason.RequestedCancellation, processExitConfiguration, exception);
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

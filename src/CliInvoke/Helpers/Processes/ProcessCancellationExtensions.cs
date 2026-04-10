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
/// </summary>
internal static class ProcessCancellationExtensions
{
    internal const int GracefulTimeoutWaitSeconds = 5;
    
    /// <param name="process">The process to cancel.</param>
    extension(ProcessWrapper process)
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal async Task WaitForExitOrTimeoutAsync(
            ProcessExitConfiguration processExitConfiguration,
            CancellationToken cancellationToken = default)
        {
            if (processExitConfiguration.TimeoutPolicy.TimeoutThreshold <= TimeSpan.Zero)
            {
                await process.WaitForExitOrCancellationAsync(processExitConfiguration,
                    cancellationToken);
                return;
            }

            switch (processExitConfiguration.TimeoutPolicy.TimeoutExitBehaviour)
            {
                case ProcessExitBehaviour.WaitForExit:
                {
                    await process.WaitForExitOrCancellationAsync(processExitConfiguration,
                        cancellationToken);
                    return;
                }
                case ProcessExitBehaviour.GracefulExit:
                default:
                {
                    await process.WaitForExitOrGracefulTimeoutAsync(processExitConfiguration,
                        cancellationToken);
                    return;
                }
                case ProcessExitBehaviour.ForcefulExit:
                {
                    await process.WaitForExitOrForcefulTimeoutAsync(processExitConfiguration,
                        cancellationToken);
                    return;
                }
            }
        }

        private async Task WaitForExitOrCancellationAsync(
            ProcessExitConfiguration processExitConfiguration,
            CancellationToken cancellationToken = default)
        {
            // Use semaphore to prevent simultaneous cancellation attempts
            if (!await process._cancellationSemaphore.WaitAsync(0, cancellationToken))
            {
                // Another cancellation is already in progress, wait for it to complete
                await process.WaitForExitAsync(cancellationToken);
                return;
            }

            try
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
                    // Recalculate values in exception handler to avoid using stale values
                    DateTime currentExpectedExitTime =
                        CancellationHelper.CalculateExpectedExitTime(processExitConfiguration);
                    CancellationHelper.HandleCancellationExceptions(
                        currentExpectedExitTime
                        , CancellationReason.RequestedCancellation, processExitConfiguration,
                        exception);
                }
            }
            finally
            {
                if (!process.HasExited) process.ForcefulExit();
                process._cancellationSemaphore.Release();
            }
        }
    }

    #region Graceful Cancellation Entrypoint
    extension(ProcessWrapper process)
    {
        /// <summary>
        ///     Asynchronously waits for the process to exit or for the exit configuration's timeout policy
        ///     threshold to be exceeded, whichever is sooner.
        /// </summary>
        /// <param name="exitConfiguration"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="fallbackToForceful"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the timeout threshold is less than 0.</exception>
        /// <exception cref="NotSupportedException">Thrown if run on a remote computer or device.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal async Task WaitForExitOrGracefulTimeoutAsync(
            ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken,
            bool fallbackToForceful = true)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(
                exitConfiguration.TimeoutPolicy.TimeoutThreshold, TimeSpan.Zero);

            // Use semaphore to prevent simultaneous cancellation attempts
            if (!await process._cancellationSemaphore.WaitAsync(0, cancellationToken))
            {
                // Another cancellation is already in progress, wait for it to complete
                await process.WaitForExitAsync(cancellationToken);
                return;
            }

            try
            {
                await Task.WhenAny([
                    process.WaitForExitAsync(cancellationToken),
                    process.GracefulInterruptCancellation(TimeSpan.Zero,
                        exitConfiguration, cancellationToken)
                ]);

                await Task.WhenAny([
                    Task.Delay(500, cancellationToken), process.WaitForExitAsync(cancellationToken)
                ]);

                if (!process.HasExited && fallbackToForceful) process.ForcefulExit();
            }
            finally
            {
                process._cancellationSemaphore.Release();
            }
        }

        private Task<bool> GracefulInterruptCancellation(TimeSpan timeoutThreshold,
            ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
        {
            Task<bool> gracefulInterruptCancellation = !OperatingSystem.IsWindows()
                ? process.CancelWithInterruptOnUnix(timeoutThreshold, exitConfiguration,
                    cancellationToken)
                : process.CancelWithInterruptOnWindows(timeoutThreshold, exitConfiguration,
                    cancellationToken);
            return gracefulInterruptCancellation;
        }
    }
    #endregion
    
    #region Forceful Cancellation
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
            ArgumentOutOfRangeException.ThrowIfLessThan(
                exitConfiguration.TimeoutPolicy.TimeoutThreshold, TimeSpan.Zero);

            DateTime expectedExitTime =
                DateTime.UtcNow.Add(exitConfiguration.TimeoutPolicy.TimeoutThreshold);

            var cts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (exitConfiguration.TimeoutPolicy.TimeoutThreshold > TimeSpan.Zero)
                cts.CancelAfter(exitConfiguration.TimeoutPolicy.TimeoutThreshold);

            CancellationToken actualCancellationToken = cts.Token;

            // Use a local variable to store the cancellation reason to avoid race conditions
            var cancellationReason = CancellationReason.NotKnown;

            actualCancellationToken.Register(() =>
            {
                cancellationReason =
                    CancellationHelper.GetCancellationReason(expectedExitTime,
                        cancellationToken);
            });

            // Use semaphore to prevent simultaneous cancellation attempts
            if (!await process._cancellationSemaphore.WaitAsync(0, cancellationToken))
            {
                // Another cancellation is already in progress, wait for it to complete
                await process.WaitForExitAsync(cancellationToken);
                // Dispose of the linked CTS to prevent resource leaks
                cts.Dispose();
                return;
            }

            try
            {
                await process.WaitForExitAsync(actualCancellationToken);
            }
            catch (Exception exception)
            {
                // Recalculate expected exit time in exception handler to avoid using stale values
                DateTime currentExpectedExitTime =
                    CancellationHelper.CalculateExpectedExitTime(exitConfiguration);
                CancellationHelper.HandleCancellationExceptions(currentExpectedExitTime,
                    cancellationReason, exitConfiguration, exception);
            }
            finally
            {
                process.ForcefulExit();
                // Dispose of the linked CTS to prevent resource leaks

                cts.Dispose();
                process._cancellationSemaphore.Release();
            }
        }
    }
    #endregion
}
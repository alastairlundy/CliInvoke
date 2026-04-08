/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;
// ReSharper disable PartialTypeWithSinglePart

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static partial class UnixGracefulCancellation
{
    private const int Sigint = 2;
    private const int Sigterm = 15;

    private const int DelayBeforeSigintMilliseconds = 3000;

    extension(ProcessWrapper process)
    {
        /// <summary>
        /// </summary>
        /// <param name="timeoutThreshold"></param>
        /// <param name="exitConfiguration"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        internal async Task<bool> CancelWithInterruptOnUnix(TimeSpan timeoutThreshold,
            ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
        {
            // Use semaphore to prevent simultaneous cancellation attempts
            if (!await process._cancellationSemaphore.WaitAsync(0, cancellationToken))
            {
                // Another cancellation is already in progress, wait for it to complete
                await process.WaitForExitAsync(cancellationToken);
                return process.HasExited;
            }

            try
            {
                DateTime expectedExitTime =
                    CancellationHelper.CalculateExpectedExitTime(exitConfiguration);

                // Use a local variable to store the cancellation reason to avoid race conditions
                CancellationReason cancellationReason = CancellationReason.NotKnown;

                // Register the callback to update the cancellation reason
                cancellationToken.Register(() =>
                {
                    cancellationReason =
                        CancellationHelper.GetCancellationReason(expectedExitTime,
                            cancellationToken);
                });

                bool sigIntSuccess = false;

                try
                {
                    if (OperatingSystem.IsWindows() || OperatingSystem.IsIOS() ||
                        OperatingSystem.IsTvOS())
                        throw new PlatformNotSupportedException();

                    await Task.Delay(timeoutThreshold, cancellationToken);

                    bool sigTermSuccess = SendUnixSignal(process.Id, Sigterm);

                    await Task.Delay(DelayBeforeSigintMilliseconds,
                        cancellationToken);

                    if (sigTermSuccess)
                        return true;

                    sigIntSuccess = SendUnixSignal(process.Id, Sigint);
                }
                catch (Exception exception)
                {
                    sigIntSuccess =
                        process.HandleCancellationMode(exitConfiguration, cancellationReason);

                    // Recalculate expected exit time in exception handler to avoid using stale values
                    DateTime currentExpectedExitTime =
                        CancellationHelper.CalculateExpectedExitTime(exitConfiguration);
                    CancellationHelper.HandleCancellationExceptions(currentExpectedExitTime,
                        cancellationReason, exitConfiguration, exception);
                }

                return sigIntSuccess;
            }
            finally
            {
                process._cancellationSemaphore.Release();
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        private bool HandleCancellationMode(ProcessExitConfiguration exitConfiguration,
            CancellationReason cancellationReason)
        {
            switch (cancellationReason)
            {
                case CancellationReason.Timeout:
                {
                    if (exitConfiguration.TimeoutPolicy.TimeoutExitBehaviour ==
                        ProcessExitBehaviour.ForcefulExit)
                    {
                        if (!process.HasExited)
                            process.ForcefulExit();

                        return true;
                    }

                    if (exitConfiguration.TimeoutPolicy.TimeoutExitBehaviour ==
                        ProcessExitBehaviour.GracefulExit)
                    {
                        if (!process.HasExited)
                            return SendUnixSignal(process.Id, Sigint);

                        return true;
                    }

                    break;
                }
                case CancellationReason.RequestedCancellation or CancellationReason.NotKnown:
                default:
                {
                    if (exitConfiguration.RequestedCancellationExitBehaviour ==
                        ProcessExitBehaviour.ForcefulExit)
                    {
                        if (!process.HasExited)
                            process.ForcefulExit();
                        return true;
                    }

                    if (exitConfiguration.RequestedCancellationExitBehaviour ==
                        ProcessExitBehaviour.GracefulExit)
                    {
                        if (!process.HasExited)
                            return SendUnixSignal(process.Id, Sigint);

                        return true;
                    }

                    break;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="processId"></param>
    /// <param name="signalId"></param>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("browser")]
    private static bool SendUnixSignal(int processId, int signalId)
    {
        return kill_libc(processId, signalId) == 0;
    }

#if NETSTANDARD2_0
    [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static extern int kill_libc(int processid, int signal);
#else
    [LibraryImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static partial int kill_libc(int processid, int signal);
#endif
}
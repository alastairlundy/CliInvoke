/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static partial class GracefulCancellation
{
    private const int Sigint = 2;
    private const int Sigterm = 15;

    private const int DelayBeforeSigintMilliseconds = 3000;
    
    extension(Process process)
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutThreshold"></param>
        /// <param name="cancellationExceptionBehavior"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        internal async Task<bool> CancelWithInterruptOnUnix(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior, CancellationToken cancellationToken)
        {
            if(OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException();
            
            await Task.Delay(timeoutThreshold, CancellationToken.None);

            bool sigTermSuccess =  SendSignal(process.Id, Sigterm);

            await Task.Delay(millisecondsDelay: DelayBeforeSigintMilliseconds, CancellationToken.None);

            if (sigTermSuccess)
                return true;

            return SendSignal(process.Id, Sigint);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processId"></param>
    /// <param name="signalId"></param>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("browser")]
    private static bool SendSignal(int processId, int signalId)
    {
        return kill_libc(processId, signalId) == 0;
    }
    
    [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static extern int kill_libc(int processid, int signal);
}

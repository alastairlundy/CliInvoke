/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;
using System.Threading;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static partial class GracefulCancellation
{
    private const int Sigint = 2;
    private const int Sigterm = 15;
    
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
            
            await Task.Delay(timeoutThreshold, cancellationToken);

            SendSignal(process.Id, Sigint);
            SendSignal(process.Id,Sigterm);

            Task<bool> task = await Task.WhenAny(new[]{Task.Run(() =>
            {
                Task.Delay(100, CancellationToken.None);
                return false;
            }, CancellationToken.None), Task.Run(() =>
            {
                process.WaitForExit();
                return true;
            }, CancellationToken.None)});

            return task.Result;
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
    private static void SendSignal(int processId, int signalId)
    {
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            kill_macos(processId, signalId);
        }
        else if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsIOS())
        {
            kill_linux(processId, signalId);
        }
    }
    
    [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static extern int kill_linux(int processid, int signal);

    [DllImport("libSystem", SetLastError = true, EntryPoint = "kill")]
    private static extern int kill_macos(int processId, int signal);
}

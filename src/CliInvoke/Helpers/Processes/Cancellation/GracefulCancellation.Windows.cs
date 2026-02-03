/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static partial class GracefulCancellation
{
    private const uint CtrlCSignalEvent = 0;
    
    extension(ProcessWrapper process)
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutThreshold"></param>
        /// <param name="cancellationExceptionBehavior"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [SupportedOSPlatform("windows")]
        internal async Task<bool> CancelWithInterruptOnWindows(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior, CancellationToken cancellationToken)
        {
            if(!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException();

            bool ctrlCSignalSuccess = false;
            
            try
            {
                await Task.Delay(timeoutThreshold, cancellationToken);

                if (process.HasExited)
                    return true;

                // Allocate a Console to the Process so that it has one it can use.
                bool successfulAttachment = AllocConsoleWin();
                // Attach the allocated console to the process.
                successfulAttachment = successfulAttachment && AttachConsoleWin((uint)process.Id);

                if (!successfulAttachment)
                    return false;

                ctrlCSignalSuccess = SendCtrlCToConsoleWin(CtrlCSignalEvent, 0);
            }
            catch (TaskCanceledException)
            {
                if (cancellationExceptionBehavior is ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected
                    or ProcessCancellationExceptionBehavior.AllowException)
                    throw;
            }
            catch (Exception)
            {
                if (cancellationExceptionBehavior !=
                    ProcessCancellationExceptionBehavior.SuppressException)
                    throw;
            }

            return ctrlCSignalSuccess;
        }
    }

    [DllImport("Kernel32.dll", EntryPoint = "GenerateConsoleCtrlEvent", SetLastError = true)]
    private static extern bool SendCtrlCToConsoleWin(uint ctrlEvent, uint processGroupEventId);

    [DllImport("Kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
    private static extern bool AllocConsoleWin();

    [DllImport("Kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
    private static extern bool AttachConsoleWin(uint processId);
}
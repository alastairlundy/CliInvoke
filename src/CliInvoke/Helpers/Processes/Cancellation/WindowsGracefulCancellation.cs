/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static partial class WindowsGracefulCancellation
{
    private const uint CtrlCSignalEvent = 0;
    
    extension(ProcessWrapper process)
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutThreshold"></param>
        /// <param name="exitConfiguration"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [SupportedOSPlatform("windows")]
        internal async Task<bool> CancelWithInterruptOnWindows(TimeSpan timeoutThreshold,
            ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
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
                if (cancellationExceptionBehavior is ProcessExceptionBehaviour.AllowExceptionIfUnexpected
                    or ProcessExceptionBehaviour.AllowException)
                    throw;
            }
            catch (Exception)
            {
                if (cancellationExceptionBehavior !=
                    ProcessExceptionBehaviour.SuppressException)
                    throw;
            }

            return ctrlCSignalSuccess;
        }
    }

#if NETSTANDARD2_0
    [DllImport("Kernel32.dll", EntryPoint = "GenerateConsoleCtrlEvent", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SendCtrlCToConsoleWin(uint ctrlEvent, uint processGroupEventId);

    [DllImport("Kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsoleWin();

    [DllImport("Kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsoleWin(uint processId);
#else
    [LibraryImport("Kernel32.dll", EntryPoint = "GenerateConsoleCtrlEvent", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SendCtrlCToConsoleWin(uint ctrlEvent, uint processGroupEventId);

    [LibraryImport("Kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsoleWin();

    [LibraryImport("Kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsoleWin(uint processId);
#endif
}
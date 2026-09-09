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
            bool allocatedConsole = false;
            
            try
            {
                await Task.Delay(timeoutThreshold, cancellationToken);

                if (process.HasExited)
                    return true;

                // Only attempt Ctrl+C delivery if we don't already own a console.
                // In console apps, AllocConsole/AttachConsole will fail because
                // we already have a console attached.
                if (GetConsoleWindow() != IntPtr.Zero)
                    return false;

                uint processGroupId = GetProcessGroupId(process);
                
                allocatedConsole = AllocConsoleWin();
                
                bool attached = AttachConsoleWin((uint)process.Id);
                
                if (attached)
                {
                    ctrlCSignalSuccess = SendCtrlCToConsoleWin(CtrlCSignalEvent, processGroupId);
                    if (!ctrlCSignalSuccess)
                    {
                        ctrlCSignalSuccess = SendCtrlCToConsoleWin(1, processGroupId);
                    }
                    await Task.Delay(50, CancellationToken.None);
                }
                else if (allocatedConsole)
                {
                    ctrlCSignalSuccess = SendCtrlCToConsoleWin(CtrlCSignalEvent, processGroupId);
                    if (!ctrlCSignalSuccess)
                    {
                        ctrlCSignalSuccess = SendCtrlCToConsoleWin(1, processGroupId);
                    }
                }
            }
            catch (OperationCanceledException)
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
            finally
            {
                if (allocatedConsole)
                    FreeConsoleWin();
            }

            return ctrlCSignalSuccess;
        }
        
        /// <summary>
        /// Gets the process group ID for the given process.
        /// </summary>
        private static uint GetProcessGroupId(ProcessWrapper targetProcess)
        {
            try
            {
                // Try to get process group ID using NtQueryInformationProcess
                // This is more reliable than using the process ID directly
                if (NtQueryInformationProcess(targetProcess.Handle, 0, out PROCESS_BASIC_INFORMATION pbi, 
                        Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), out _) == 0)
                {
                    // ProcessGroupId is stored in the 5th ULONG in the structure
                    return pbi.ProcessGroupId;
                }
            }
            catch
            {
                // If NtQueryInformationProcess fails, fall back to using process ID
            }
            
            // Fallback: use process ID as group ID
            return (uint)targetProcess.Id;
        }
    }

    /// <summary>
    /// Process basic information structure for NtQueryInformationProcess
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public uint ExitStatus;
        public IntPtr PebBaseAddress;
        public ulong AffinityMask;
        public uint BasePriority;
        public ulong UniqueProcessId;
        public ulong InheritedFromUniqueProcessId;
        public uint ProcessGroupId;
    }

#if NETSTANDARD2_0
    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(
        IntPtr ProcessHandle,
        int ProcessInformationClass,
        out PROCESS_BASIC_INFORMATION ProcessInformation,
        int ProcessInformationLength,
        out int ReturnLength);

    [DllImport("Kernel32.dll", EntryPoint = "GenerateConsoleCtrlEvent", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SendCtrlCToConsoleWin(uint ctrlEvent, uint processGroupEventId);

    [DllImport("Kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsoleWin();

    [DllImport("Kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsoleWin(uint processId);

    [DllImport("Kernel32.dll", EntryPoint = "FreeConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsoleWin();

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();
#else
    [LibraryImport("ntdll.dll", SetLastError = true)]
    private static partial int NtQueryInformationProcess(
        IntPtr ProcessHandle,
        int ProcessInformationClass,
        out PROCESS_BASIC_INFORMATION ProcessInformation,
        int ProcessInformationLength,
        out int ReturnLength);

    [LibraryImport("Kernel32.dll", EntryPoint = "GenerateConsoleCtrlEvent", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SendCtrlCToConsoleWin(uint ctrlEvent, uint processGroupEventId);

    [LibraryImport("Kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsoleWin();

    [LibraryImport("Kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsoleWin(uint processId);

    [LibraryImport("Kernel32.dll", EntryPoint = "FreeConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    // ReSharper disable once UnusedMethodReturnValue.Local
    private static partial bool FreeConsoleWin();

    [LibraryImport("Kernel32.dll", SetLastError = true)]
    private static partial IntPtr GetConsoleWindow();
#endif
}
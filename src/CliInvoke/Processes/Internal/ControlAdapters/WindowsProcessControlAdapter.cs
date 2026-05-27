/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;

using CliInvoke.Processes.Internal.Cancellation;

namespace CliInvoke.Processes.Internal.ControlAdapters;

internal partial class WindowsProcessControlAdapter : BaseProcessControlAdapter
{
    private const uint CtrlCSignalEvent = 0;
    
    internal override void ResumeProcess(Process process)
    {
        uint actualPid = GetProcessId(process.Handle);

        Process proc;
        try
        {
            proc = Process.GetProcessById((int)actualPid);
        }
        catch (ArgumentException)
        {
            // Process was not found - probably exited already.
            return;
        }

        ProcessThreadCollection threads;
        try
        {
            threads = proc.Threads;
        }
        catch (InvalidOperationException)
        {
            // Process has exited; nothing to resume.
            return;
        }

        foreach (ProcessThread pt in threads)
        {
            IntPtr threadHandle =
                OpenThread(ThreadAccess.THREAD_SUSPEND_RESUME, false, (uint)pt.Id);
            if (threadHandle == IntPtr.Zero) continue;

            uint result = ResumeThread(threadHandle);
            if (result == uint.MaxValue)
            {
                int err = Marshal.GetLastWin32Error();
                CloseHandle(threadHandle);
                // ERROR_ACCESS_DENIED (5) can occur when resuming protected threads; ignore it.
                const int ERROR_ACCESS_DENIED = 5;
                if (err == ERROR_ACCESS_DENIED)
                    continue;

                // If the process has exited since we started enumerating threads, treat that as benign and continue.
                if (proc.HasExited)
                    continue;

                throw new InvalidOperationException(
                    $"ResumeThread failed for thread {pt.Id} with error {err}.");
            }
            CloseHandle(threadHandle);
        }
    }

    internal override void SuspendProcess(Process process)
    {
        uint actualPid = GetProcessId(process.Handle);

        Process proc;
        try
        {
            proc = Process.GetProcessById((int)actualPid);
        }
        catch (ArgumentException)
        {
            // Process was not found - probably exited already.
            return;
        }

        ProcessThreadCollection threads;
        try
        {
            threads = proc.Threads;
        }
        catch (InvalidOperationException)
        {
            // Process has exited; nothing to suspend.
            return;
        }

        foreach (ProcessThread pt in threads)
        {
            IntPtr threadHandle = OpenThread(ThreadAccess.THREAD_SUSPEND_RESUME, false, (uint)pt.Id);
            if (threadHandle == IntPtr.Zero) continue;

            uint result = SuspendThread(threadHandle);
            if (result == uint.MaxValue)
            {
                int err = Marshal.GetLastWin32Error();
                CloseHandle(threadHandle);
                // ERROR_ACCESS_DENIED (5) can occur for certain systems or protected threads.
                // Treat access denied as non-fatal and continue with other threads.
                const int ERROR_ACCESS_DENIED = 5;
                if (err == ERROR_ACCESS_DENIED)
                    continue;

                // If the process has exited since we started enumerating threads, treat that as benign and continue.
                if (proc.HasExited)
                    continue;

                throw new InvalidOperationException($"SuspendThread failed for thread {pt.Id} with error {err}.");
            }

            CloseHandle(threadHandle);
        }
    }

    internal override void SetResourcePolicy(ProcessWrapper process, ProcessResourcePolicy?  resourcePolicy)
    {
        resourcePolicy ??= ProcessResourcePolicy.Default;

        if (!process.HasStarted)
            throw new InvalidOperationException(
                Resources.Exceptions_ResourcePolicy_CannotSetToNonStartedProcess
            );

        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            if (resourcePolicy.ProcessorAffinity is not null)
                process.ProcessorAffinity = (IntPtr)resourcePolicy.ProcessorAffinity;

        if (OperatingSystem.IsMacOS()
            || OperatingSystem.IsMacCatalyst()
            || OperatingSystem.IsFreeBSD()
            || OperatingSystem.IsWindows()
           )
        {
            if (resourcePolicy.MinWorkingSet is not null)
                process.MinWorkingSet = (nint)resourcePolicy.MinWorkingSet;

            if (resourcePolicy.MaxWorkingSet is not null)
                process.MaxWorkingSet = (nint)resourcePolicy.MaxWorkingSet;
        }

        process.PriorityClass = resourcePolicy.PriorityClass;
        process.PriorityBoostEnabled = resourcePolicy.EnablePriorityBoost;
    }

    [SupportedOSPlatform("windows")]
    internal override void RequireRunningAsAdmin(Process process)
    {
        if(OperatingSystem.IsWindows())
            process.StartInfo.Verb = "runas";
    }

    [SupportedOSPlatform("windows")]
    internal override void SetUserCredential(Process process, UserCredential credential)
    {
        if (credential.Domain is not null && OperatingSystem.IsWindows())
            process.StartInfo.Domain = credential.Domain;

        if (credential.UserName is not null)  
            process.StartInfo.UserName = credential.UserName;

        if (credential.Password is not null && OperatingSystem.IsWindows())
            process.StartInfo.Password = credential.Password;

        if (credential.LoadUserProfile is not null && OperatingSystem.IsWindows())
            process.StartInfo.LoadUserProfile = (bool)credential.LoadUserProfile;
    }

    internal override async Task<bool> SendInterruptSignalAsync(Process process,
        CancellationReason cancellationReason,
        ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
    {
        // Allocate a Console to the Process so that it has one it can use.
        bool successfulAttachment = AllocConsoleWin();
        // Attach the allocated console to the process.
        successfulAttachment =
            successfulAttachment && AttachConsoleWin((uint)process.Id);

        return successfulAttachment && SendCtrlCToConsoleWin(CtrlCSignalEvent, 0);
    }

    #region P/Invoke code for Graceful Cancellation of Processes on Windows
    [LibraryImport("Kernel32.dll", EntryPoint = "GenerateConsoleCtrlEvent", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SendCtrlCToConsoleWin(uint ctrlEvent, uint processGroupEventId);

    [LibraryImport("Kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsoleWin();

    [LibraryImport("Kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsoleWin(uint processId);
    
    
    // Windows: list threads and call SuspendThread/ResumeThread on each thread of the process.
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenThread(ThreadAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint SuspendThread(IntPtr hThread);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint ResumeThread(IntPtr hThread);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    // ReSharper disable once UnusedMethodReturnValue.Local
    private static partial bool CloseHandle(IntPtr hObject);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint GetProcessId(IntPtr hProcess);

    [Flags]
    private enum ThreadAccess : uint
    {
        THREAD_SUSPEND_RESUME = 0x0002,
        THREAD_GET_CONTEXT = 0x0008,
        THREAD_SET_CONTEXT = 0x0010,
        THREAD_QUERY_INFORMATION = 0x0040,
        THREAD_SET_INFORMATION = 0x0020,
        THREAD_TERMINATE = 0x0001,
        THREAD_ALL_ACCESS = 0x001F03FF
    }
    #endregion
}
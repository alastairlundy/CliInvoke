/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;

using CliInvoke.Processes.Internal.Cancellation;
// ReSharper disable UnusedMember.Local

namespace CliInvoke.Processes.Internal.ControlAdapters;

internal partial class WindowsProcessControlAdapter : BaseProcessControlAdapter
{
    private const uint CtrlCSignalEvent = 0;
    private const uint CtrlBreakSignalEvent = 1;
    
    internal override void ResumeProcess(Process process)
    {
        // By-ID lookups refuse a child whose exit code is recorded, so use a Toolhelp thread
        // snapshot keyed off the held handle instead; that handle stays valid until termination.
        uint actualPid = GetProcessId(process.Handle);
        if (actualPid == 0)
        {
            // No associated process: nothing to resume.
            return;
        }

        IntPtr snapshot = CreateToolhelp32Snapshot(Th32csSnapThread, 0);
        if (snapshot == InvalidSnapshotHandle)
        {
            throw new InvalidOperationException(
                $"CreateToolhelp32Snapshot failed with error {Marshal.GetLastWin32Error()}.");
        }

        try
        {
            ThreadEntry32 entry = new()
            {
                dwSize = (uint)Marshal.SizeOf<ThreadEntry32>()
            };

            if (!Thread32First(snapshot, ref entry))
            {
                // Enumeration failure; OnStarted decides if it is benign.
                throw new InvalidOperationException(
                    $"Thread32First failed with error {Marshal.GetLastWin32Error()}.");
            }

            do
            {
                if (entry.th32OwnerProcessID != actualPid)
                    continue;

                ResumeThreadById(entry.th32ThreadID);
            } while (Thread32Next(snapshot, ref entry));
        }
        finally
        {
            CloseHandle(snapshot);
        }
    }

    /// <summary>Releases one suspend count on a snapshot thread, skipping gone/denied ones.</summary>
    private static void ResumeThreadById(uint threadId)
    {
        IntPtr threadHandle = OpenThread(ThreadAccess.THREAD_SUSPEND_RESUME, false, threadId);
        if (threadHandle == IntPtr.Zero) return;

        uint result = ResumeThread(threadHandle);
        if (result == uint.MaxValue)
        {
            // Read the error before CloseHandle overwrites the last-error slot.
            int err = Marshal.GetLastWin32Error();
            CloseHandle(threadHandle);
            const int ERROR_ACCESS_DENIED = 5;
            if (err == ERROR_ACCESS_DENIED)
                return;

            // HasExited is also true in the pre-signal window, so let the caller decide.
            throw new InvalidOperationException(
                $"ResumeThread failed for thread {threadId} with error {err}.");
        }

        CloseHandle(threadHandle);
    }

    internal override void SuspendProcess(Process process)
    {
        // A refused by-ID lookup here means nothing was frozen.
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

    /// <summary>Never-throwing HasExited read; a disposed handle counts as gone.</summary>
    private static bool ChildHasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (Exception)
        {
            // Disposed or invalid process handle: the process is gone.
            return true;
        }
    }

    internal override void SetResourcePolicy(ProcessWrapper process, ProcessResourcePolicy?  resourcePolicy)
    {
        resourcePolicy ??= ProcessResourcePolicy.Default;

        if (!process.HasStarted)
            throw new InvalidOperationException(
                Resources.Exceptions_ResourcePolicy_CannotSetToNonStartedProcess
            );

        // A child may exit between OnStarted's check and this call; skip its policy.
        if (process.HasExited)
            return;

        // Skip remaining setters once the child reports exited, keeping this method self-contained.
        void ApplySettingIfRunning(Action apply)
        {
            if (!ChildHasExited(process))
                ApplyPolicySetting(process, apply);
        }

        try
        {
            if (OperatingSystem.IsWindows())
                if (resourcePolicy.ProcessorAffinity is not null)
                    ApplySettingIfRunning(
                        () => process.ProcessorAffinity = (IntPtr)resourcePolicy.ProcessorAffinity);

            if (OperatingSystem.IsMacOS()
                || OperatingSystem.IsMacCatalyst()
                || OperatingSystem.IsFreeBSD()
                || OperatingSystem.IsWindows()
               )
            {
                if (resourcePolicy.MinWorkingSet is not null)
                    ApplySettingIfRunning(
                        () => process.MinWorkingSet = (nint)resourcePolicy.MinWorkingSet);

                if (resourcePolicy.MaxWorkingSet is not null)
                    ApplySettingIfRunning(
                        () => process.MaxWorkingSet = (nint)resourcePolicy.MaxWorkingSet);
            }

            ApplySettingIfRunning(() => process.PriorityClass = resourcePolicy.PriorityClass);
            ApplySettingIfRunning(
                () => process.PriorityBoostEnabled = resourcePolicy.EnablePriorityBoost);
        }
        catch (InvalidOperationException) when (ChildHasExited(process))
        {
            // Child exited after the last guard but before a setter's own check.
        }
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
        // A null credential is a no-op, matching UnixProcessControlAdapter semantics.
        if (credential is null)
            return;

        if (credential.Domain is not null && OperatingSystem.IsWindows())
            process.StartInfo.Domain = credential.Domain;

        if (credential.UserName is not null)  
            process.StartInfo.UserName = credential.UserName;

        if (credential.Password is not null && OperatingSystem.IsWindows())
            process.StartInfo.Password = credential.Password;

        if (credential.LoadUserProfile is not null && OperatingSystem.IsWindows())
            process.StartInfo.LoadUserProfile = (bool)credential.LoadUserProfile;
    }

    internal override PosixSignal? GetTerminatingSignal(int exitCode) => null;

    internal override Task<bool> SendInterruptSignalAsync(Process process,
        CancellationReason cancellationReason,
        ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
    {
        // Try to share the child process's console so we can send it control events.
        // If the child has no console (started with CreateNoWindow=true), this will fail
        // with ERROR_INVALID_HANDLE — Fix #2: give up cleanly instead of sending no-op signals.
        bool attached = AttachConsoleWin((uint)process.Id);

        bool signalSent = false;
        if (attached)
        {
            // Fix #3: CTRL_BREAK_EVENT (1) CAN target a specific process group, unlike
            // CTRL_C_EVENT which is a documented no-op with a non-zero group id.
            // Fix #4: use GetProcessGroupId which returns the PID — the correct group id
            // for processes started without CREATE_NEW_PROCESS_GROUP.
            signalSent = SendCtrlCToConsoleWin(CtrlBreakSignalEvent, GetProcessGroupId(process));

            if (!signalSent)
            {
                // Fall back to broadcasting CTRL_C_EVENT to all processes sharing this console.
                signalSent = SendCtrlCToConsoleWin(CtrlCSignalEvent, 0);
            }

            // Detach from the child's console to avoid interfering.
            FreeConsoleWin();
        }

        return Task.FromResult(signalSent);
    }

    /// <summary>
    ///     Returns the process group ID for use with GenerateConsoleCtrlEvent.
    ///     For processes started without <c>CREATE_NEW_PROCESS_GROUP</c>, the process ID is the
    ///     correct group identifier. Fix #4: returns the PID directly instead of querying the
    ///     EPROCESS-level group id which GenerateConsoleCtrlEvent does not interpret.
    /// </summary>
    private static uint GetProcessGroupId(Process process) => (uint)process.Id;

    #region P/Invoke code for Graceful Cancellation of Processes on Windows
    [LibraryImport("Kernel32.dll", EntryPoint = "GenerateConsoleCtrlEvent", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SendCtrlCToConsoleWin(uint ctrlEvent, uint processGroupEventId);

    [LibraryImport("Kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsoleWin(uint processId);

    [LibraryImport("Kernel32.dll", EntryPoint = "FreeConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    // ReSharper disable once UnusedMethodReturnValue.Local
    private static partial bool FreeConsoleWin();
    
    
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

    // TH32CS_SNAPTHREAD: list the child's threads without re-opening it by ID.
    private const uint Th32csSnapThread = 0x00000004;

    private static readonly IntPtr InvalidSnapshotHandle = new(-1); // INVALID_HANDLE_VALUE

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Thread32First(IntPtr hSnapshot, ref ThreadEntry32 lpte);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Thread32Next(IntPtr hSnapshot, ref ThreadEntry32 lpte);

    // All seven native fields declared; the 28-byte layout must match THREADENTRY32.
    [StructLayout(LayoutKind.Sequential)]
    private struct ThreadEntry32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ThreadID;
        public uint th32OwnerProcessID;
        public int tpBasePri;
        public int tpDeltaPri;
        public uint dwFlags;
    }

    // Deliberately not marked [Flags]: the values mirror the Win32 THREAD_* access masks,
    // but this enum is only ever passed to OpenThread as a single right (never combined),
    // and THREAD_ALL_ACCESS (0x001F03FF) is not a clean combination of the other members.
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
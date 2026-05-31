/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;
using System.Runtime.InteropServices;

using CliInvoke.Helpers.Processes;
// ReSharper disable UnusedMember.Local

namespace CliInvoke.Helpers;

internal 
#if NET8_0_OR_GREATER
    partial
#endif
    class ProcessWrapper : Process
{
    // Synchronisation primitives for cancellation operations
    internal readonly SemaphoreSlim CancellationSemaphore = new(1, 1);
    
    internal readonly SemaphoreSlim ForcefulExitLock = new(1, 1);
    
    // Track if forceful exit has been attempted to prevent double invocation
    internal bool ForcefulExitAttempted = false;
    
    internal ProcessWrapper(ProcessConfiguration configuration,
        ProcessResourcePolicy? resourcePolicy)
    {
        StartInfo = configuration.ToProcessStartInfo(configuration.RedirectStandardOutput,
            configuration.RedirectStandardError);
        ProcessName = StartInfo.FileName;
        EnableRaisingEvents = true;
        Exited += OnExited;
        Started += OnStarted;

        HasStarted = false;
        ResourcePolicy = resourcePolicy ?? ProcessResourcePolicy.Default;
    }

    internal ProcessResourcePolicy ResourcePolicy { get; set; }

    internal bool HasStarted { get; private set; }

    internal new DateTime StartTime { get; private set; }

    internal new DateTime ExitTime { get; private set; }

    internal new int Id { get; private set; }

    internal new string ProcessName { get; private set; }

    private void OnStarted(object? sender, EventArgs e)
    {
        try
        {
            SuspendProcess();

        #pragma warning disable CA1416
            this.SetResourcePolicy(ResourcePolicy);
        #pragma warning restore CA1416

            ResumeProcess();
        }
        catch (InvalidOperationException)
        {
            // Process may have exited between starting and applying policy
        }
    }

    private void OnExited(object? sender, EventArgs e)
    {
        ExitTime = base.ExitTime;
    }

    internal event EventHandler Started;

    public new bool Start()
    {
        try
        {
            HasStarted = base.Start();
        }
        catch(Win32Exception exception)
        {
            HasStarted = false;

            throw new UnauthorizedAccessException($"The current user does not have permission to execute the file '{StartInfo.FileName}'.", exception);
        }

        if (!HasStarted)
        {
            throw new InvalidOperationException($"Process with Target File Name of '{StartInfo.FileName}' could not be started.");
        }

        if (!HasStarted) return HasStarted;
        
        StartTime = DateTime.UtcNow;
        Started.Invoke(this, EventArgs.Empty);
        Id = base.Id;
        ProcessName = base.ProcessName;

        return HasStarted;
    }

    /// <summary>
    /// Suspend this process. Routes to the platform-specific implementation.
    /// </summary>
    internal void SuspendProcess()
    {
        if (HasExited)
            throw new InvalidOperationException("Cannot suspend a process that has already exited.");

        if (OperatingSystem.IsWindows())
        {
            SuspendProcessWindows(Handle);
        }
        else
        {
            SuspendProcessUnix(base.Id);
        }
    }

    /// <summary>
    /// Resume this process. Routes to the platform-specific implementation.
    /// </summary>
    internal void ResumeProcess()
    {
        if (HasExited)
            throw new InvalidOperationException("Cannot resume a process that has already exited.");

        if (OperatingSystem.IsWindows())
        {
            ResumeProcessWindows(Handle);
        }
        else
        {
            ResumeProcessUnix(base.Id);
        }
    }

    // Windows: list threads and call SuspendThread/ResumeThread on each thread of the process.
#if NET8_0_OR_GREATER
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenThread(ThreadAccess dwDesiredAccess, 
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);
#else
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, 
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);
#endif


#if NET8_0_OR_GREATER
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint SuspendThread(IntPtr hThread);
#else
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SuspendThread(IntPtr hThread);
#endif

#if NET8_0_OR_GREATER
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint ResumeThread(IntPtr hThread);
#else
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr hThread);
#endif

#if NET8_0_OR_GREATER
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    // ReSharper disable once UnusedMethodReturnValue.Local
    private static partial bool CloseHandle(IntPtr hObject);
#else
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
#endif
    // ReSharper disable once UnusedMethodReturnValue.Local

#if NET8_0_OR_GREATER
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint GetProcessId(IntPtr hProcess);
#else
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetProcessId(IntPtr hProcess);
#endif

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

    private static void SuspendProcessWindows(IntPtr processHandle)
    {
        uint pid = GetProcessId(processHandle);

        Process proc;
        try
        {
            proc = GetProcessById((int)pid);
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

    private static void ResumeProcessWindows(IntPtr processHandle)
    {
        uint pid = GetProcessId(processHandle);

        Process proc;
        try
        {
            proc = GetProcessById((int)pid);
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
            IntPtr threadHandle = OpenThread(ThreadAccess.THREAD_SUSPEND_RESUME, false, (uint)pt.Id);
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

                throw new InvalidOperationException($"ResumeThread failed for thread {pt.Id} with error {err}.");
            }

            CloseHandle(threadHandle);
        }
    }

    // Unix: use kill(pid, SIGSTOP) and kill(pid, SIGCONT).
#if NET8_0_OR_GREATER
    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int kill(int pid, int sig);
#else
    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int kill(int pid, int sig);
#endif    

    private static void SuspendProcessUnix(int pid)
    {
        int SIGSTOP = OperatingSystem.IsMacOS() ? 17 : 19; // macOS fallback vs Linux
        if (kill(pid, SIGSTOP) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            // No such process - indicates the process already exited; treat as no-op.
            if (errno == 3) return;
            throw new InvalidOperationException($"kill(SIGSTOP) failed for pid {pid} with errno {errno}.");
        }
    }

    private static void ResumeProcessUnix(int pid)
    {
        const int SIGCONT = 18; // common value for SIGCONT
        if (kill(pid, SIGCONT) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            // No such process - indicates the process already exited; treat as no-op.
            if (errno == 3) return;
            throw new InvalidOperationException($"kill(SIGCONT) failed for pid {pid} with errno {errno}.");
        }
    }
}

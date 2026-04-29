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

namespace CliInvoke.Helpers;

internal partial class ProcessWrapper : Process
{
    // Synchronisation primitive to prevent simultaneous cancellation attempts
    internal readonly SemaphoreSlim _cancellationSemaphore = new(1, 1);

    internal ProcessWrapper(ProcessConfiguration configuration,
        ProcessResourcePolicy? resourcePolicy)
    {
        StartInfo = configuration.ToProcessStartInfo();
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
        SuspendProcess();
        
#pragma warning disable CA1416
        this.SetResourcePolicy(ResourcePolicy);
#pragma warning restore CA1416
        
        ResumeProcess();
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
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenThread(ThreadAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint SuspendThread(IntPtr hThread);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint ResumeThread(IntPtr hThread);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
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

    private static void SuspendProcessWindows(IntPtr processHandle)
    {
        uint pid = GetProcessId(processHandle);
        Process proc = Process.GetProcessById((int)pid);
        foreach (ProcessThread pt in proc.Threads)
        {
            IntPtr threadHandle = OpenThread(ThreadAccess.THREAD_SUSPEND_RESUME, false, (uint)pt.Id);
            if (threadHandle == IntPtr.Zero) continue;

            uint result = SuspendThread(threadHandle);
            if (result == uint.MaxValue)
            {
                int err = Marshal.GetLastWin32Error();
                CloseHandle(threadHandle);
                throw new InvalidOperationException($"SuspendThread failed for thread {pt.Id} with error {err}.");
            }

            CloseHandle(threadHandle);
        }
    }

    private static void ResumeProcessWindows(IntPtr processHandle)
    {
        uint pid = GetProcessId(processHandle);
        Process proc = Process.GetProcessById((int)pid);
        foreach (ProcessThread pt in proc.Threads)
        {
            IntPtr threadHandle = OpenThread(ThreadAccess.THREAD_SUSPEND_RESUME, false, (uint)pt.Id);
            if (threadHandle == IntPtr.Zero) continue;

            uint result = ResumeThread(threadHandle);
            if (result == uint.MaxValue)
            {
                int err = Marshal.GetLastWin32Error();
                CloseHandle(threadHandle);
                throw new InvalidOperationException($"ResumeThread failed for thread {pt.Id} with error {err}.");
            }

            CloseHandle(threadHandle);
        }
    }

    // Unix: use kill(pid, SIGSTOP) and kill(pid, SIGCONT).
    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int kill(int pid, int sig);

    private static void SuspendProcessUnix(int pid)
    {
        int SIGSTOP = OperatingSystem.IsMacOS() ? 17 : 19; // macOS fallback vs Linux
        if (kill(pid, SIGSTOP) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"kill(SIGSTOP) failed for pid {pid} with errno {errno}.");
        }
    }

    private static void ResumeProcessUnix(int pid)
    {
        const int SIGCONT = 18; // common value for SIGCONT
        if (kill(pid, SIGCONT) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"kill(SIGCONT) failed for pid {pid} with errno {errno}.");
        }
    }
}

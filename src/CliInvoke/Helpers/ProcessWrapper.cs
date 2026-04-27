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
        if (!HasExited && HasStarted)
            try
            {
#pragma warning disable CA1416
                this.SetResourcePolicy(ResourcePolicy);
#pragma warning restore CA1416
            }
            catch
            {
                // ignored
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
            SuspendProcessWindows(base.Handle);
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
            ResumeProcessWindows(base.Handle);
        }
        else
        {
            ResumeProcessUnix(base.Id);
        }
    }

    // Windows: use undocumented NtSuspendProcess/NtResumeProcess in ntdll.dll.
    // This is pragmatic and works on many Windows versions, but it is undocumented.
    // An alternative would be to enumerate threads and call SuspendThread/ResumeThread.
    [LibraryImport("ntdll.dll")]
    private static partial int NtSuspendProcess(IntPtr processHandle);

    [LibraryImport("ntdll.dll")]
    private static partial int NtResumeProcess(IntPtr processHandle);

    private static void SuspendProcessWindows(IntPtr processHandle)
    {
        // NtSuspendProcess returns an NTSTATUS. Zero indicates success.
        int status = NtSuspendProcess(processHandle);
        if (status != 0)
        {
            throw new InvalidOperationException($"NtSuspendProcess failed with NTSTATUS 0x{status:X8}.");
        }
    }

    private static void ResumeProcessWindows(IntPtr processHandle)
    {
        int status = NtResumeProcess(processHandle);
        if (status != 0)
        {
            throw new InvalidOperationException($"NtResumeProcess failed with NTSTATUS 0x{status:X8}.");
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
        int SIGCONT = 18; // common value for SIGCONT
        if (kill(pid, SIGCONT) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"kill(SIGCONT) failed for pid {pid} with errno {errno}.");
        }
    }
}

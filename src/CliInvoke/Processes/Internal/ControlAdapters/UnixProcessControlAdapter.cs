/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;

using CliInvoke.Helpers;

namespace CliInvoke.Processes.Internal.ControlAdapters;

internal partial class UnixProcessControlAdapter : BaseProcessControlAdapter
{
    private const int Sigint = 2;
    private const int Sigterm = 15;

    private const int DelayBeforeSigintMilliseconds = 3000;
    
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    internal override void ResumeProcess(Process process)
    {
        const int SIGCONT = 18; // common value for SIGCONT
        if (kill(process.Id, SIGCONT) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            // No such process - indicates the process already exited; treat as no-op.
            if (errno == 3) return;
            throw new InvalidOperationException($"kill(SIGCONT) failed for pid {process} with errno {errno}.");
        }
    }

    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    internal override void SuspendProcess(Process process)
    {
        int SIGSTOP = OperatingSystem.IsMacOS() ? 17 : 19; // macOS fallback vs Linux
        if (kill(process.Id, SIGSTOP) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            // No such process - indicates the process already exited; treat as no-op.
            if (errno == 3) return;
            throw new InvalidOperationException($"kill(SIGSTOP) failed for pid {process} with errno {errno}.");
        }
    }

    internal override void SetResourcePolicy(ProcessWrapper process, ProcessResourcePolicy? resourcePolicy)
    {
        resourcePolicy ??= ProcessResourcePolicy.Default;

        if (!process.HasStarted)
            throw new InvalidOperationException(
                Resources.Exceptions_ResourcePolicy_CannotSetToNonStartedProcess
            );

        if (OperatingSystem.IsLinux())
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

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    internal override void RequireRunningAsAdmin(Process process)
    {
        if (OperatingSystem.IsLinux() ||
            OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ||
            OperatingSystem.IsFreeBSD())
            process.StartInfo.Verb = "sudo";
    }

    internal override void SetUserCredential(Process process, UserCredential credential)
    {
    }

    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    internal override async Task<bool> SendInterruptSignalAsync(Process process,
        CancellationReason cancellationReason,
        ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsIOS() ||
            OperatingSystem.IsTvOS())
            throw new PlatformNotSupportedException();
            
        bool sigTermSuccess = SendUnixSignal(process.Id, Sigterm);

        await Task.Delay(DelayBeforeSigintMilliseconds,
            cancellationToken);

        return sigTermSuccess || SendUnixSignal(process.Id, Sigint);
    }

    /// <summary>
    /// </summary>
    /// <param name="processId"></param>
    /// <param name="signalId"></param>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("browser")]
    private static bool SendUnixSignal(int processId, int signalId) => 
        kill(processId, signalId) == 0;

    // Unix: use kill(pid, SIGSTOP) and kill(pid, SIGCONT).
    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int kill(int pid, int sig);
}
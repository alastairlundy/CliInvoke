/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;
using System.Runtime.InteropServices;

using CliInvoke.Processes.Internal.Cancellation;

namespace CliInvoke.Processes.Internal.ControlAdapters;

internal partial class UnixProcessControlAdapter : BaseProcessControlAdapter
{
#if !NET11_0
    // Raw signal numbers for the kill() P/Invoke path. On the net11.0 leg these are
    // replaced by PosixSignal members passed to Process.Signal (except SIGSTOP, which
    // the enum does not model and which keeps the kill() path on both legs).
    private const int Sigint = 2;
    private const int Sigterm = 15;
#endif

    private const int DelayBeforeSigintMilliseconds = 3000;
    
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    internal override void ResumeProcess(Process process)
    {
#if NET11_0
        // net11.0 leg: Process.Signal delivers SIGCONT through the runtime's own kill()
        // plumbing, which resolves the platform-correct signal number internally —
        // the Linux-vs-BSD numbering handled manually on the net10.0 path below.
        try
        {
            // false means the process is already gone (exited or ESRCH): the same no-op
            // the kill() path treated errno==3 as.
            _ = process.Signal(PosixSignal.SIGCONT);
        }
        catch (Win32Exception exception)
        {
            // Preserve the kill() path's failure contract: non-ESRCH delivery failures
            // surface as InvalidOperationException carrying the errno.
            throw new InvalidOperationException(
                $"Signal(SIGCONT) failed for pid {process} with errno {exception.NativeErrorCode}.",
                exception);
        }
#else
        // SIGCONT is 18 on Linux but 19 on macOS/FreeBSD (BSD numbering); using the Linux
        // value there raises SIGTSTP instead, stopping the process it was meant to resume.
        int sigcont = GetContinueSignalNumber();
        if (kill(process.Id, sigcont) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            // No such process - indicates the process already exited; treat as no-op.
            if (errno == 3) return;
            throw new InvalidOperationException($"kill(SIGCONT) failed for pid {process} with errno {errno}.");
        }
#endif
    }

    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    internal override void SuspendProcess(Process process)
    {
        // SIGSTOP is 19 on Linux but 17 on macOS/FreeBSD (BSD numbering); using the Linux
        // value there raises SIGCONT instead, a no-op for a process meant to be stopped.
        // This site keeps the kill() P/Invoke on BOTH legs: the BCL PosixSignal enum has
        // no SIGSTOP member (only the catchable SIGTSTP), so Process.Signal cannot
        // express a stop and a swap here would change semantics.
        int sigstop = GetStopSignalNumber();
        if (kill(process.Id, sigstop) != 0)
        {
            int errno = Marshal.GetLastWin32Error();
            // No such process - indicates the process already exited; treat as no-op.
            if (errno == 3) return;
            throw new InvalidOperationException($"kill(SIGSTOP) failed for pid {process} with errno {errno}.");
        }
    }

    /// <summary>
    ///     Whether the current platform numbers its POSIX signals using the BSD layout
    ///     (SIGSTOP 17, SIGTSTP 18, SIGCONT 19, SIGCHLD 20) rather than the Linux layout
    ///     (SIGCHLD 17, SIGSTOP 19, SIGCONT 18, SIGTSTP 20).
    /// </summary>
    internal static bool UsesBsdSignalNumbers() =>
        OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsFreeBSD();

    /// <summary>
    ///     The platform-correct signal number for raising SIGSTOP.
    /// </summary>
    internal static int GetStopSignalNumber() => UsesBsdSignalNumbers() ? 17 : 19;

    /// <summary>
    ///     The platform-correct signal number for raising SIGCONT.
    /// </summary>
    internal static int GetContinueSignalNumber() => UsesBsdSignalNumbers() ? 19 : 18;

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
        throw new PlatformNotSupportedException(
            "Running as admin is not supported on Unix-like systems. Use sudo or run as root directly.");
    }

    internal override void SetUserCredential(Process process, UserCredential credential)
    {
        if (credential is null)
            return;

        // An "empty" credential (all fields null) is a no-op, matching the
        // WindowsProcessControlAdapter field-by-field semantics; only a populated
        // credential is unsupported on Unix-like systems. ProcessConfiguration.Credential
        // defaults to the non-null UserCredential.Null sentinel, so a null check alone
        // would reject every default-configuration process start on Unix.
#pragma warning disable CA1416
        bool hasCredential = credential.UserName is not null
            || credential.Domain is not null
            || credential.Password is not null
            || credential.LoadUserProfile is not null;
#pragma warning restore CA1416

        if (hasCredential)
            throw new PlatformNotSupportedException(
                "Setting user credentials is not supported on Unix-like systems.");
    }

    internal override PosixSignal? GetTerminatingSignal(int exitCode)
    {
        if (exitCode <= 128)
            return null;

        // The BCL PosixSignal enum does not use OS signal numbers (it uses negative sentinels,
        // e.g. SIGTERM = -4, SIGINT = -2), so a raw cast of (exitCode - 128) would yield a
        // value that does not equal the corresponding enum member. Map the raw signal number
        // to the correct PosixSignal explicitly instead.
        int signalNumber = exitCode - 128;
        return _signalByNumber.TryGetValue(signalNumber, out PosixSignal signal) ? signal : null;
    }

    private static readonly Dictionary<int, PosixSignal> _signalByNumber = CreateSignalByNumberTable();

    private static Dictionary<int, PosixSignal> CreateSignalByNumberTable()
    {
        Dictionary<int, PosixSignal> table = new()
        {
            // Signals with the same numbers on both Linux and the BSD family.
            [1] = PosixSignal.SIGHUP,
            [2] = PosixSignal.SIGINT,
            [3] = PosixSignal.SIGQUIT,
            [15] = PosixSignal.SIGTERM,
            [21] = PosixSignal.SIGTTIN,
            [22] = PosixSignal.SIGTTOU,
            [28] = PosixSignal.SIGWINCH,
        };

        if (UsesBsdSignalNumbers())
        {
            // BSD numbering (macOS, FreeBSD): SIGSTOP 17 (no PosixSignal member, omitted),
            // SIGTSTP 18, SIGCONT 19, SIGCHLD 20.
            table[18] = PosixSignal.SIGTSTP;
            table[19] = PosixSignal.SIGCONT;
            table[20] = PosixSignal.SIGCHLD;
        }
        else
        {
            // Linux numbering: SIGCHLD 17, SIGCONT 18, SIGTSTP 20.
            table[17] = PosixSignal.SIGCHLD;
            table[18] = PosixSignal.SIGCONT;
            table[20] = PosixSignal.SIGTSTP;
        }

        // SIGKILL (9) maps to null: a process cannot catch or trap SIGKILL, and callers
        // distinguish library-initiated forceful kills via ProcessResult.Canceled
        // rather than Signal.
        return table;
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
            
#if NET11_0
        bool sigTermSuccess = SendUnixSignal(process, PosixSignal.SIGTERM);

        await Task.Delay(DelayBeforeSigintMilliseconds,
            cancellationToken).ConfigureAwait(false);

        return sigTermSuccess || SendUnixSignal(process, PosixSignal.SIGINT);
#else
        bool sigTermSuccess = SendUnixSignal(process.Id, Sigterm);

        await Task.Delay(DelayBeforeSigintMilliseconds,
            cancellationToken).ConfigureAwait(false);

        return sigTermSuccess || SendUnixSignal(process.Id, Sigint);
#endif
    }

#if NET11_0
    /// <summary>
    ///     Delivers <paramref name="signal"/> to <paramref name="process"/> via
    ///     <see cref="Process.Signal(PosixSignal)"/> (net11.0 leg). Returns <c>false</c> when
    ///     the signal could not be delivered — including when the process has already exited —
    ///     matching the net10.0 <c>kill() == 0</c> contract of the shared-source path.
    /// </summary>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("browser")]
    private static bool SendUnixSignal(Process process, PosixSignal signal)
    {
        try
        {
            return process.Signal(signal);
        }
        catch (Win32Exception)
        {
            // Process.Signal throws Win32Exception for non-ESRCH delivery failures where the
            // kill() path returned a non-zero errno; fold those back to false so callers see
            // the same bool-only behaviour on both legs.
            return false;
        }
    }
#else
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
#endif

    // Unix: use kill(pid, SIGSTOP) and kill(pid, SIGCONT).
    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int kill(int pid, int sig);
}
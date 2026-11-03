/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core;

/// <summary>
///     A point-in-time snapshot of a child process's state, captured when a
///     timeout or cancellation occurs. Used to enrich
///     <see cref="Exceptions.CliInvokeTimeoutException"/> and
///     <see cref="Exceptions.CliInvokeCanceledException"/> so callers (and CI
///     logs) can distinguish a cold-start stall, a suspended-start race, and a
///     genuine deadlock.
/// </summary>
/// <param name="ProcessId">The OS-assigned identifier of the child process.</param>
/// <param name="HasExited">Whether the process had already exited when the snapshot was taken.</param>
/// <param name="ExitCode">
///     The process exit code when <paramref name="HasExited"/> is <c>true</c> and the code is
///     available; otherwise <c>null</c>.
/// </param>
/// <param name="IsSuspended">
///     <c>true</c> when every thread of the process is in a suspended wait state (Windows:
///     <see cref="System.Diagnostics.ThreadState.Wait"/> with
///     <see cref="System.Diagnostics.ThreadWaitReason.Suspended"/>). On platforms where
///     thread-state reporting is unreliable this defaults to <c>false</c>.
/// </param>
/// <param name="AwaitingInitialResume">
///     <c>true</c> when the net11.0 creation-suspended release has not yet been delivered
///     (i.e. <c>StartSuspended</c> was set but <c>SafeProcessHandle.Resume()</c> has not run).
///     Always <c>false</c> on net10.0, where <c>StartSuspended</c> is unavailable.
/// </param>
/// <param name="Elapsed">How long the process had been running when the snapshot was taken.</param>
/// <param name="CapturedAtUtc">The UTC time at which the snapshot was taken.</param>
public sealed record ProcessDiagnostic(
    int ProcessId,
    bool HasExited,
    int? ExitCode,
    bool IsSuspended,
    bool AwaitingInitialResume,
    TimeSpan Elapsed,
    DateTime CapturedAtUtc);
/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using TUnit.Core.Exceptions;

namespace CliInvoke.Tests.Invokers;

/// <summary>
///     Real-executable end-to-end proof of the H2 raw-mode pipe-deadlock fix: a
///     <see cref="CliRun.RunAsync(CliInvoke.Core.ProcessConfiguration, CliInvoke.Core.ProcessExitConfiguration?, System.Threading.CancellationToken)"/>
///     (Raw mode) invocation whose configuration requests output redirection completes
///     even when the child writes far more than the operating-system pipe buffer,
///     because the pipeline forces redirection off for Raw mode. Before the fix the
///     child blocked on the undrained pipe and the run never completed. The
///     deterministic normalization contract (factory receives a redirection-disabled
///     copy; Buffered untouched; null env values preserved) is pinned separately by
///     <c>PipelineDispatchTests</c>.
/// </summary>
public class RawModeRedirectionNormalizationTests
{
    /// <summary>
    ///     Payload size the chatty child writes to stdout. Deliberately larger than the
    ///     largest default OS pipe buffer (64 KiB on Linux) so that, without the
    ///     normalization, the child blocks mid-write on the undrained pipe and the run
    ///     deadlocks. With the fix in place the child inherits the test host's stdout,
    ///     so the payload lands in the captured test output — written as spaces it
    ///     renders as one blank line instead of flooding the log.
    /// </summary>
    private const int ChattyPayloadBytes = 70000;

    /// <summary>
    ///     Fail-fast bound for the run. A regression deadlocks the child (it blocks on
    ///     the full pipe) and the test fails with a timeout instead of stalling the
    ///     suite; no assertion depends on this bound firing.
    /// </summary>
    private static readonly TimeSpan GuardTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     The H2 hazard shape end to end: Raw mode + <c>outputRedirection: true</c>
    ///     (the position-argument constructor's default) with a child that writes well
    ///     beyond the pipe buffer. The run must complete with exit code 0; pre-fix it
    ///     deadlocked until the default timeout policy killed the child.
    /// </summary>
    [Test]
    public async Task RunAsync_RawRedirectedChattyChild_CompletesWithoutDeadlock()
    {
        (string targetFilePath, string arguments) = GetChattyCommand();

        ProcessConfiguration configuration = new(targetFilePath, arguments,
            outputRedirection: true);

        ProcessResult result = await CliRun.RunAsync(configuration,
                ProcessExitConfiguration.CreateGraceful())
            .WaitAsync(GuardTimeout).ConfigureAwait(true);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Canceled).IsFalse();
    }

    /// <summary>
    ///     Regression guard for the non-deadlocking half of the shape: a quiet child
    ///     under the same Raw + redirected configuration still runs to completion with
    ///     exit code 0 after the normalization.
    /// </summary>
    [Test]
    public async Task RunAsync_RawRedirectedQuietChild_CompletesSuccessfully()
    {
        (string targetFilePath, string arguments) = GetQuietCommand();

        ProcessConfiguration configuration = new(targetFilePath, arguments,
            outputRedirection: true);

        ProcessResult result = await CliRun.RunAsync(configuration,
                ProcessExitConfiguration.CreateGraceful())
            .WaitAsync(GuardTimeout).ConfigureAwait(true);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Canceled).IsFalse();
    }

    /// <summary>
    ///     Returns the per-OS chatty child that writes <see cref="ChattyPayloadBytes"/>
    ///     bytes to stdout and exits 0. Skips the test when the child executable is
    ///     unavailable on PATH.
    /// </summary>
    private static (string TargetFilePath, string Arguments) GetChattyCommand()
    {
        if (OperatingSystem.IsWindows())
        {
            string powerShell = ResolveRequired("powershell.exe",
                "Windows PowerShell (powershell.exe) is not available on PATH.");
            return (powerShell,
                $@"-NoProfile -Command ""[Console]::Out.Write(' ' * {ChattyPayloadBytes})""");
        }

        string sh = ResolveRequired("sh", "A POSIX shell (sh) is not available on PATH.");
        return (sh,
            $@"-c ""dd if=/dev/zero bs={ChattyPayloadBytes} count=1 2>/dev/null | tr '\0' ' '""");
    }

    /// <summary>
    ///     Returns the per-OS quiet child that writes nothing and exits 0.
    ///     Skips the test when the child executable is unavailable on PATH.
    /// </summary>
    private static (string TargetFilePath, string Arguments) GetQuietCommand()
    {
        if (OperatingSystem.IsWindows())
        {
            string powerShell = ResolveRequired("powershell.exe",
                "Windows PowerShell (powershell.exe) is not available on PATH.");
            return (powerShell, "-NoProfile -Command \"exit 0\"");
        }

        string sh = ResolveRequired("sh", "A POSIX shell (sh) is not available on PATH.");
        return (sh, "-c \"exit 0\"");
    }

    /// <summary>
    ///     Resolves <paramref name="fileName"/> against every PATH entry, mirroring
    ///     the skip convention used by <c>StdinPipingClaimTests</c>.
    /// </summary>
    /// <param name="fileName">The executable file name to locate.</param>
    /// <returns>The full path to the executable, or <c>null</c> when not found.</returns>
    private static string? ResolveOnPath(string fileName)
    {
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv is null)
            return null;

        foreach (string directory in pathEnv.Split(Path.PathSeparator))
        {
            string trimmed = directory.Trim();
            if (trimmed.Length == 0) continue;

            string candidate = Path.Combine(trimmed, fileName);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>
    ///     Resolves <paramref name="fileName"/> against PATH or skips the current test
    ///     with <paramref name="reason"/> when the executable is unavailable, so
    ///     per-OS child gaps skip cleanly instead of failing.
    /// </summary>
    /// <param name="fileName">The executable file name to locate.</param>
    /// <param name="reason">The skip reason stated when the executable is missing.</param>
    /// <returns>The full path to the executable.</returns>
    private static string ResolveRequired(string fileName, string reason)
    {
        string? resolved = ResolveOnPath(fileName);
        if (resolved is null)
            throw new SkipTestException(reason);

        return resolved;
    }
}

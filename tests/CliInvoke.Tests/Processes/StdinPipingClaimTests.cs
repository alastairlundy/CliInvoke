/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Text;

using CliInvoke.Processes;

using TUnit.Core.Exceptions;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Real-executable end-to-end proofs for the stdin-piping claims:
///     Raw and Buffered runs through the documented public API deliver the configured
///     <see cref="ProcessConfiguration.StandardInput"/> to a real child over real OS
///     pipes, and a stdin-reading child exits on its own once the write end closes.
///     These complement the deterministic stub matrix in
///     <see cref="StdinPipingStubTests"/>, which covers what real processes cannot:
///     kill and cancellation timing. No test here depends on kill or cancellation timing.
/// </summary>
public class StdinPipingClaimTests
{
    /// <summary>
    ///     The payload every child compares its received stdin against. Pure ASCII so
    ///     decoding is identical across shell, PowerShell, and console code pages.
    /// </summary>
    private const string Payload = "hello-stdin";

    /// <summary>
    ///     Number of payload lines fed through Buffered mode. Sized well above the OS
    ///     pipe buffer so the stdin copy and the output drain must overlap for the run
    ///     to complete.
    /// </summary>
    private const int BufferedLineCount = 6000;

    /// <summary>
    ///     Fail-fast bound for every real-process wait. A regression hangs the run
    ///     (the child never exits) and the test fails with a timeout instead of
    ///     stalling the suite; no assertion depends on this bound firing.
    /// </summary>
    private static readonly TimeSpan GuardTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Claim 1 (Raw): <see cref="CliRun.RunAsync(CliInvoke.Core.ProcessConfiguration, CliInvoke.Core.ProcessExitConfiguration?, System.Threading.CancellationToken)"/> pipes the configured
    ///     stdin to a real child. The child reads exactly one line and exits 0 only
    ///     when it equals the payload, so a non-piped or corrupted payload fails the
    ///     test. The payload is newline-terminated so the read completes on the
    ///     newline — this test proves data delivery, while end-of-file delivery is
    ///     proved separately by <see cref="StartAsync_WithConfiguration_StdinReadingChild_ExitsOnItsOwnAfterWriteEndCloses"/>.
    /// </summary>
    [Test]
    public async Task RunAsync_PipesConfiguredStdinToRealChild_PayloadIsConsumed()
    {
        (string targetFilePath, string arguments) = GetLineCheckingCommand();

        using MemoryStream source = new(Encoding.ASCII.GetBytes(Payload + "\n"));
        using StreamWriter standardInput = new(source);
        ProcessConfiguration configuration = new(targetFilePath, arguments, outputRedirection: false)
        {
            RedirectStandardInput = true,
            StandardInput = standardInput,
        };

        ProcessResult result = await CliRun.RunAsync(configuration,
                ProcessExitConfiguration.CreateGraceful())
            .WaitAsync(GuardTimeout);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Canceled).IsFalse();
    }

    /// <summary>
    ///     Claim 1 (Buffered): <see cref="CliRun.RunBufferedAsync(CliInvoke.Core.ProcessConfiguration, CliInvoke.Core.ProcessExitConfiguration?, System.Threading.CancellationToken)"/> completes
    ///     while the child consumes stdin concurrently with output capture. The echo
    ///     child streams stdin to stdout until end-of-file, and the payload is far
    ///     larger than the OS pipe buffer, so the run can only complete if the stdin
    ///     copy and the output drain progress together. The captured output containing
    ///     the final payload line proves the child received the input end to end.
    /// </summary>
    [Test]
    public async Task RunBufferedAsync_PipesStdinWhileCapturingOutput_CompletesWithFullPayload()
    {
        (string targetFilePath, string arguments) = GetEchoCommand();

        StringBuilder payloadBuilder = new();
        for (int i = 0; i < BufferedLineCount; i++)
        {
            payloadBuilder.Append("cli-invoke stdin claim line ").Append(i).Append('\n');
        }

        string firstLine = "cli-invoke stdin claim line 0";
        string lastLine = "cli-invoke stdin claim line " + (BufferedLineCount - 1);

        using MemoryStream source = new(Encoding.ASCII.GetBytes(payloadBuilder.ToString()));
        using StreamWriter standardInput = new(source);
        ProcessConfiguration configuration = new(targetFilePath, arguments, outputRedirection: true)
        {
            RedirectStandardInput = true,
            StandardInput = standardInput,
        };

        BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
                ProcessExitConfiguration.CreateGraceful())
            .WaitAsync(GuardTimeout);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).Contains(firstLine);
        await Assert.That(result.StandardOutput).Contains(lastLine);
        await Assert.That(result.Canceled).IsFalse();
    }

    /// <summary>
    ///     Claim 2 (the EOF proof): after <c>StartAsync(config, ct)</c> returns,
    ///     the finally-close has run and a stdin-reading child exits on its own — it
    ///     receives no further input, is never killed, and is never cancelled. The
    ///     payload carries no trailing newline, so the child's read can only complete
    ///     when the write end delivers end-of-file over the real OS pipe; a missing
    ///     close leaves the child blocked and the wait below fails with a stated
    ///     reason. End-of-file is therefore proven by process exit, not by timing.
    /// </summary>
    [Test]
    public async Task StartAsync_WithConfiguration_StdinReadingChild_ExitsOnItsOwnAfterWriteEndCloses()
    {
        (string targetFilePath, string arguments) = GetEofCheckingCommand();

        using MemoryStream source = new(Encoding.ASCII.GetBytes(Payload));
        using StreamWriter standardInput = new(source);
        ProcessConfiguration configuration = new(targetFilePath, arguments, outputRedirection: true)
        {
            RedirectStandardInput = true,
            StandardInput = standardInput,
        };

        using ExternalProcess process = new(new FilePathResolver(), configuration);

        await process.StartAsync(configuration, CancellationToken.None).WaitAsync(GuardTimeout);

        await ProcessTestHelper.WaitForConditionAsync(
            () => process.HasExited,
            GuardTimeout,
            failureMessage:
            "The stdin-reading child never exited: the child's stdin write end was not closed, so end-of-file was never delivered.");

        ProcessResult result = await process.WaitForExitOrTimeoutAsync(CancellationToken.None)
            .WaitAsync(GuardTimeout);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Canceled).IsFalse();
    }

    /// <summary>
    ///     Returns the per-OS child that reads one line of stdin and exits 0 only when
    ///     it equals <see cref="Payload"/>. Skips the test when the child executable
    ///     is unavailable on PATH.
    /// </summary>
    private static (string TargetFilePath, string Arguments) GetLineCheckingCommand()
    {
        if (OperatingSystem.IsWindows())
        {
            string powerShell = ResolveRequired("powershell.exe",
                "Windows PowerShell (powershell.exe) is not available on PATH.");
            return (powerShell,
                $@"-NoProfile -Command ""if ([Console]::In.ReadLine() -eq '{Payload}') {{ exit 0 }} else {{ exit 1 }}""");
        }

        string sh = ResolveRequired("sh", "A POSIX shell (sh) is not available on PATH.");
        return (sh, $"-c \"read line && [ $line = {Payload} ]\"");
    }

    /// <summary>
    ///     Returns the per-OS child that copies stdin to stdout until end-of-file.
    ///     Skips the test when the child executable is unavailable on PATH.
    /// </summary>
    private static (string TargetFilePath, string Arguments) GetEchoCommand()
    {
        if (OperatingSystem.IsWindows())
        {
            string powerShell = ResolveRequired("powershell.exe",
                "Windows PowerShell (powershell.exe) is not available on PATH.");
            return (powerShell,
                @"-NoProfile -Command ""[Console]::Out.Write([Console]::In.ReadToEnd())""");
        }

        string cat = ResolveRequired("cat", "The cat utility is not available on PATH.");
        return (cat, string.Empty);
    }

    /// <summary>
    ///     Returns the per-OS child that reads stdin until end-of-file and then exits
    ///     0 only when the received payload matches. Because the payload has no
    ///     trailing newline, the child cannot exit before the write end closes.
    ///     Skips the test when the child executable is unavailable on PATH.
    /// </summary>
    private static (string TargetFilePath, string Arguments) GetEofCheckingCommand()
    {
        if (OperatingSystem.IsWindows())
        {
            string powerShell = ResolveRequired("powershell.exe",
                "Windows PowerShell (powershell.exe) is not available on PATH.");
            return (powerShell,
                $@"-NoProfile -Command ""$t = [Console]::In.ReadToEnd(); if ($t -eq '{Payload}') {{ exit 0 }} else {{ exit 1 }}""");
        }

        string sh = ResolveRequired("sh", "A POSIX shell (sh) is not available on PATH.");
        return (sh, $"-c \"read line; [ $line = {Payload} ]\"");
    }

    /// <summary>
    ///     Resolves <paramref name="fileName"/> against every PATH entry, mirroring
    ///     the skip convention used by <c>RunnerConfigurationFactoryTests</c>.
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

/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Tests.Invokers;

/// <summary>
/// Exercises the public surface of <see cref="CliRun"/> by running real processes.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CliRun"/> is a batteries-included defaults facade with no injectable
/// factory, so these tests spawn a real, per-OS child process — the quiet child from
/// <see cref="ProcessTestHelper.GetQuietChildCommand"/> — and assert on its exit code
/// or process id rather than on a fake factory. They are deterministic and
/// do not touch shared static state.
/// </para>
/// <para>
/// The child is deliberately quiet: <see cref="CliRun.RunAsync(CliInvoke.Core.ProcessConfiguration, CliInvoke.Core.ProcessExitConfiguration?, System.Threading.CancellationToken)"/>
/// (Raw mode) and <c>FireAndForget</c> leave redirection off, so those children inherit
/// the test host's console, and an output-producing child — such as <c>dotnet --version</c> —
/// leaks its banner into test output (a raw child write displaces the test runner's live
/// console redraws, wrapping into blank rows). No assertion here depends on what the
/// child writes, only on its exit code and process id.
/// </para>
/// </remarks>
public class CliRunTests
{
    [Test]
    public async Task RunAsync_WithConfig_RunsProcessAndReturnsSuccess()
    {
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();

        ProcessConfiguration configuration = new(targetFilePath, arguments);

        ProcessResult result = await CliRun.RunAsync(configuration,
            ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunBufferedAsync_WithConfig_RunsProcessAndReturnsSuccess()
    {
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();

        ProcessConfiguration configuration = new(targetFilePath, arguments);

        BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
            ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WithStringArgs_RunsProcessAndReturnsSuccess()
    {
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();

        ProcessResult result = await CliRun.RunAsync(targetFilePath, arguments).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunBufferedAsync_WithStringArgs_RunsProcessAndReturnsSuccess()
    {
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();

        BufferedProcessResult result = await CliRun.RunBufferedAsync(targetFilePath, arguments).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WhenProcessCannotBeResolved_Throws()
    {
        await Assert.That(async () =>
                await CliRun.RunAsync("NONEXISTENT_COMMAND_ABC123.exe")
                    .ConfigureAwait(true))
            .Throws<FileNotFoundException>();
    }

    [Test]
    public async Task FireAndForget_WithConfig_StartsProcessAndReturnsProcessId()
    {
        // Redirecting output is rejected by FireAndForget (the process handle is
        // disposed immediately after start, so nobody drains the pipes); pass
        // outputRedirection: false explicitly, because the ctor defaults it to true.
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();

        ProcessConfiguration configuration =
            new ProcessConfiguration(targetFilePath, arguments, outputRedirection: false);

        int processId = CliRun.FireAndForget(configuration);

        await Assert.That(processId).IsGreaterThan(0);
    }

    [Test]
    public async Task FireAndForget_WithStringArgs_StartsProcessAndReturnsProcessId()
    {
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();

        int processId = CliRun.FireAndForget(targetFilePath, arguments);

        await Assert.That(processId).IsGreaterThan(0);
    }

    [Test]
    public async Task FireAndForget_WithInvalidConfig_Throws()
    {
        ProcessConfiguration configuration =
            new ProcessConfiguration("NONEXISTENT_COMMAND_ABC123.exe", 
                outputRedirection: false);

        await Assert.That(() => CliRun.FireAndForget(configuration))
            .Throws<FileNotFoundException>();
    }

    [Test]
    public async Task FireAndForget_WithRedirectedOutput_Throws()
    {
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();

        ProcessConfiguration configuration =
            new ProcessConfiguration(targetFilePath, arguments)
                { OutputRedirection = true };

        await Assert.That(() => CliRun.FireAndForget(configuration))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task FireAndForget_WithRedirectedStandardInput_Throws()
    {
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();

        ProcessConfiguration configuration =
            new ProcessConfiguration(targetFilePath, arguments)
                { RedirectStandardInput = true };

        await Assert.That(() => CliRun.FireAndForget(configuration))
            .Throws<ArgumentException>();
    }
}
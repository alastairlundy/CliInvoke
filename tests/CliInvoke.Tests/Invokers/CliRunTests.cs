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
/// factory, so these tests spawn a real, cross-platform process (<c>dotnet --version</c>)
/// and assert on its exit code rather than on a fake factory. They are deterministic and
/// do not touch shared static state.
/// </para>
/// </remarks>
public class CliRunTests
{
    private const string TargetFilePath = "dotnet";
    private const string TargetArguments = "--version";

    [Test]
    public async Task RunAsync_WithConfig_RunsProcessAndReturnsSuccess()
    {
        ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments);

        ProcessResult result = await CliRun.RunAsync(configuration,
            ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunBufferedAsync_WithConfig_RunsProcessAndReturnsSuccess()
    {
        ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments);

        BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
            ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WithStringArgs_RunsProcessAndReturnsSuccess()
    {
        ProcessResult result = await CliRun.RunAsync(TargetFilePath, TargetArguments).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunBufferedAsync_WithStringArgs_RunsProcessAndReturnsSuccess()
    {
        BufferedProcessResult result = await CliRun.RunBufferedAsync(TargetFilePath, TargetArguments).ConfigureAwait(true);

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
        ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments, outputRedirection: false);

        int processId = CliRun.FireAndForget(configuration);

        await Assert.That(processId).IsGreaterThan(0);
    }

    [Test]
    public async Task FireAndForget_WithStringArgs_StartsProcessAndReturnsProcessId()
    {
        int processId = CliRun.FireAndForget(TargetFilePath, TargetArguments);

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
        ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments)
                { OutputRedirection = true };

        await Assert.That(() => CliRun.FireAndForget(configuration))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task FireAndForget_WithRedirectedStandardInput_Throws()
    {
        ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments)
                { RedirectStandardInput = true };

        await Assert.That(() => CliRun.FireAndForget(configuration))
            .Throws<ArgumentException>();
    }
}
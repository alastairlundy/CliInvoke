/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Processes;

namespace CliInvoke.Tests;

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
        using ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments);

        ProcessResult result = await CliRun.RunAsync(configuration,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunBufferedAsync_WithConfig_RunsProcessAndReturnsSuccess()
    {
        using ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments);

        BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunPipedAsync_WithConfig_RunsProcessAndReturnsSuccess()
    {
        using ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments);

        PipedProcessResult result = await CliRun.RunPipedAsync(configuration,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WithStringArgs_RunsProcessAndReturnsSuccess()
    {
        ProcessResult result = await CliRun.RunAsync(TargetFilePath, TargetArguments);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunBufferedAsync_WithStringArgs_RunsProcessAndReturnsSuccess()
    {
        BufferedProcessResult result = await CliRun.RunBufferedAsync(TargetFilePath, TargetArguments);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunPipedAsync_WithStringArgs_RunsProcessAndReturnsSuccess()
    {
        PipedProcessResult result = await CliRun.RunPipedAsync(TargetFilePath, TargetArguments);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task RunAsync_WhenProcessCannotBeResolved_Throws()
    {
        await Assert.That(async () =>
                await CliRun.RunAsync("NONEXISTENT_COMMAND_ABC123.exe", ""))
            .Throws<FileNotFoundException>();
    }

    [Test]
    public async Task FireAndForget_WithConfig_StartsProcessAndReturnsProcessId()
    {
        using ProcessConfiguration configuration =
            new ProcessConfiguration(TargetFilePath, TargetArguments);

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
        using ProcessConfiguration configuration =
            new ProcessConfiguration("NONEXISTENT_COMMAND_ABC123.exe", "");

        await Assert.That(() => CliRun.FireAndForget(configuration))
            .Throws<FileNotFoundException>();
    }
}

/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Processes;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Verifies the no-mutation contract (D002): <c>Configuration.TargetFilePath</c>
///     is never rewritten by <see cref="ExternalProcess.Start"/> or
///     <see cref="ExternalProcess.StartAsync(CancellationToken)"/>, while the
///     result's <c>ExecutedFilePath</c> reflects the resolved path.
/// </summary>
public class ExternalProcessNoMutationTests
{
    private readonly string _targetFilePath = ProcessTestHelper.GetTargetFilePath();

    [Test]
    public async Task Start_DoesNotMutateConfiguration_ExecutedFilePathIsResolved()
    {
        using ProcessConfiguration configuration = new(_targetFilePath);

        string originalTargetFilePath = configuration.TargetFilePath;

        using ExternalProcess process = new(configuration);

        process.Start();
        ProcessResult result = await process.WaitForExitOrTimeoutAsync(CancellationToken.None);

        try
        {
            await Assert.That(configuration.TargetFilePath).IsEqualTo(originalTargetFilePath);

            FilePathResolver resolver = new();
            FileInfo resolvedPath = resolver.ResolveFilePath(_targetFilePath);
            await Assert.That(result.ExecutedFilePath).IsEqualTo(resolvedPath.FullName);
        }
        finally
        {
            await process.Kill();
        }
    }

    [Test]
    public async Task StartAsync_DoesNotMutateConfiguration_ExecutedFilePathIsResolved()
    {
        using ProcessConfiguration configuration = new(_targetFilePath);

        string originalTargetFilePath = configuration.TargetFilePath;

        using ExternalProcess process = new(configuration);

        await process.StartAsync(CancellationToken.None);
        ProcessResult result = await process.WaitForExitOrTimeoutAsync(CancellationToken.None);

        try
        {
            await Assert.That(configuration.TargetFilePath).IsEqualTo(originalTargetFilePath);

            FilePathResolver resolver = new();
            FileInfo resolvedPath = resolver.ResolveFilePath(_targetFilePath);
            await Assert.That(result.ExecutedFilePath).IsEqualTo(resolvedPath.FullName);
        }
        finally
        {
            await process.Kill();
        }
    }

    [Test]
    public async Task CaptureBufferedResult_Start_DoesNotMutateConfiguration()
    {
        using ProcessConfiguration configuration = new(_targetFilePath);

        string originalTargetFilePath = configuration.TargetFilePath;

        using ExternalProcess process = new(configuration);

        process.Start();
        BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);

        try
        {
            await Assert.That(configuration.TargetFilePath).IsEqualTo(originalTargetFilePath);

            FilePathResolver resolver = new();
            FileInfo resolvedPath = resolver.ResolveFilePath(_targetFilePath);
            await Assert.That(result.ExecutedFilePath).IsEqualTo(resolvedPath.FullName);
        }
        finally
        {
            await process.Kill();
        }
    }
}

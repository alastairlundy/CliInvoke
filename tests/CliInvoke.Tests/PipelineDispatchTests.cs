/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;

namespace CliInvoke.Tests;

internal class PipelineDispatchTests : IDisposable
{
    private readonly CountingExternalProcessFactory _factory;
    private readonly string _targetFilePath;
    private readonly List<ProcessConfiguration> _configurations = new();

    public PipelineDispatchTests()
    {
        _factory = new CountingExternalProcessFactory();
        _targetFilePath = ProcessTestHelper.GetTargetFilePath();
    }

    public void Dispose()
    {
        foreach (ProcessConfiguration config in _configurations)
        {
            config.Dispose();
        }

        _factory.Dispose();
    }

    private ProcessInvocationContext CreateContext(InvocationMode mode,
        CancellationToken cancellationToken = default)
    {
        ProcessConfiguration config = ProcessConfigurationFactory.Create(_targetFilePath);
        ProcessExitConfiguration exitConfig = ProcessExitConfiguration.CreateGraceful();
        _configurations.Add(config);
        return new ProcessInvocationContext(config, exitConfig, mode, cancellationToken);
    }

    [Test]
    public async Task InvokeAsync_Raw_ReturnsProcessResult()
    {
        var pipeline = new ProcessInvocationPipeline(_factory);
        ProcessInvocationContext ctx = CreateContext(InvocationMode.Raw);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(ctx);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(_factory.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_Buffered_ReturnsBufferedProcessResult()
    {
        var pipeline = new ProcessInvocationPipeline(_factory);
        ProcessInvocationContext ctx = CreateContext(InvocationMode.Buffered);

        BufferedProcessResult result =
            await pipeline.InvokeAsync<BufferedProcessResult>(ctx);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(_factory.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_Piped_ReturnsPipedProcessResult()
    {
        var pipeline = new ProcessInvocationPipeline(_factory);
        ProcessInvocationContext ctx = CreateContext(InvocationMode.Piped);

        PipedProcessResult result =
            await pipeline.InvokeAsync<PipedProcessResult>(ctx);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(_factory.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_FireAndForget_ReturnsProcessResult()
    {
        var pipeline = new ProcessInvocationPipeline(_factory);
        ProcessInvocationContext ctx = CreateContext(InvocationMode.FireAndForget);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(ctx);

        await Assert.That(result).IsNotNull();
        await Assert.That(_factory.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var pipeline = new ProcessInvocationPipeline(_factory);
        ProcessInvocationContext ctx = CreateContext(InvocationMode.Raw,
            new CancellationToken(true));

        await Assert.That(async () => await pipeline.InvokeAsync<ProcessResult>(ctx))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task InvokeAsync_StartThrows_PropagatesException()
    {
        _factory.ThrowOnStart =
            new InvalidOperationException("simulated start failure");
        var pipeline = new ProcessInvocationPipeline(_factory);
        ProcessInvocationContext ctx = CreateContext(InvocationMode.Raw);

        await Assert.That(async () => await pipeline.InvokeAsync<ProcessResult>(ctx))
            .Throws<InvalidOperationException>();
    }
}

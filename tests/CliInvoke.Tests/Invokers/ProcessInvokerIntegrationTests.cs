/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Tests.TestData;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Tests.Invokers;

[ClassDataSource<TestFixture>(Shared = SharedType.PerClass)]
public class ProcessInvokerIntegrationTests
{
    private readonly TestFixture _testFixture;
    private readonly string _targetFilePath;

    public ProcessInvokerIntegrationTests(TestFixture testFixture)
    {
        _testFixture = testFixture;
        _targetFilePath = ProcessTestHelper.GetTargetFilePath();
    }

    [Test]
    public async Task ExecuteAsync_ReturnsSuccessfulResult()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config =
            ProcessConfigurationFactory.Create(_targetFilePath, "");

        ProcessResult result =
            await processInvoker.ExecuteAsync(config,
                ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteBufferedAsync_ReturnsSuccessfulResult()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config =
            ProcessConfigurationFactory.Create(_targetFilePath, "");

        BufferedProcessResult result =
            await processInvoker.ExecuteBufferedAsync(config,
                ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ExecutePipedAsync_ReturnsSuccessfulResult()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config =
            ProcessConfigurationFactory.Create(_targetFilePath, "");

        PipedProcessResult result =
            await processInvoker.ExecutePipedAsync(config,
                ProcessExitConfiguration.CreateGraceful());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config =
            ProcessConfigurationFactory.Create(_targetFilePath, "");

        await Assert.That(async () =>
                await processInvoker.ExecuteAsync(config,
                    ProcessExitConfiguration.CreateGraceful(),
                    new CancellationToken(true)))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task ExecuteBufferedAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config =
            ProcessConfigurationFactory.Create(_targetFilePath, "");

        await Assert.That(async () =>
                await processInvoker.ExecuteBufferedAsync(config,
                    ProcessExitConfiguration.CreateGraceful(),
                    new CancellationToken(true)))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task ExecutePipedAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        using ProcessConfiguration config =
            ProcessConfigurationFactory.Create(_targetFilePath, "");

        await Assert.That(async () =>
                await processInvoker.ExecutePipedAsync(config,
                    ProcessExitConfiguration.CreateGraceful(),
                    new CancellationToken(true)))
            .Throws<OperationCanceledException>();
    }
}

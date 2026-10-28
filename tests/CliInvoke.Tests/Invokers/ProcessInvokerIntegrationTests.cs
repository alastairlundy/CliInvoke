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

        // /c keeps the child non-interactive: since the H2 Raw-mode normalization removed
        // output redirection, a bare `cmd` inherits the host's interactive console stdin
        // and waits at its REPL for keyboard input instead of exiting. The test's intent
        // is a trivially successful process, not cmd's REPL behaviour.
        ProcessConfiguration config =
            new ProcessConfiguration(_targetFilePath, ProcessTestHelper.GetExitingArguments());

        ProcessResult result =
            await processInvoker.ExecuteAsync(config,
                ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteBufferedAsync_ReturnsSuccessfulResult()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        ProcessConfiguration config =
            new ProcessConfiguration(_targetFilePath, ProcessTestHelper.GetExitingArguments());

        BufferedProcessResult result =
            await processInvoker.ExecuteBufferedAsync(config,
                ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        ProcessConfiguration config =
            new ProcessConfiguration(_targetFilePath, ProcessTestHelper.GetExitingArguments());

        await Assert.That(async () =>
                await processInvoker.ExecuteAsync(config,
                    ProcessExitConfiguration.CreateGraceful(),
                    new CancellationToken(true)).ConfigureAwait(true))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task ExecuteBufferedAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        IProcessInvoker processInvoker =
            _testFixture.ServiceProvider.GetRequiredService<IProcessInvoker>();

        ProcessConfiguration config =
            new ProcessConfiguration(_targetFilePath, ProcessTestHelper.GetExitingArguments());

        await Assert.That(async () =>
                await processInvoker.ExecuteBufferedAsync(config,
                    ProcessExitConfiguration.CreateGraceful(),
                    new CancellationToken(true)).ConfigureAwait(true))
            .Throws<OperationCanceledException>();
    }

}

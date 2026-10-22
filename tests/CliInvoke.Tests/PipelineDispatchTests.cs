/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Tests;

internal class PipelineDispatchTests : IDisposable
{
    private readonly CountingExternalProcessFactory _factory;
    private readonly string _targetFilePath;

    public PipelineDispatchTests()
    {
        _factory = new CountingExternalProcessFactory();
        _targetFilePath = ProcessTestHelper.GetTargetFilePath();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private InvocationContext CreateContext(InvocationMode mode,
        CancellationToken cancellationToken = default)
    {
        ProcessConfiguration config = new ProcessConfiguration(_targetFilePath, "");
        ProcessExitConfiguration exitConfig = ProcessExitConfiguration.CreateGraceful();
        return new InvocationContext(config, exitConfig, mode, cancellationToken);
    }

    [Test]
    public async Task InvokeAsync_Raw_ReturnsProcessResult()
    {
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);
        InvocationContext ctx = CreateContext(InvocationMode.Raw);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(ctx);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(_factory.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_Buffered_ReturnsBufferedProcessResult()
    {
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);
        InvocationContext ctx = CreateContext(InvocationMode.Buffered);

        BufferedProcessResult result =
            await pipeline.InvokeAsync<BufferedProcessResult>(ctx);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(_factory.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_FireAndForget_ReturnsProcessResult()
    {
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);
        InvocationContext ctx = CreateContext(InvocationMode.FireAndForget);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(ctx);

        await Assert.That(result).IsNotNull();
        await Assert.That(_factory.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task InvokeAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);
        InvocationContext ctx = CreateContext(InvocationMode.Raw,
            new CancellationToken(true));

        await Assert.That(async () => await pipeline.InvokeAsync<ProcessResult>(ctx))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task InvokeAsync_StartThrows_PropagatesException()
    {
        _factory.ThrowOnStart =
            new InvalidOperationException("simulated start failure");
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);
        InvocationContext ctx = CreateContext(InvocationMode.Raw);

        await Assert.That(async () => await pipeline.InvokeAsync<ProcessResult>(ctx))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    ///     H2: Raw mode waits for exit without draining redirected output, so the pipeline
    ///     must hand the factory a redirection-disabled copy of a redirected configuration
    ///     (the raw-mode pipe deadlock normalization), leaving the caller's configuration
    ///     untouched.
    /// </summary>
    [Test]
    public async Task InvokeAsync_Raw_RedirectedConfig_FactoryReceivesRedirectionDisabledCopy()
    {
        ProcessConfiguration config =
            new ProcessConfiguration(_targetFilePath, "", outputRedirection: true);
        ProcessExitConfiguration exitConfig = ProcessExitConfiguration.CreateGraceful();
        InvocationContext ctx = new InvocationContext(config, exitConfig,
            InvocationMode.Raw, default);
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);

        await pipeline.InvokeAsync<ProcessResult>(ctx);

        await Assert.That(_factory.LastConfiguration).IsNotNull();
        await Assert.That(_factory.LastConfiguration!.OutputRedirection).IsFalse();
        await Assert.That(_factory.LastConfiguration).IsNotSameReferenceAs(config);
        await Assert.That(config.OutputRedirection).IsTrue();
    }

    /// <summary>
    ///     Buffered mode drains its redirected streams via the capture methods, so the
    ///     Raw-mode normalization must leave its redirection enabled — stripping it would
    ///     silently discard buffered output.
    /// </summary>
    [Test]
    public async Task InvokeAsync_Buffered_RedirectedConfig_PreservesRedirection()
    {
        ProcessConfiguration config =
            new ProcessConfiguration(_targetFilePath, "", outputRedirection: true);
        ProcessExitConfiguration exitConfig = ProcessExitConfiguration.CreateGraceful();
        InvocationContext ctx = new InvocationContext(config, exitConfig,
            InvocationMode.Buffered, default);
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);

        await pipeline.InvokeAsync<BufferedProcessResult>(ctx);

        await Assert.That(_factory.LastConfiguration).IsNotNull();
        await Assert.That(_factory.LastConfiguration!.OutputRedirection).IsTrue();
    }

    /// <summary>
    ///     The common non-redirected Raw path needs no normalization, so the pipeline must
    ///     pass the caller's configuration through without allocating a copy.
    /// </summary>
    [Test]
    public async Task InvokeAsync_Raw_NonRedirectedConfig_PassesThroughWithoutCopy()
    {
        ProcessConfiguration config =
            new ProcessConfiguration(_targetFilePath, "", outputRedirection: false);
        ProcessExitConfiguration exitConfig = ProcessExitConfiguration.CreateGraceful();
        InvocationContext ctx = new InvocationContext(config, exitConfig,
            InvocationMode.Raw, default);
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);

        await pipeline.InvokeAsync<ProcessResult>(ctx);

        await Assert.That(_factory.LastConfiguration).IsNotNull();
        await Assert.That(_factory.LastConfiguration).IsSameReferenceAs(config);
        await Assert.That(_factory.LastConfiguration!.OutputRedirection).IsFalse();
    }

    /// <summary>
    ///     L6 regression guard for the H2 normalization: a null env-var value is the
    ///     documented removal signal and must survive the Raw-mode copy. A builder
    ///     round-trip would throw <see cref="ArgumentNullException"/> here because
    ///     <c>EnvironmentVariablesSpec</c> rejects nulls — the normalization is therefore
    ///     a faithful member-wise copy.
    /// </summary>
    [Test]
    public async Task InvokeAsync_Raw_RedirectedConfigWithNullEnvValue_PreservesNullValue()
    {
        const string VariableName = "CLIINVOKE_H2_NULL_ENV";
        ProcessConfiguration config =
            new ProcessConfiguration(_targetFilePath, "", outputRedirection: true)
            {
                EnvironmentVariables = new Dictionary<string, string>
                {
                    [VariableName] = null!
                }
            };
        ProcessExitConfiguration exitConfig = ProcessExitConfiguration.CreateGraceful();
        InvocationContext ctx = new InvocationContext(config, exitConfig,
            InvocationMode.Raw, default);
        ProcessInvocationPipeline pipeline = new ProcessInvocationPipeline(_factory);

        await pipeline.InvokeAsync<ProcessResult>(ctx);

        await Assert.That(_factory.LastConfiguration).IsNotNull();
        await Assert.That(_factory.LastConfiguration!.EnvironmentVariables[VariableName])
            .IsNull();
    }
}

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

/// <summary>
/// Exercises the public surface of <see cref="CliRun"/> through the
/// <see cref="IExternalProcessFactory"/> hook installed by
/// <see cref="CliRun.UseExternalProcessFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests do not spawn real processes. <see cref="CountingExternalProcessFactory"/>
/// is wired in for every test, returns a stub <see cref="IExternalProcess"/>, and
/// records how many times the factory delegate is invoked so each
/// <c>CliRun.*Async</c> entry point can be asserted independently.
/// </para>
/// <para>
/// The class is marked <c>[NotInParallel]</c> because <see cref="CliRun"/> keeps
/// shared static state for its factory and resolver. Each test receives a fresh
/// factory; the <c>[After]</c> hook resets captured counters between tests so
/// the sequence in which the tests run is irrelevant.
/// </para>
/// </remarks>
[NotInParallel]
public class CliRunTests : IDisposable
{
    private readonly CountingExternalProcessFactory _factory;
    private readonly CapturingFilePathResolver _resolver;
    private readonly string _targetFilePath;

    public CliRunTests()
    {
        _factory = new CountingExternalProcessFactory();
        _resolver = new CapturingFilePathResolver();
        _targetFilePath = ProcessTestHelper.GetTargetFilePath();

        CliRun.UseExternalProcessFactory(_factory);
        CliRun.UseFilePathResolver(_resolver);
    }

    [After(HookType.Test)]
    public void ResetFakes()
    {
        _factory.Reset();
        _resolver.Reset();
    }

    public void Dispose()
    {
        _factory.Dispose();
        _resolver.Dispose();
    }

    [Test]
    public async Task RunAsync_WithConfig_RoutesThroughFactory()
    {
        using ProcessConfiguration configuration =
            ProcessConfigurationFactory.Create(_targetFilePath);

        ProcessResult result = await CliRun.RunAsync(configuration,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(_factory.CreateCount).IsEqualTo(1);
        await Assert.That(_factory.LastConfiguration).IsSameReferenceAs(configuration);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RunBufferedAsync_WithConfig_RoutesThroughFactory()
    {
        using ProcessConfiguration configuration =
            ProcessConfigurationFactory.Create(_targetFilePath, outputRedirection: true);

        BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(_factory.CreateCount).IsEqualTo(1);
        await Assert.That(_factory.LastConfiguration).IsSameReferenceAs(configuration);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RunPipedAsync_WithConfig_RoutesThroughFactory()
    {
        using ProcessConfiguration configuration =
            ProcessConfigurationFactory.Create(_targetFilePath, outputRedirection: true);

        PipedProcessResult result = await CliRun.RunPipedAsync(configuration,
            ProcessExitConfiguration.CreateGraceful());

        await Assert.That(_factory.CreateCount).IsEqualTo(1);
        await Assert.That(_factory.LastConfiguration).IsSameReferenceAs(configuration);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RunAsync_WithStringArgs_BuildsConfigAndRoutesThroughFactory()
    {
        ProcessResult result = await CliRun.RunAsync(_targetFilePath);

        await Assert.That(_factory.CreateCount).IsEqualTo(1);
        await Assert.That(_factory.LastConfiguration).IsNotNull();
        await Assert.That(_factory.LastConfiguration!.TargetFilePath).IsEqualTo(_targetFilePath);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RunBufferedAsync_WithStringArgs_BuildsConfigAndRoutesThroughFactory()
    {
        BufferedProcessResult result = await CliRun.RunBufferedAsync(_targetFilePath);

        await Assert.That(_factory.CreateCount).IsEqualTo(1);
        await Assert.That(_factory.LastConfiguration).IsNotNull();
        await Assert.That(_factory.LastConfiguration!.TargetFilePath).IsEqualTo(_targetFilePath);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RunPipedAsync_WithStringArgs_BuildsConfigAndRoutesThroughFactory()
    {
        PipedProcessResult result = await CliRun.RunPipedAsync(_targetFilePath);

        await Assert.That(_factory.CreateCount).IsEqualTo(1);
        await Assert.That(_factory.LastConfiguration).IsNotNull();
        await Assert.That(_factory.LastConfiguration!.TargetFilePath).IsEqualTo(_targetFilePath);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RunAsync_WhenProcessThrows_PropagatesException()
    {
        _factory.ThrowOnStart = new InvalidOperationException("simulated start failure");

        using ProcessConfiguration configuration =
            ProcessConfigurationFactory.Create(_targetFilePath);

        await Assert.That(async () => await CliRun.RunAsync(configuration,
                ProcessExitConfiguration.CreateGraceful()))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DefaultFactory_ReEvaluatedOnEachCall()
    {
        CountingExternalProcessFactory firstFactory = _factory;
        CountingExternalProcessFactory secondFactory = new CountingExternalProcessFactory();
        CountingExternalProcessFactory thirdFactory = new CountingExternalProcessFactory();

        using (ProcessConfiguration firstConfig =
               ProcessConfigurationFactory.Create(_targetFilePath))
        {
            await CliRun.RunAsync(firstConfig, ProcessExitConfiguration.CreateGraceful());
        }
        await Assert.That(firstFactory.CreateCount).IsEqualTo(1);

        CliRun.UseExternalProcessFactory(secondFactory);
        using (ProcessConfiguration secondConfig =
               ProcessConfigurationFactory.Create(_targetFilePath))
        {
            await CliRun.RunAsync(secondConfig, ProcessExitConfiguration.CreateGraceful());
        }
        await Assert.That(secondFactory.CreateCount).IsEqualTo(1);
        await Assert.That(firstFactory.CreateCount).IsEqualTo(1);

        CliRun.UseExternalProcessFactory(thirdFactory);
        using (ProcessConfiguration thirdConfig =
               ProcessConfigurationFactory.Create(_targetFilePath))
        {
            await CliRun.RunAsync(thirdConfig, ProcessExitConfiguration.CreateGraceful());
        }
        await Assert.That(thirdFactory.CreateCount).IsEqualTo(1);
        await Assert.That(secondFactory.CreateCount).IsEqualTo(1);
        await Assert.That(firstFactory.CreateCount).IsEqualTo(1);

        secondFactory.Dispose();
        thirdFactory.Dispose();
    }

    [Test]
    public async Task FireAndForget_WithConfig_RoutesThroughFactory()
    {
        using ProcessConfiguration configuration =
            ProcessConfigurationFactory.Create(_targetFilePath);

        CliRun.FireAndForget(configuration);

        await Assert.That(_factory.CreateCount).IsEqualTo(1);
        await Assert.That(_factory.LastConfiguration).IsSameReferenceAs(configuration);
    }

    [Test]
    public async Task FireAndForget_WithStringArgs_BuildsConfigAndRoutesThroughFactory()
    {
        CliRun.FireAndForget(_targetFilePath);

        await Assert.That(_factory.CreateCount).IsEqualTo(1);
        await Assert.That(_factory.LastConfiguration).IsNotNull();
        await Assert.That(_factory.LastConfiguration!.TargetFilePath).IsEqualTo(_targetFilePath);
    }

    [Test]
    public async Task FireAndForget_DisposesProcessAfterStart()
    {
        var disposalFactory = new DisposalTrackingFactory();

        try
        {
            CliRun.UseExternalProcessFactory(disposalFactory);

            using ProcessConfiguration configuration =
                ProcessConfigurationFactory.Create(_targetFilePath);

            CliRun.FireAndForget(configuration);

            await Assert.That(disposalFactory.WasDisposed).IsTrue();
        }
        finally
        {
            CliRun.UseExternalProcessFactory(_factory);
        }
    }

    [Test]
    public async Task FireAndForget_DisposesProcessOnStartFailure()
    {
        var disposalFactory = new DisposalTrackingFactory
        {
            ThrowOnStart = new InvalidOperationException("simulated start failure")
        };

        try
        {
            CliRun.UseExternalProcessFactory(disposalFactory);

            using ProcessConfiguration configuration =
                ProcessConfigurationFactory.Create(_targetFilePath);

            await Assert.That(() => CliRun.FireAndForget(configuration))
                .Throws<InvalidOperationException>();

            await Assert.That(disposalFactory.WasDisposed).IsTrue();
        }
        finally
        {
            CliRun.UseExternalProcessFactory(_factory);
        }
    }
}

/// <summary>
/// Records how many times <see cref="CreateExternalProcess(ProcessConfiguration, ProcessExitConfiguration)"/>
/// was called and the last <see cref="ProcessConfiguration"/> it received. Returns a stub
/// <see cref="IExternalProcess"/> so tests can exercise <see cref="CliRun"/>'s public
/// surface without actually starting a process.
/// </summary>
internal sealed class CountingExternalProcessFactory : IExternalProcessFactory, IDisposable
{
    private int _createCount;

    public int CreateCount => _createCount;

    public ProcessConfiguration? LastConfiguration { get; private set; }

    public ProcessExitConfiguration? LastExitConfiguration { get; private set; }

    /// <summary>
    /// When set, the next stub process will throw this exception from
    /// <see cref="StubExternalProcess.StartAsync(System.Threading.CancellationToken)"/>.
    /// Used by the throw-propagation test. Cleared after firing once.
    /// </summary>
    public Exception? ThrowOnStart { get; set; }

    public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration)
    {
        _createCount++;
        LastConfiguration = configuration;
        return new StubExternalProcess(configuration, ThrowOnStart);
    }

    public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration,
        ProcessExitConfiguration exitConfiguration)
    {
        _createCount++;
        LastConfiguration = configuration;
        LastExitConfiguration = exitConfiguration;
        return new StubExternalProcess(configuration, exitConfiguration, ThrowOnStart);
    }

    public void Reset()
    {
        _createCount = 0;
        LastConfiguration = null;
        LastExitConfiguration = null;
        ThrowOnStart = null;
    }

    public void Dispose()
    {
        Reset();
    }

    /// <summary>
    /// Stub <see cref="IExternalProcess"/> used by the test factory. Records that
    /// <see cref="StartAsync(System.Threading.CancellationToken)"/> was called and
    /// returns sentinel result objects for the capture methods.
    /// </summary>
    internal sealed class StubExternalProcess : IExternalProcess
    {
        private readonly Exception? _throwOnStart;
        private readonly Action? _onDisposed;
        private bool _throwOnStartConsumed;

        public StubExternalProcess(ProcessConfiguration configuration, Exception? throwOnStart,
            Action? onDisposed = null)
            : this(configuration, ProcessExitConfiguration.CreateGraceful(), throwOnStart, onDisposed)
        {
        }

        public StubExternalProcess(ProcessConfiguration configuration,
            ProcessExitConfiguration exitConfiguration, Exception? throwOnStart,
            Action? onDisposed = null)
        {
            Configuration = configuration;
            ExitConfiguration = exitConfiguration;
            _throwOnStart = throwOnStart;
            _onDisposed = onDisposed;
        }

        public ProcessConfiguration Configuration { get; set; }

        public ProcessExitConfiguration ExitConfiguration { get; set; }

        public bool HasExited => HasStarted;

        public bool HasStarted { get; private set; }

        public bool IsDisposed { get; internal set; }

        public event EventHandler? Started;

        public event EventHandler? Exited;

        public int Start()
        {
            if (_throwOnStart is not null && !_throwOnStartConsumed)
            {
                _throwOnStartConsumed = true;
                throw _throwOnStart;
            }

            HasStarted = true;
            Started?.Invoke(this, EventArgs.Empty);
            return 0;
        }

        public Task StartAsync(CancellationToken cancellationToken)
            => StartAsync(Configuration, cancellationToken);

        public Task StartAsync(ProcessConfiguration configuration, CancellationToken cancellationToken)
        {
                cancellationToken.ThrowIfCancellationRequested();

                if (_throwOnStart is not null && !_throwOnStartConsumed)
                {
                    _throwOnStartConsumed = true;
                    throw _throwOnStart;
                }

                HasStarted = true;
                Started?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            }

        public Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new ProcessResult(
                Configuration.TargetFilePath, exitCode: 0, processId: 0,
                startTime: now, exitTime: now));
        }

        public Task<BufferedProcessResult> CaptureBufferedResultAsync(CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new BufferedProcessResult(
                Configuration.TargetFilePath, exitCode: 0, processId: 0,
                standardOutput: string.Empty, standardError: string.Empty,
                startTime: now, exitTime: now));
        }

        public Task<PipedProcessResult> CapturePipedResultAsync(CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new PipedProcessResult(
                Configuration.TargetFilePath, exitCode: 0, processId: 0,
                startTime: now, exitTime: now,
                standardOutput: Stream.Null, standardError: Stream.Null));
        }

        public Task Kill() => Task.CompletedTask;

        public void Dispose()
        {
            IsDisposed = true;
            _onDisposed?.Invoke();
            Exited?.Invoke(this, EventArgs.Empty);
        }
    }
}

/// <summary>
/// Factory that tracks whether the created <see cref="IExternalProcess"/> was disposed.
/// Used by the FireAndForget disposal tests.
/// </summary>
internal sealed class DisposalTrackingFactory : IExternalProcessFactory
{
    private readonly List<IExternalProcess> _created = new();

    public bool WasDisposed { get; private set; }

    public Exception? ThrowOnStart { get; set; }

    public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration)
        => CreateExternalProcess(configuration, ProcessExitConfiguration.CreateGraceful());

    public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration,
        ProcessExitConfiguration exitConfiguration)
    {
        var stub = new CountingExternalProcessFactory.StubExternalProcess(
            configuration, exitConfiguration, ThrowOnStart,
            onDisposed: () => WasDisposed = true);
        _created.Add(stub);
        return stub;
    }
}

/// <summary>
/// Test <see cref="IFilePathResolver"/> that records each call to
/// <see cref="ResolveFilePath"/> and <see cref="TryResolveFilePath"/>. The resolver
/// does not touch the file system; the target path is always returned unchanged
/// (with no existence check) so the test path is fully self-contained.
/// </summary>
internal sealed class CapturingFilePathResolver : IFilePathResolver, IDisposable
{
    public int ResolveCount { get; private set; }

    public int TryResolveCount { get; private set; }

    public FileInfo ResolveFilePath(string filePathToResolve)
    {
        ResolveCount++;
        return new FileInfo(filePathToResolve);
    }

    public bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath)
    {
        TryResolveCount++;
        resolvedFilePath = new FileInfo(filePathToResolve);
        return true;
    }

    public void Reset()
    {
        ResolveCount = 0;
        TryResolveCount = 0;
    }

    public void Dispose()
    {
        Reset();
    }
}

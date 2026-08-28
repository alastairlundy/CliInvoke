/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.InteropServices;
using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;

namespace CliInvoke.Tests;

/// <summary>
/// Records how many times <see cref="CreateExternalProcess(ProcessConfiguration, ProcessExitConfiguration)"/>
/// was called and the last <see cref="ProcessConfiguration"/> it received. Returns a stub
/// <see cref="IExternalProcess"/> so tests can exercise the pipeline without actually starting a process.
/// </summary>
internal sealed class CountingExternalProcessFactory : IExternalProcessFactory, IDisposable
{
    private int _createCount;

    /// <summary>
    ///     When set, the next stub process will report this value for its
    ///     <c>Canceled</c> state (used by TK006 tests).
    /// </summary>
    public bool DefaultCanceled { get; set; }

    /// <summary>
    ///     When set, the next stub process will report this value for
    ///     <see cref="ProcessResult.Signal"/>-derived results (used by TK006 tests).
    /// </summary>
    public PosixSignal? DefaultSignal { get; set; }

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
        return new StubExternalProcess(configuration, ThrowOnStart)
        {
            Canceled = DefaultCanceled,
            Signal = DefaultSignal
        };
    }

    public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration,
        ProcessExitConfiguration exitConfiguration)
    {
        _createCount++;
        LastConfiguration = configuration;
        LastExitConfiguration = exitConfiguration;
        return new StubExternalProcess(configuration, exitConfiguration, ThrowOnStart)
        {
            Canceled = DefaultCanceled,
            Signal = DefaultSignal
        };
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

        public ProcessConfiguration Configuration { get; init; }

        public ProcessExitConfiguration ExitConfiguration { get; }

        public bool HasExited => HasStarted;

        public bool HasStarted { get; private set; }

        public bool Canceled { get; set; }

        public PosixSignal? Signal { get; set; }

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
                startTime: now, exitTime: now,
                canceled: Canceled, signal: Signal));
        }

        public Task<BufferedProcessResult> CaptureBufferedResultAsync(
            CancellationToken cancellationToken,
            long? maxStandardOutputBytes = null,
            long? maxStandardErrorBytes = null)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new BufferedProcessResult(
                Configuration.TargetFilePath, exitCode: 0, processId: 0,
                standardOutput: string.Empty, standardError: string.Empty,
                startTime: now, exitTime: now,
                canceled: Canceled, signal: Signal));
        }

        public Task<PipedProcessResult> CapturePipedResultAsync(CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new PipedProcessResult(
                Configuration.TargetFilePath, exitCode: 0, processId: 0,
                startTime: now, exitTime: now,
                standardOutput: Stream.Null, standardError: Stream.Null,
                canceled: Canceled, signal: Signal));
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

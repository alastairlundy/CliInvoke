/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;

namespace CliInvoke.Tests.Helpers;

/// <summary>
/// Records how many times <see cref="CreateExternalProcess(ProcessConfiguration, ProcessExitConfiguration)"/>
/// was called and the last <see cref="ProcessConfiguration"/> it received. Returns a stub
/// <see cref="IExternalProcess"/> so tests can exercise the pipeline without actually starting a process.
/// The stubs share <see cref="StdinPiping"/>, a piping seam that records the modelled
/// copy/finally-close stdin contract.
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

    /// <summary>
    ///     The stdin-piping event log shared with every stub process this factory creates.
    ///     The stub's modelled piping records copy start, copy completion/fault, and the
    ///     child-side write-end close here, so tests can assert close-on-every-exit-path
    ///     and copy-before-close ordering deterministically.
    /// </summary>
    public StdinPipingLog StdinPiping { get; } = new();

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
            Signal = DefaultSignal,
            Piping = StdinPiping,
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
            Signal = DefaultSignal,
            Piping = StdinPiping,
        };
    }

    public void Reset()
    {
        _createCount = 0;
        LastConfiguration = null;
        LastExitConfiguration = null;
        ThrowOnStart = null;
        StdinPiping.Clear();
    }

    public void Dispose()
    {
        Reset();
    }

    /// <summary>
    /// Stub <see cref="IExternalProcess"/> used by the test factory. Records that
    /// <see cref="StartAsync(System.Threading.CancellationToken)"/> was called, models the
    /// stdin copy + finally-close piping contract against <see cref="Piping"/> on the
    /// one-call paths (mirroring <c>ExternalProcess</c>'s gates and join fault rules), and
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

        /// <summary>
        ///     The stdin-piping log targeted by this stub's modelled piping. The factory
        ///     overrides it with a shared log so tests can read the events after the
        ///     pipeline has disposed the stub.
        /// </summary>
        public StdinPipingLog Piping { get; init; } = new();

        public bool HasExited => HasStarted;

        public bool HasStarted { get; private set; }

        public bool Canceled { get; set; }

        public PosixSignal? Signal { get; set; }

        public bool IsDisposed { get; internal set; }

        public event EventHandler? Started;

        public event EventHandler? Exited;

        public int Start()
        {
            BeginStart(CancellationToken.None);
            return 0;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            BeginStart(cancellationToken);

            // Gate mirrors ExternalProcess.StartAsync(ct): pipe only when a stdin source is
            // configured and redirection is enabled.
            if (Configuration.StandardInput is not null && Configuration.RedirectStandardInput)
            {
                try
                {
                    await PipeStandardInputAsync(Configuration.StandardInput.BaseStream,
                        cancellationToken).ConfigureAwait(true);
                }
                catch (Exception) when (Canceled)
                {
                    // Join (mirror of ExternalProcess.StartAsync): a copy fault
                    // that is an artifact of this stub's own kill machinery is absorbed here;
                    // source read errors and cancellation (Canceled false) propagate.
                }
            }
        }

        public async Task StartAsync(ProcessConfiguration configuration,
            CancellationToken cancellationToken)
        {
            BeginStart(cancellationToken);

            // The configuration overload pipes whenever a stdin source is supplied and has
            // no join of its own - every fault propagates, mirroring ExternalProcess.
            if (configuration.StandardInput is not null)
                await PipeStandardInputAsync(configuration.StandardInput.BaseStream,
                    cancellationToken).ConfigureAwait(true);
        }

        private void BeginStart(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_throwOnStart is not null && !_throwOnStartConsumed)
            {
                _throwOnStartConsumed = true;
                throw _throwOnStart;
            }

            HasStarted = true;
            Started?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Models the wrapper's copy + finally-close stdin piping contract: the
        ///     source is copied into the stub child's stdin write end, and the
        ///     write end is closed in a <c>finally</c> on every exit path - successful copy,
        ///     cancellation, source error, or kill-induced fault. The caller's source stream
        ///     is never closed or disposed here.
        /// </summary>
        /// <param name="source">The caller-owned stdin source stream.</param>
        /// <param name="cancellationToken">Token observed by the copy.</param>
        /// <returns>A task that completes once the copy and close have run.</returns>
        private async Task PipeStandardInputAsync(Stream source,
            CancellationToken cancellationToken)
        {
            Piping.Record(StdinPipingEvent.CopyStarted);

            try
            {
                await source.CopyToAsync(Piping.WriteEnd, cancellationToken).ConfigureAwait(true);
                Piping.Record(StdinPipingEvent.CopyCompleted);
            }
            catch
            {
                Piping.Record(StdinPipingEvent.CopyFaulted);
                throw;
            }
            finally
            {
                Piping.WriteEnd.Close();
            }
        }

        public Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            return Task.FromResult(new ProcessResult(
                Configuration.TargetFilePath, exitCode: 0, processId: 0,
                startTime: now, exitTime: now,
                canceled: Canceled, signal: Signal));
        }

        /// <summary>
        ///     Models the buffered capture join: the stdin copy runs
        ///     alongside the wait and drain, and the join absorbs a copy fault only when it
        ///     is an artifact of this stub's own kill machinery; source read errors and any
        ///     wait/drain fault propagate.
        /// </summary>
        /// <param name="cancellationToken">Token observed by the copy and the wait.</param>
        /// <param name="maxStandardOutputBytes">Unused by the stub; matches the interface.</param>
        /// <param name="maxStandardErrorBytes">Unused by the stub; matches the interface.</param>
        /// <returns>A task producing the sentinel buffered result.</returns>
        public async Task<BufferedProcessResult> CaptureBufferedResultAsync(
            CancellationToken cancellationToken,
            long? maxStandardOutputBytes = null,
            long? maxStandardErrorBytes = null)
        {
            Task standardInputCopy = Configuration.StandardInput is not null
                                     && Configuration.RedirectStandardInput
                ? PipeStandardInputAsync(Configuration.StandardInput.BaseStream, cancellationToken)
                : Task.CompletedTask;

            // The stub's wait and drain complete immediately and never fault.
            Task waitTask = WaitForExitOrTimeoutAsync(cancellationToken);
            Task drainTask = Task.CompletedTask;

            try
            {
                await Task.WhenAll(standardInputCopy, waitTask, drainTask).ConfigureAwait(true);
            }
            catch (Exception) when (standardInputCopy.IsFaulted
                                    && !waitTask.IsFaulted
                                    && !drainTask.IsFaulted
                                    && Canceled)
            {
                // Kill artifact absorbed at the join; a genuine source-stream
                // read error leaves the kill machinery un-run (Canceled false) and propagates.
            }

            DateTime now = DateTime.UtcNow;
            return new BufferedProcessResult(
                Configuration.TargetFilePath, exitCode: 0, processId: 0,
                standardOutput: string.Empty, standardError: string.Empty,
                startTime: now, exitTime: now,
                canceled: Canceled, signal: Signal, wasTruncated: false);
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

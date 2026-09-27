/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;

using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;

namespace CliInvoke.Tests;

/// <summary>
/// Records how many times <see cref="CreateExternalProcess(ProcessConfiguration, ProcessExitConfiguration)"/>
/// was called and the last <see cref="ProcessConfiguration"/> it received. Returns a stub
/// <see cref="IExternalProcess"/> so tests can exercise the pipeline without actually starting a process.
/// The stubs share <see cref="StdinPiping"/>, a piping seam that records the modelled
/// copy/finally-close stdin contract (ledger T008).
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
    ///     and copy-before-close ordering deterministically (ledger T008).
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
            Piping = StdinPiping
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
            Piping = StdinPiping
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
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception) when (Canceled)
                {
                    // Join (mirror of ExternalProcess.StartAsync, ledger T006): a copy fault
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
            // no join of its own - every fault propagates, mirroring ExternalProcess (T006).
            if (configuration.StandardInput is not null)
                await PipeStandardInputAsync(configuration.StandardInput.BaseStream,
                    cancellationToken).ConfigureAwait(false);
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
        ///     Models the wrapper's copy + finally-close stdin piping contract (ledger D003,
        ///     T005): the source is copied into the stub child's stdin write end, and the
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
                await source.CopyToAsync(Piping.WriteEnd, cancellationToken).ConfigureAwait(false);
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
        ///     Models the buffered capture join (ledger T003, T006): the stdin copy runs
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
                await Task.WhenAll(standardInputCopy, waitTask, drainTask).ConfigureAwait(false);
            }
            catch (Exception) when (standardInputCopy.IsFaulted
                                    && !waitTask.IsFaulted
                                    && !drainTask.IsFaulted
                                    && Canceled)
            {
                // Kill artifact absorbed at the join (ledger T006); a genuine source-stream
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

/// <summary>
///     Events recorded by the stub stdin-piping seam, in the order they occurred.
/// </summary>
internal enum StdinPipingEvent
{
    CopyStarted,
    CopyCompleted,
    CopyFaulted,
    WriteEndClosed
}

/// <summary>
///     Deterministic record of a stub child's stdin piping: the ordered events plus the
///     child-side write end the copy targets. This is the piping seam <see cref="IExternalProcess"/>
///     cannot express (ledger T008).
/// </summary>
internal sealed class StdinPipingLog
{
    private readonly List<StdinPipingEvent> _events = [];

    internal StdinPipingLog()
    {
        WriteEnd = new RecordingStdinWriteEnd(this);
    }

    /// <summary>
    ///     The recorded events in occurrence order: copy start, then copy completion or
    ///     fault, then the write-end close.
    /// </summary>
    public IReadOnlyList<StdinPipingEvent> Events => _events;

    /// <summary>
    ///     The stub child's stdin write end that the copy targets and the finally closes.
    /// </summary>
    public RecordingStdinWriteEnd WriteEnd { get; }

    /// <summary>
    ///     When armed, writes to the write end fail with an <see cref="IOException"/> -
    ///     the broken pipe a library kill leaves behind mid-copy (ledger T006).
    /// </summary>
    public bool ArmBrokenPipe { get; set; }

    internal void Record(StdinPipingEvent pipingEvent)
    {
        _events.Add(pipingEvent);
    }

    /// <summary>
    ///     Clears the recorded events and disarms the injected write fault.
    /// </summary>
    public void Clear()
    {
        _events.Clear();
        ArmBrokenPipe = false;
    }
}

/// <summary>
///     Stand-in for the child's stdin pipe write end. Records its own close (the
///     end-of-file delivery the matrix asserts) and can be armed to fail writes like a
///     pipe torn down by a library kill.
/// </summary>
internal sealed class RecordingStdinWriteEnd : Stream
{
    private readonly StdinPipingLog _log;
    private bool _closed;

    internal RecordingStdinWriteEnd(StdinPipingLog log)
    {
        _log = log;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => !_closed;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (_closed)
            throw new ObjectDisposedException(nameof(RecordingStdinWriteEnd));

        if (_log.ArmBrokenPipe)
            throw new IOException("Stub stdin write fault: the child was killed mid-copy (broken pipe).");
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count,
        CancellationToken cancellationToken)
    {
        Write(buffer.AsSpan(offset, count));
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_closed)
        {
            _closed = true;
            _log.Record(StdinPipingEvent.WriteEndClosed);
        }

        base.Dispose(disposing);
    }
}

/// <summary>
///     Caller-owned stdin source stand-in for the piping matrix: yields a fixed payload,
///     optionally cancels the copy token after the first read (<see cref="OnFirstRead"/>)
///     or throws a prescribed fault from the second read onward (<see cref="FaultAfterFirstRead"/>),
///     so cancellation and source-error exits are injected deterministically mid-copy.
///     Tests assert <see cref="CanRead"/> stays true after every exit path - the library
///     must never close it (ledger D003).
/// </summary>
internal sealed class InjectableStdinSourceStream : Stream
{
    private readonly byte[] _payload;
    private int _position;
    private int _readCount;
    private bool _disposed;

    public InjectableStdinSourceStream(string payload)
    {
        _payload = Encoding.UTF8.GetBytes(payload);
    }

    /// <summary>
    ///     Invoked once before the first read returns its bytes - cancel the copy token
    ///     here to land cancellation mid-copy, after data has flowed.
    /// </summary>
    public Action? OnFirstRead { get; init; }

    /// <summary>
    ///     When set, thrown by the second read - a genuine source-stream read error after
    ///     the first bytes have flowed.
    /// </summary>
    public Exception? FaultAfterFirstRead { get; init; }

    public override bool CanRead => !_disposed;

    // StreamWriter's constructor rejects non-writable streams, and the configuration
    // type only accepts a StreamWriter as its stdin source. The library only ever
    // reads from this stream during piping, so writes are never issued.
    public override bool CanWrite => true;

    public override bool CanSeek => false;

    public override long Length => _payload.Length;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
        => ReadCore(buffer);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ReadCore(buffer.Span));
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read(buffer, offset, count));
    }

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        base.Dispose(disposing);
    }

    private int ReadCore(Span<byte> buffer)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InjectableStdinSourceStream));

        if (_readCount == 0)
            OnFirstRead?.Invoke();

        if (_readCount >= 1 && FaultAfterFirstRead is not null)
            throw FaultAfterFirstRead;

        int bytesToCopy = Math.Min(buffer.Length, _payload.Length - _position);
        if (bytesToCopy == 0)
            return 0;

        _payload.AsSpan(_position, bytesToCopy).CopyTo(buffer);
        _position += bytesToCopy;
        _readCount++;
        return bytesToCopy;
    }
}

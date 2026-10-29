/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;

namespace CliInvoke.Tests.Helpers;

/// <summary>
///     Caller-owned stdin source stand-in for the piping matrix: yields a fixed payload,
///     optionally cancels the copy token after the first read (<see cref="OnFirstRead"/>)
///     or throws a prescribed fault from the second read onward (<see cref="FaultAfterFirstRead"/>),
///     so cancellation and source-error exits are injected deterministically mid-copy.
///     Tests assert <see cref="CanRead"/> stays true after every exit path - the library
///     must never close it.
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

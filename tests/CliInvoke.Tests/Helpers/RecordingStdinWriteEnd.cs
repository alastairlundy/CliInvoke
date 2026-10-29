/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Tests.Helpers;

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

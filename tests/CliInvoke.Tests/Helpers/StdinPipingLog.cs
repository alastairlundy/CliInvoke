/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Processes;

namespace CliInvoke.Tests.Helpers;

/// <summary>
///     Deterministic record of a stub child's stdin piping: the ordered events plus the
///     child-side write end the copy targets. This is the piping seam <see cref="IExternalProcess"/>
///     cannot express.
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
    ///     the broken pipe a library kill leaves behind mid-copy.
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

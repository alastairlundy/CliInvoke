/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core;

namespace CliInvoke.Core.Exceptions;

/// <summary>
///     An exception thrown when a process execution exceeds its configured timeout
///     threshold. Derives from <see cref="TimeoutException"/> so existing
///     <c>catch (TimeoutException)</c> handlers continue to work, while exposing
///     structured child-state diagnostics via <see cref="ProcessId"/> and
///     <see cref="ChildState"/>.
/// </summary>
public sealed class CliInvokeTimeoutException : TimeoutException
{
    /// <summary>
    ///     Initialises a new instance with a message and child-state diagnostics.
    /// </summary>
    /// <param name="message">A description of the timeout.</param>
    /// <param name="processId">The OS-assigned identifier of the child process.</param>
    /// <param name="childState">
    ///     A point-in-time snapshot of the child process's state when the timeout fired.
    /// </param>
    /// <param name="elapsed">How long the process had been running when the timeout fired.</param>
    public CliInvokeTimeoutException(string message, int processId, ProcessDiagnostic? childState, TimeSpan elapsed)
        : base(message)
    {
        ProcessId = processId;
        ChildState = childState;
        Elapsed = elapsed;
    }

    /// <summary>
    ///     Initialises a new instance with a message, an inner exception, and child-state diagnostics.
    /// </summary>
    /// <param name="message">A description of the timeout.</param>
    /// <param name="innerException">The exception that caused this timeout.</param>
    /// <param name="processId">The OS-assigned identifier of the child process.</param>
    /// <param name="childState">
    ///     A point-in-time snapshot of the child process's state when the timeout fired.
    /// </param>
    /// <param name="elapsed">How long the process had been running when the timeout fired.</param>
    public CliInvokeTimeoutException(string message, Exception? innerException, int processId,
        ProcessDiagnostic? childState, TimeSpan elapsed)
        : base(message, innerException)
    {
        ProcessId = processId;
        ChildState = childState;
        Elapsed = elapsed;
    }

    /// <summary>
    ///     The OS-assigned identifier of the child process that timed out.
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    ///     A point-in-time snapshot of the child process's state when the timeout fired,
    ///     or <c>null</c> if the state could not be captured.
    /// </summary>
    public ProcessDiagnostic? ChildState { get; }

    /// <summary>
    ///     How long the process had been running when the timeout fired.
    /// </summary>
    public TimeSpan Elapsed { get; }
}
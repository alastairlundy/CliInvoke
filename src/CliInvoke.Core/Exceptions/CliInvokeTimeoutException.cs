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
///     threshold. A timeout is implemented as cancellation of the wait — the kill timer
///     cancels it — so this type derives from <see cref="OperationCanceledException"/>:
///     timer-driven timeouts stay catchable by the existing
///     <c>catch (OperationCanceledException)</c> handlers around
///     <c>CaptureBufferedResultAsync</c> and the buffered run APIs, matching the <c>CancellationThrowsException</c>
///     contract in the troubleshooting guide, while exposing structured child-state
///     diagnostics via <see cref="ProcessId"/>, <see cref="ChildState"/>, and
///     <see cref="Elapsed"/>. Catch the concrete <c>CliInvokeTimeoutException</c> to
///     distinguish a timer-driven timeout from a caller-requested cancellation (which
///     surfaces as <see cref="CliInvokeCanceledException"/>).
/// </summary>
/// <remarks>
///     Deliberately not derived from <see cref="TimeoutException"/>: C# single
///     inheritance cannot make one exception type match both
///     <c>catch (OperationCanceledException)</c> and <c>catch (TimeoutException)</c>,
///     and the cancellation contract is the one CliInvoke has always shipped —
///     timeouts surfaced as a bare <see cref="OperationCanceledException"/> before
///     this type existed.
/// </remarks>
public sealed class CliInvokeTimeoutException : OperationCanceledException
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
    ///     Initialises a new instance with a message, an inner exception, a cancellation token,
    ///     and child-state diagnostics.
    /// </summary>
    /// <param name="message">A description of the timeout.</param>
    /// <param name="innerException">The exception that caused this timeout.</param>
    /// <param name="cancellationToken">
    ///     The cancellation token that fired when the wait timed out; preserved by the base
    ///     <see cref="OperationCanceledException"/> so callers can match the exception against
    ///     the token they observe.
    /// </param>
    /// <param name="processId">The OS-assigned identifier of the child process.</param>
    /// <param name="childState">
    ///     A point-in-time snapshot of the child process's state when the timeout fired.
    /// </param>
    /// <param name="elapsed">How long the process had been running when the timeout fired.</param>
    public CliInvokeTimeoutException(string message, Exception? innerException, CancellationToken cancellationToken,
        int processId, ProcessDiagnostic? childState, TimeSpan elapsed)
        : base(message, innerException, cancellationToken)
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
/*
    CliInvoke.Core

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core.Processes;

/// <summary>
/// Defines a contract for an external process that can be suspended and resumed.
/// </summary>
public interface ISuspendableExternalProcess : IExternalProcess
{
    /// <summary>
    /// Suspends the execution of the external process.
    /// </summary>
    /// <remarks>
    /// This method allows pausing the execution of an external process that implements
    /// the <see cref="ISuspendableExternalProcess" /> interface. Suspending a process may
    /// release resources held by the process or pause its operation without terminating it.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process is not in a state that allows suspension, such as when it has not started
    /// or has already exited.
    /// </exception>
    void Suspend();

    /// <summary>
    /// Resumes the execution of the external process.
    /// </summary>
    /// <remarks>
    /// This method resumes an external process that was previously suspended using the
    /// <see cref="ISuspendableExternalProcess.Suspend" /> method. Resuming a suspended process
    /// allows it to continue its operations from the point where it was paused.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process cannot be resumed, such as if it has not been started, is not currently suspended,
    /// or has already exited.
    /// </exception>
    void Resume();
}
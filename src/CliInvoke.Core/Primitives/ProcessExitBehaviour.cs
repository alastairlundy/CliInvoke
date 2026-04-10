/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;

namespace CliInvoke.Core;

/// <summary>
///     Specifies the mode for handling process cancellation.
/// </summary>
[DefaultValue(GracefulExit)]
public enum ProcessExitBehaviour
{
    /// <summary>
    ///     The Process will run until it initiates exit.
    /// </summary>
    /// <remarks>
    ///     If the process gets stuck or hangs, the process will not be exited unless cancellation is
    ///     requested via a cancellation token.
    /// </remarks>
    WaitForExit = 0,
    /// <summary>
    ///     Gracefully cancels the Process upon request using SIGTERM/SIGINT Signals, or a
    ///     <see cref="CancellationTokenSource" /> if that fails.
    /// </summary>
    GracefulExit = 1,
    /// <summary>
    ///     Forcefully terminates the Process upon request along with all child processes.
    /// </summary>
    ForcefulExit = 2
}
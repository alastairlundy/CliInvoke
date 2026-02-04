/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
/// Specifies the mode for handling process cancellation.
/// </summary>
public enum ProcessCancellationMode
{
    /// <summary>
    /// Forcefully terminates the Process along with all child processes.
    /// </summary>
    Forceful,
    /// <summary>
    /// Gracefully cancels the Process using SIGTERM/SIGINT Signals, or a <see cref="CancellationTokenSource"/> if that fails.
    /// </summary>
    Graceful,
    /// <summary>
    /// No cancellation is desired, Process will run until exit.
    /// </summary>
    None,
}
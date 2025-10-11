/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading;

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// 
/// </summary>
public enum ProcessCancellationMode
{
    /// <summary>
    /// Forcefully terminates the Process along with all child processes.
    /// </summary>
    Forceful,
    /// <summary>
    /// Gracefully cancels the Process using a new <see cref="CancellationTokenSource"/>. 
    /// </summary>
    Graceful,
    /// <summary>
    /// No cancellation is desired, Process will run until exit. 
    /// </summary>
    None,
}
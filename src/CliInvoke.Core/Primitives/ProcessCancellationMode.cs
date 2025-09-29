/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace AlastairLundy.CliInvoke.Core.Primitives;

/// <summary>
/// 
/// </summary>
public enum ProcessCancellationMode
{
    /// <summary>
    /// 
    /// </summary>
    Forceful,
    /// <summary>
    /// Graceful Cancellation is attempted, but the process's exit upon cancellation is not guaranteed.
    /// </summary>
    Graceful,
    /// <summary>
    /// No cancellation is desired, Process will run until exit. 
    /// </summary>
    None,
}
/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
///     The shape of invocation used to run a process.
/// </summary>
public enum InvocationMode
{
    /// <summary>
    ///     Runs the process and returns its result directly from the middleware pipeline.
    /// </summary>
    Raw,
    /// <summary>
    ///     Runs the process and buffers its output into a <c>BufferedProcessResult</c>.
    /// </summary>
    Buffered,
    /// <summary>
    ///     Starts the process without waiting for it to exit or capturing a result.
    /// </summary>
    FireAndForget
}

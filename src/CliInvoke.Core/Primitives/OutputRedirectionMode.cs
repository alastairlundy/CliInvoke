/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
/// Defines the modes for redirecting process output.
/// </summary>
public enum OutputRedirectionMode
{
    /// <summary>
    /// 
    /// </summary>
    None = 0,
    /// <summary>
    /// 
    /// </summary>
    Buffer,
    /// <summary>
    /// 
    /// </summary>
    Pipe,
}
/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Internal;

/// <summary>
///     Identifies the target shell for escaping and command composition.
/// </summary>
internal enum ShellKind
{
    /// <summary>
    ///     PowerShell (<c>pwsh</c> / <c>powershell</c>) shell.
    /// </summary>
    PowerShell,

    /// <summary>
    ///     Windows <c>cmd.exe</c> shell.
    /// </summary>
    Cmd,

    /// <summary>
    ///     A POSIX-compatible shell (<c>sh</c>, <c>bash</c>, etc.).
    /// </summary>
    Posix
}

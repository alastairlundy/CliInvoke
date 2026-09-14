/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Internal;

/// <summary>
///     Determines how the composed inner command is delivered to the target process.
/// </summary>
internal enum ShellDelivery
{
    /// <summary>
    ///     Deliver via <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList" />.
    ///     Each entry is passed as a discrete token; .NET applies CommandLineToArgvW quoting.
    /// </summary>
    ArgumentList,

    /// <summary>
    ///     Deliver via the single <see cref="System.Diagnostics.ProcessStartInfo.Arguments" /> string.
    ///     The raw command-line text is passed verbatim to the child process.
    /// </summary>
    Arguments
}

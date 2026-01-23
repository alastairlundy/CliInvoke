/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Core;

/// <summary>
/// Provides functionality to detect the default shell on various operating systems.
/// </summary>
public interface IShellDetector
{
    /// <summary>
    /// Resolves the default shell on various operating systems asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous resolution of the default shell,
    /// returning a <see cref="ShellInformation"/> object containing details about the detected shell.</returns>
    Task<ShellInformation> ResolveDefaultShellAsync(CancellationToken cancellationToken);
}
/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Configuration options for <see cref="PowerShellMiddleware"/>.
/// </summary>
/// <remarks>
///     Register an instance of this type in the dependency injection container
///     to customise PowerShell wrapping behaviour. When no instance is registered,
///     <see cref="Default"/> is used.
/// </remarks>
public sealed class PowerShellMiddlewareOptions
{
    /// <summary>
    ///     Gets the default options instance with both flags set to <c>false</c>.
    /// </summary>
    public static PowerShellMiddlewareOptions Default { get; } = new();

    /// <summary>
    ///     Gets or sets a value indicating whether PowerShell should create a new window when launched.
    /// </summary>
    public bool WindowCreation { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to use shell execution semantics for the wrapped process.
    /// </summary>
    public bool UseShellExecution { get; set; }
}

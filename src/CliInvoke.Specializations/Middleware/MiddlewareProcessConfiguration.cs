/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     A small <see cref="ProcessConfiguration"/> derived class used by platform middleware
///     to construct a configuration with explicit <c>windowCreation</c> and
///     <c>useShellExecution</c> values, which are not exposed by the public
///     <see cref="ProcessConfiguration"/> constructors.
/// </summary>
internal sealed class MiddlewareProcessConfiguration : ProcessConfiguration
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="MiddlewareProcessConfiguration"/> class.
    /// </summary>
    /// <param name="targetFilePath">The path to the executable to run.</param>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <param name="windowCreation">Whether to allow the process to create a new window.</param>
    /// <param name="useShellExecution">Whether to use shell execution semantics.</param>
    public MiddlewareProcessConfiguration(
        string targetFilePath,
        string arguments,
        bool windowCreation,
        bool useShellExecution)
        : base(
            targetFilePath,
            arguments,
            redirectStandardInput: false,
            outputRedirection: true,
            windowCreation: windowCreation,
            useShellExecution: useShellExecution)
    {
    }
}
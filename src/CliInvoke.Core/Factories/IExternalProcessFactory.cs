/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Processes;

namespace CliInvoke.Core.Factories;

/// <summary>
/// Represents an interface for creating instances of the <see cref="IExternalProcess"/> interface.
/// </summary>
public interface IExternalProcessFactory
{
    /// <summary>
    /// Creates an instance of the <see cref="IExternalProcess"/> interface based on the provided process configuration.
    /// </summary>
    /// <param name="configuration">The configuration details for creating the external process.</param>
    /// <returns>An instance of <see cref="IExternalProcess"/> configured according to the specified <paramref name="configuration"/>.</returns>
    IExternalProcess CreateExternalProcess(ProcessConfiguration configuration);

    /// <summary>
    /// Creates an instance of the <see cref="IExternalProcess"/> interface based on the provided process configuration.
    /// </summary>
    /// <param name="configuration">The configuration details for creating the external process.</param>
    /// <param name="exitConfiguration">The process exit configuration details for configuring process exit behaviour.</param>
    /// <returns>An instance of <see cref="IExternalProcess"/> configured according to the specified <paramref name="configuration"/>.</returns>
    IExternalProcess CreateExternalProcess(ProcessConfiguration configuration,
        ProcessExitConfiguration exitConfiguration);
}
/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;

using AlastairLundy.CliInvoke.Core.Builders;

namespace AlastairLundy.CliInvoke.Core.Factories;

/// <summary>
/// A factory interface to enable easier <see cref="ProcessConfiguration"/> creation.
/// </summary>
public interface IProcessConfigurationFactory
{
    /// <summary>
    /// Creates a Process configuration that can be run by a <see cref="IProcessConfigurationInvoker"/> from specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <returns>The <see cref="ProcessConfiguration"/> created from the configured parameters.</returns>
    ProcessConfiguration Create(string targetFilePath, params string[] arguments);
    
    /// <summary>
    /// Creates a Process configuration that can be run by a <see cref="IProcessConfigurationInvoker"/> from specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="configureBuilder">Actions to apply to the internal <see cref="IProcessConfigurationBuilder"/> if not null.</param>
    /// <returns>The <see cref="ProcessConfiguration"/> created from the configured parameters.</returns>
    ProcessConfiguration Create(string targetFilePath, string arguments, Action<IProcessConfigurationBuilder>? configureBuilder = null);

    /// <summary>
    /// Creates a Process configuration that can be run by a <see cref="IProcessConfigurationInvoker"/> from specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="configureBuilder">Actions to apply to the internal <see cref="IProcessConfigurationBuilder"/> if not null.</param>
    /// <returns>The <see cref="ProcessConfiguration"/> created from the configured parameters.</returns>
    ProcessConfiguration Create(string targetFilePath, IEnumerable<string> arguments,
        Action<IProcessConfigurationBuilder>? configureBuilder = null);
}
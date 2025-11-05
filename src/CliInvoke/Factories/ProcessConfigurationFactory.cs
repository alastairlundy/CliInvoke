/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Factories;

namespace AlastairLundy.CliInvoke.Factories;

/// <summary>
/// A Dependency Injection supporting factory class to enable easier <see cref="ProcessConfiguration"/> creation.
/// </summary>
public class ProcessConfigurationFactory : IProcessConfigurationFactory
{
    /// <summary>
    /// Creates a Process configuration that can be run by a <see cref="IProcessInvoker"/> from specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <returns>The <see cref="ProcessConfiguration"/> created from the configured parameters.</returns>
    [Pure]
    public ProcessConfiguration Create(string targetFilePath, params string[] arguments) =>
        Create(targetFilePath, arguments, null);

    /// <summary>
    /// Creates a Process configuration that can be run by a <see cref="IProcessInvoker"/> from specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="configureBuilder">Actions to apply to the internal <see cref="IProcessConfigurationBuilder"/> if not null.</param>
    /// <returns>The <see cref="ProcessConfiguration"/> created from the configured parameters.</returns>
    [Pure]
    public ProcessConfiguration Create(
        string targetFilePath,
        string arguments,
        Action<IProcessConfigurationBuilder>? configureBuilder = null
    )
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(
            targetFilePath
        )
            .SetArguments(arguments)
            .RedirectStandardOutput(true)
            .RedirectStandardError(true)
            .ConfigureWindowCreation(false);

        if (configureBuilder is not null)
            configureBuilder.Invoke(processConfigurationBuilder);

        return processConfigurationBuilder.Build();
    }

    /// <summary>
    /// Creates a Process configuration that can be run by a <see cref="IProcessInvoker"/> from specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="configureBuilder">Actions to apply to the internal <see cref="IProcessConfigurationBuilder"/> if not null.</param>
    /// <returns>The <see cref="ProcessConfiguration"/> created from the configured parameters.</returns>
    [Pure]
    public ProcessConfiguration Create(
        string targetFilePath,
        IEnumerable<string> arguments,
        Action<IProcessConfigurationBuilder>? configureBuilder = null
    )
    {
        IArgumentsBuilder argumentsBuilder = new ArgumentsBuilder(x =>
            string.IsNullOrEmpty(x) == false
        ).AddEnumerable(arguments);

        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(
            targetFilePath
        )
            .SetArguments(argumentsBuilder.ToString())
            .RedirectStandardOutput(true)
            .RedirectStandardError(true)
            .ConfigureWindowCreation(false);

        if (configureBuilder is not null)
            configureBuilder.Invoke(processConfigurationBuilder);

        return processConfigurationBuilder.Build();
    }
}

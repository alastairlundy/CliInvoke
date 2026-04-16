/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Builders;

namespace CliInvoke;

/// <summary>
///     A Dependency Injection supporting factory class to enable easier
///     <see cref="ProcessConfiguration" /> creation.
/// </summary>
public static class ProcessConfigurationFactoryExtensions
{
    extension(ProcessConfiguration)
    {
        /// <summary>
        ///     Creates a Process configuration that can be run by a <see cref="IProcessInvoker" /> from
        ///     specified parameters.
        /// </summary>
        /// <param name="targetFilePath">The target file path of the command to be executed.</param>
        /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
        /// <param name="outputRedirection"></param>
        /// <returns>The <see cref="ProcessConfiguration" /> created from the configured parameters.</returns>
        [Pure]
        public static ProcessConfiguration Create(string targetFilePath, 
            bool outputRedirection = true, params string[] arguments) 
            => ProcessConfiguration.Create(targetFilePath, arguments, null,
                outputRedirection);

        /// <summary>
        ///     Creates a Process configuration that can be run by a <see cref="IProcessInvoker" /> from
        ///     specified parameters.
        /// </summary>
        /// <param name="targetFilePath">The target file path of the command to be executed.</param>
        /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
        /// <param name="workingDirectory"></param>
        /// <param name="outputRedirection"></param>
        /// <param name="configureBuilder">
        ///     Actions to apply to the internal
        ///     <see cref="IProcessConfigurationBuilder" /> if not null.
        /// </param>
        /// <param name="enableWindowCreation"></param>
        /// <returns>The <see cref="ProcessConfiguration" /> created from the configured parameters.</returns>
        [Pure]
        public static ProcessConfiguration Create(
            string targetFilePath,
            string arguments,
            string? workingDirectory = null,
            bool outputRedirection =  true,
            Action<IProcessConfigurationBuilder>? configureBuilder = null,
            bool enableWindowCreation = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(targetFilePath);
            ArgumentNullException.ThrowIfNull(arguments);

            workingDirectory ??= Environment.CurrentDirectory;
            
            IProcessConfigurationBuilder processConfigurationBuilder =
                new ProcessConfigurationBuilder(
                        targetFilePath)
                    .SetArguments(arguments)
                    .SetWorkingDirectory(workingDirectory)
                    .SetOutputRedirection(outputRedirection)
                    .EnableWindowCreation(enableWindowCreation);

            if (configureBuilder is not null)
                configureBuilder.Invoke(processConfigurationBuilder);

            return processConfigurationBuilder.Build();
        }

        /// <summary>
        ///     Creates a Process configuration that can be run by a <see cref="IProcessInvoker" /> from
        ///     specified parameters.
        /// </summary>
        /// <param name="targetFilePath">The target file path of the command to be executed.</param>
        /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
        /// <param name="workingDirectory"></param>
        /// <param name="outputRedirection"></param>
        /// <param name="configureBuilder">
        ///     Actions to apply to the internal
        ///     <see cref="IProcessConfigurationBuilder" /> if not null.
        /// </param>
        /// <param name="enableWindowCreation"></param>
        /// <returns>The <see cref="ProcessConfiguration" /> created from the configured parameters.</returns>
        [Pure]
        public static ProcessConfiguration Create(
            string targetFilePath,
            IEnumerable<string> arguments,
            string? workingDirectory = null,
            bool outputRedirection =  true,
            Action<IProcessConfigurationBuilder>? configureBuilder = null,
            bool enableWindowCreation = false)
        {
            ArgumentException.ThrowIfNullOrEmpty(targetFilePath);
            ArgumentNullException.ThrowIfNull(arguments);

            IArgumentsBuilder argumentsBuilder = new ArgumentsBuilder()
                .AddRange(arguments);

            workingDirectory ??= Environment.CurrentDirectory;
            
            IProcessConfigurationBuilder processConfigurationBuilder =
                new ProcessConfigurationBuilder(
                        targetFilePath)
                    .SetArguments(argumentsBuilder.ToString())
                    .SetWorkingDirectory(workingDirectory)
                    .SetOutputRedirection(outputRedirection)
                    .EnableWindowCreation(enableWindowCreation);

            if (configureBuilder is not null)
                configureBuilder.Invoke(processConfigurationBuilder);

            return processConfigurationBuilder.Build();
        }
    }
}
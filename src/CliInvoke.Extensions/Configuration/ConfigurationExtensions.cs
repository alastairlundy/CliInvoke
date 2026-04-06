/*
    CliInvoke.Extensions

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using CliInvoke.Builders;
using CliInvoke.Core.Builders;
using CliInvoke.Helpers.Processes;

namespace CliInvoke.Extensions;

/// <summary>
///     Provides extension methods for working with configuration settings in the application,
///     enhancing functionality and simplifying common configuration-related tasks.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    ///     Provides extension methods for configuring and transforming process-related settings
    ///     to create reusable and standardized process configurations.
    /// </summary>
    extension(ProcessConfiguration)
    {
        /// <summary>
        ///     Converts a <see cref="ProcessStartInfo" /> instance to a <see cref="ProcessConfiguration" />
        ///     instance,
        ///     applying all relevant configurations such as environment variables, execution settings,
        ///     user credentials, and other process-related parameters.
        /// </summary>
        /// <param name="processStartInfo">
        ///     The <see cref="ProcessStartInfo" /> containing the process start
        ///     configuration.
        /// </param>
        /// <returns>
        ///     An instance of <see cref="ProcessConfiguration" /> with the configuration applied from
        ///     the provided <see cref="ProcessStartInfo" />.
        /// </returns>
        [OverloadResolutionPriority(2)]
        [Obsolete(
            "This method is deprecated and will be removed in CliInvoke.Extensions version 3.")]
        public static ProcessConfiguration FromStartInfo(ProcessStartInfo processStartInfo)
        {
            return ProcessConfiguration.FromProcessStartInfo(processStartInfo);
        }

        /// <summary>
        ///     Converts a <see cref="ProcessStartInfo" /> instance to a <see cref="ProcessConfiguration" />
        ///     instance,
        ///     applying all relevant configurations such as environment variables, execution settings,
        ///     user credentials, and other process-related parameters.
        /// </summary>
        /// <param name="processStartInfo">
        ///     The <see cref="ProcessStartInfo" /> containing the process start
        ///     configuration.
        /// </param>
        /// <returns>
        ///     An instance of <see cref="ProcessConfiguration" /> with the configuration applied from
        ///     the provided <see cref="ProcessStartInfo" />.
        /// </returns>
        [Pure]
        public static ProcessConfiguration FromProcessStartInfo(ProcessStartInfo processStartInfo)
        {
            bool requiresAdministrator =
                processStartInfo.Verb.StartsWith("runas", StringComparison.OrdinalIgnoreCase)
                || processStartInfo.Verb.StartsWith("sudo", StringComparison.OrdinalIgnoreCase);

            IEnvironmentVariablesBuilder environmentVariablesBuilder =
                new EnvironmentVariablesBuilder();

            IEnumerable<KeyValuePair<string, string>> kvp = processStartInfo.Environment
                .Where(kv => kv.Value is not null)
                // Suppression is okay here because we check for null before Select.

                // ReSharper disable once NullableWarningSuppressionIsUsed
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value!));

            environmentVariablesBuilder = environmentVariablesBuilder.SetEnumerable(kvp);

            IReadOnlyDictionary<string, string> environmentVars =
                environmentVariablesBuilder.Build();

            IProcessConfigurationBuilder processConfigurationBuilder =
                new ProcessConfigurationBuilder(processStartInfo.FileName);

            processConfigurationBuilder = processConfigurationBuilder
                .ConfigureEnvironmentVariables(envConfig =>
                {
                    envConfig.SetReadOnlyDictionary(environmentVars);
                })
                .UseShellExecution(processStartInfo.UseShellExecute)
                .EnableWindowCreation(!processStartInfo.CreateNoWindow)
                .SetWorkingDirectory(processStartInfo.WorkingDirectory)
                .SetArguments(processStartInfo.Arguments)
                .SetOutputRedirectionMode( processStartInfo.RedirectStandardOutput ||  processStartInfo.RedirectStandardError ?
                    OutputRedirectionMode.Buffer : OutputRedirectionMode.None)
                .SetProcessResourcePolicy(ProcessResourcePolicy.Default)
                .SetStandardInputPipe(StreamWriter.Null)
                .SetEncoding(
#if NET8_0_OR_GREATER
                    processStartInfo.StandardInputEncoding, 
#endif
                    processStartInfo.StandardOutputEncoding, processStartInfo.StandardErrorEncoding);
            
            
            if (requiresAdministrator)
                processConfigurationBuilder.RequireAdministratorPrivileges();

            IUserCredentialBuilder userCredentialBuilder = new UserCredentialBuilder();

#pragma warning disable CA1416
            if (processStartInfo.Domain != string.Empty)
                userCredentialBuilder = userCredentialBuilder.SetDomain(processStartInfo.Domain);

            if (processStartInfo.Password is not null)
                userCredentialBuilder.SetPassword(processStartInfo.Password);

            if (processStartInfo.UserName != string.Empty)
                userCredentialBuilder.SetUsername(processStartInfo.UserName);

            userCredentialBuilder.LoadUserProfile(processStartInfo.LoadUserProfile);

            processConfigurationBuilder =
                processConfigurationBuilder.SetUserCredential(userCredentialBuilder.Build());

#pragma warning restore CA1416
            return processConfigurationBuilder.Build();
        }
    }

    /// <param name="processConfiguration">
    ///     The <see cref="ProcessConfiguration" /> containing the process
    ///     configuration.
    /// </param>
    extension(ProcessConfiguration processConfiguration)
    {
        /// <summary>
        ///     Converts a <see cref="ProcessConfiguration" /> instance to a <see cref="ProcessStartInfo" />
        ///     instance,
        ///     applying all relevant configurations such as environment variables, execution settings,
        ///     user credentials, and other process-related parameters.
        /// </summary>
        /// <returns>
        ///     An instance of <see cref="ProcessStartInfo" /> with the configuration applied from
        ///     the provided <see cref="ProcessConfiguration" />.
        /// </returns>
        public ProcessStartInfo ToProcessStartInfo() => ToStartInfoExtensions.ToProcessStartInfo(processConfiguration);
    }
}
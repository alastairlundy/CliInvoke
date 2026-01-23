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

using CliInvoke.Builders;
using CliInvoke.Core.Builders;

namespace CliInvoke.Extensions;

/// <summary>
/// Provides extension methods for working with configuration settings in the application,
/// enhancing functionality and simplifying common configuration-related tasks.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Provides extension methods for configuring and transforming process-related settings
    /// to create reusable and standardized process configurations.
    /// </summary>
    extension(ProcessConfiguration)
    {
        /// <summary>
        /// Converts a <see cref="ProcessStartInfo"/> instance to a <see cref="ProcessConfiguration"/> instance,
        /// applying all relevant configurations such as environment variables, execution settings,
        /// user credentials, and other process-related parameters.
        /// </summary>
        /// <param name="processStartInfo">The <see cref="ProcessStartInfo"/> containing the process start configuration.</param>
        /// <returns>An instance of <see cref="ProcessConfiguration"/> with the configuration applied from
        /// the provided <see cref="ProcessStartInfo"/>.</returns>
        [Pure]
        public static ProcessConfiguration FromStartInfo(ProcessStartInfo processStartInfo)
        {
            bool requiresAdministrator =
                processStartInfo.Verb.StartsWith("runas", StringComparison.OrdinalIgnoreCase)
                || processStartInfo.Verb.StartsWith("sudo", StringComparison.OrdinalIgnoreCase);

            IEnvironmentVariablesBuilder environmentVariablesBuilder =
                new EnvironmentVariablesBuilder();

            IEnumerable<KeyValuePair<string, string>> kvp = processStartInfo.Environment.Where(kv => kv.Value is not null)
                // Suppression is okay here because we check for null before Select.

                // ReSharper disable once NullableWarningSuppressionIsUsed
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value!));

            environmentVariablesBuilder = environmentVariablesBuilder.SetEnumerable(kvp);
            
            IReadOnlyDictionary<string, string> environmentVars = environmentVariablesBuilder.Build();

            IProcessConfigurationBuilder processConfigurationBuilder =
                new ProcessConfigurationBuilder(processStartInfo.FileName);
            
            processConfigurationBuilder = processConfigurationBuilder.SetEnvironmentVariables(environmentVars)
                .ConfigureShellExecution(processStartInfo.UseShellExecute)
                .ConfigureWindowCreation(!processStartInfo.CreateNoWindow)
                .SetWorkingDirectory(processStartInfo.WorkingDirectory)
                .SetArguments(processStartInfo.Arguments);

            if (requiresAdministrator)
            {
                processConfigurationBuilder = processConfigurationBuilder.RequireAdministratorPrivileges();
            }

            processConfigurationBuilder = processConfigurationBuilder
                .RedirectStandardInput(processStartInfo.RedirectStandardInput)
                .RedirectStandardOutput(processStartInfo.RedirectStandardOutput)
                .RedirectStandardError(processStartInfo.RedirectStandardError)
                .SetProcessResourcePolicy(ProcessResourcePolicy.Default)
                .SetStandardInputPipe(StreamWriter.Null)
                .SetStandardOutputPipe(StreamReader.Null)
                .SetStandardErrorPipe(StreamReader.Null)
#if NETSTANDARD2_1 || NET5_0_OR_GREATER
                .SetStandardInputEncoding(processStartInfo.StandardInputEncoding)
#endif
                .SetStandardOutputEncoding(processStartInfo.StandardOutputEncoding)
                .SetStandardErrorEncoding(processStartInfo.StandardErrorEncoding);
            
            IUserCredentialBuilder userCredentialBuilder = new  UserCredentialBuilder();
            
            if(processStartInfo.Domain != string.Empty)
                userCredentialBuilder = userCredentialBuilder.SetDomain(processStartInfo.Domain);

            if (processStartInfo.Password is not null)
                userCredentialBuilder.SetPassword(processStartInfo.Password);

            if (processStartInfo.UserName != string.Empty)
                userCredentialBuilder.SetUsername(processStartInfo.UserName);

            userCredentialBuilder.LoadUserProfile(processStartInfo.LoadUserProfile);
            
            processConfigurationBuilder = processConfigurationBuilder.SetUserCredential(userCredentialBuilder.Build());
            
            return processConfigurationBuilder.Build();
        }
    }
}
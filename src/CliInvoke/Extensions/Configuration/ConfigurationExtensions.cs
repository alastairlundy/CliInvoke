/*
    CliInvoke.Extensions

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;

using CliInvoke.Builders;

namespace CliInvoke.Extensions;

/// <summary>
///     Extension methods for converting and transforming <see cref="ProcessConfiguration"/>.
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
        /// <remarks>
        ///     <see cref="ProcessStartInfo"/> exposes per-stream redirect flags
        ///     (<see cref="ProcessStartInfo.RedirectStandardOutput"/> and
        ///     <see cref="ProcessStartInfo.RedirectStandardError"/>), but
        ///     <see cref="ProcessConfiguration"/> has a single
        ///     <see cref="ProcessConfiguration.OutputRedirection"/> flag.
        ///     The two per-stream flags are collapsed via a logical OR into that single flag, so the
        ///     resulting configuration redirects output when either (or both) of the original flags
        ///     was set. This is a lossy conversion; the individual per-stream information is not
        ///     preserved.
        /// </remarks>
        [Pure]
        public static ProcessConfiguration FromProcessStartInfo(ProcessStartInfo processStartInfo)
        {
            bool requiresAdministrator =
                !string.IsNullOrEmpty(processStartInfo.Verb)
                && (processStartInfo.Verb.StartsWith("runas", StringComparison.OrdinalIgnoreCase)
                    || processStartInfo.Verb.StartsWith("sudo", StringComparison.OrdinalIgnoreCase));

            IEnumerable<KeyValuePair<string, string>> kvp = processStartInfo.Environment
                .Where(kv => kv.Value is not null)
                // Suppression is okay here because we check for null before Select.

                // ReSharper disable once NullableWarningSuppressionIsUsed
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value!));

            IProcessConfigurationBuilder processConfigurationBuilder =
                new ProcessConfigurationBuilder(processStartInfo.FileName);

            processConfigurationBuilder = processConfigurationBuilder
                .ConfigureEnvironmentVariables(envSpec =>
                {
                    envSpec.SetEnumerable(kvp);
                })
                .UseShellExecution(processStartInfo.UseShellExecute)
                .EnableWindowCreation(!processStartInfo.CreateNoWindow)
                .SetArguments(processStartInfo.Arguments)
                .SetOutputRedirection( processStartInfo.RedirectStandardOutput ||  processStartInfo.RedirectStandardError)
                .SetProcessResourcePolicy(ProcessResourcePolicy.Default)
                .SetStandardInputPipe(StreamWriter.Null)
                .SetEncoding(
                    processStartInfo.StandardInputEncoding,
                    processStartInfo.StandardOutputEncoding, processStartInfo.StandardErrorEncoding);

            if (!string.IsNullOrEmpty(processStartInfo.WorkingDirectory))
                processConfigurationBuilder.SetWorkingDirectory(processStartInfo.WorkingDirectory);

            if (requiresAdministrator)
                processConfigurationBuilder.RequireAdministratorPrivileges();

#pragma warning disable CA1416
            processConfigurationBuilder =
                processConfigurationBuilder.ConfigureUserCredential(credentialSpec =>
                {
                    if (processStartInfo.Domain != string.Empty)
                        credentialSpec.SetDomain(processStartInfo.Domain);

                    if (processStartInfo.Password is not null)
                        credentialSpec.SetPassword(processStartInfo.Password);

                    if (processStartInfo.UserName != string.Empty)
                        credentialSpec.SetUsername(processStartInfo.UserName);

                    credentialSpec.SetUserProfileLoading(processStartInfo.LoadUserProfile);
                });
#pragma warning restore CA1416
            return processConfigurationBuilder.Build();
        }
    }
}
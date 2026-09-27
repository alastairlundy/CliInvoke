/*
    CliInvoke.Extensions

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;

using CliInvoke.Builders;

namespace CliInvoke.Extensions.Configuration;

/// <summary>
///     Extension methods for converting and transforming <see cref="ProcessConfiguration"/>.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    ///     Provides extension methods for configuring and transforming process-related settings
    ///     to create reusable and standardised process configurations.
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
        ///     <para>
        ///         <see cref="ProcessStartInfo.RedirectStandardInput"/> maps to
        ///         <see cref="ProcessConfiguration.RedirectStandardInput"/>. Only the flag is mapped:
        ///         <see cref="ProcessConfiguration.StandardInput"/> keeps its builder default, because
        ///         the input-pipe <see cref="StreamWriter"/> must be supplied by the caller of
        ///         <see cref="IProcessConfigurationBuilder.SetStandardInputPipe"/>.
        ///     </para>
        ///     <para>
        ///         When <see cref="ProcessStartInfo.ArgumentList"/> is non-empty it is mapped to
        ///         <see cref="ProcessConfiguration.ArgumentList"/> and
        ///         <see cref="ProcessStartInfo.Arguments"/> is ignored, mirroring how the control
        ///         adapter treats a non-empty <see cref="ProcessConfiguration.ArgumentList"/> as the
        ///         canonical source and emits it via
        ///         <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> instead of the raw
        ///         command-line string. The raw <see cref="ProcessStartInfo.Arguments"/> string is
        ///         only mapped when the list API was not used.
        ///     </para>
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
                .SetOutputRedirection( processStartInfo.RedirectStandardOutput ||  processStartInfo.RedirectStandardError)
                .SetProcessResourcePolicy(ProcessResourcePolicy.Default)
                .SetStandardInputPipe(StreamWriter.Null)
                .RedirectStandardInput(processStartInfo.RedirectStandardInput)
                .SetEncoding(
                    processStartInfo.StandardInputEncoding,
                    processStartInfo.StandardOutputEncoding, processStartInfo.StandardErrorEncoding);

            // A non-empty ArgumentList is canonical (mirrors BaseProcessControlAdapter, which
            // clears Arguments and emits the list via ProcessStartInfo.ArgumentList); the raw
            // Arguments string is only used when the caller never populated the list API.
            if (processStartInfo.ArgumentList.Count > 0)
                processConfigurationBuilder.SetArgumentList(processStartInfo.ArgumentList);
            else
                processConfigurationBuilder.SetArguments(processStartInfo.Arguments);

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
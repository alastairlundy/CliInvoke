/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Linq;

namespace CliInvoke.Helpers.Processes;

internal static class ApplyConfigurationToProcessStartInfo
{
    /// <param name="processStartInfo"></param>
    extension(ProcessStartInfo processStartInfo)
    {
        /// <summary>
        /// Applies a requirement to run the process start info as an administrator.
        /// </summary>
        internal void RunAsAdministrator()
        {
            if (OperatingSystem.IsWindows())
            {
                processStartInfo.Verb = "runas";
            }
            else if (OperatingSystem.IsLinux() ||
                     OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ||
                     OperatingSystem.IsFreeBSD())
            {
                processStartInfo.Verb = "sudo";
            }
        }

        /// <summary>
        /// Adds the specified Credential to the current ProcessStartInfo object.
        /// </summary>
        /// <param name="credential">The credential to be added.</param>
        [SupportedOSPlatform("windows")]
        internal void SetUserCredential(UserCredential credential)
        {
            if (credential.Domain is not null && OperatingSystem.IsWindows())
            {
                processStartInfo.Domain = credential.Domain;
            }

            if (credential.UserName is not null)
            {
                processStartInfo.UserName = credential.UserName;
            }

            if (credential.Password is not null && OperatingSystem.IsWindows())
            {
                processStartInfo.Password = credential.Password;
            }

            if (credential.LoadUserProfile is not null && OperatingSystem.IsWindows())
            {
                processStartInfo.LoadUserProfile = (bool)credential.LoadUserProfile;
            }
        }
        
        /// <summary>
        /// Sets environment variables for a specified ProcessStartInfo object.
        /// </summary>
        /// <param name="environmentVariables">A dictionary of environment variable names and their corresponding values.</param>
        internal void SetEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables)
        {
            if (!environmentVariables.Any())
                return;

            foreach (KeyValuePair<string, string> variable in environmentVariables)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (variable.Value is not null)
                {
                    processStartInfo.Environment[variable.Key] = variable.Value;
                }
            }
        }
    }
}
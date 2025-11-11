/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AlastairLundy.CliInvoke.Helpers.Processes;

internal static class ProcessSetEnvironmentVariablesExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processStartInfo">The ProcessStartInfo object to set environment variables for.</param>
    extension(ProcessStartInfo processStartInfo)
    {
        /// <summary>
        /// Sets environment variables for a specified ProcessStartInfo object.
        /// </summary>
        /// <param name="environmentVariables">A dictionary of environment variable names and their corresponding values.</param>
        internal void SetEnvironmentVariables(
            IReadOnlyDictionary<string, string> environmentVariables
        )
        {
            if (environmentVariables.Any() == false)
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

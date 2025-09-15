/*
    AlastairLundy.CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AlastairLundy.CliInvoke.Internal;

internal static class ApplyConfigurationToProcessStartInfo
{
    /// <summary>
    /// Applies a requirement to run the process start info as an administrator.
    /// </summary>
    /// <param name="processStartInfo"></param>
    internal static void RunAsAdministrator(this ProcessStartInfo processStartInfo)
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
    /// Applies environment variables to a specified ProcessStartInfo object.
    /// </summary>
    /// <param name="processStartInfo">The ProcessStartInfo object to apply environment variables to.</param>
    /// <param name="environmentVariables">A dictionary of environment variable names and their corresponding values.</param>
    internal static void ApplyEnvironmentVariables(this ProcessStartInfo processStartInfo,
        IReadOnlyDictionary<string, string> environmentVariables)
    {
        if (environmentVariables.Any() == false)
            return;
        
        foreach (KeyValuePair<string, string> variable in environmentVariables)
        {
            if (variable.Value is not null)
            {
                processStartInfo.Environment[variable.Key] = variable.Value;
            }
        }
    }
}
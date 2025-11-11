/*
    AlastairLundy.CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;
using System.Runtime.Versioning;

using AlastairLundy.CliInvoke.Core;
using System;

namespace AlastairLundy.CliInvoke.Helpers.Processes;

#if NETSTANDARD2_0
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#endif

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
    /// Adds the specified Credential to the current ProcessStartInfo object.
    /// </summary>
    /// <param name="processStartInfo">The current ProcessStartInfo object.</param>
    /// <param name="credential">The credential to be added.</param>
    [SupportedOSPlatform("windows")]
    internal static void SetUserCredential(this ProcessStartInfo processStartInfo, UserCredential credential)
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
}
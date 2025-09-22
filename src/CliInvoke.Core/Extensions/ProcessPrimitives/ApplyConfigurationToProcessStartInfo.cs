/*
    AlastairLundy.CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System;
using System.Diagnostics;

namespace AlastairLundy.CliInvoke.Core;

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
    

}
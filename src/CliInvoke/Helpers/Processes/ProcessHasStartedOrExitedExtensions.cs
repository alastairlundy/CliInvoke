/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Diagnostics;
using System.Runtime.Versioning;

using AlastairLundy.CliInvoke.Internal.Localizations;

namespace AlastairLundy.CliInvoke.Helpers.Processes;

internal static class ProcessHasStartedOrExitedExtensions
{
    /// <summary>
    /// Determines if a process has started.
    /// </summary>
    /// <param name="process">The process to be checked.</param>
    /// <returns>True if it has started; false otherwise.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static bool HasStarted(this Process process)
    {
        if (process.IsProcessOnRemoteDevice())
            throw new NotSupportedException(Resources.Exceptions_Processes_NotSupportedOnRemoteProcess);
        
        try
        {
            return process.StartTime.ToUniversalTime() <= DateTime.UtcNow;
        }
        catch(InvalidOperationException)
        {
            return false;
        }
    }
}
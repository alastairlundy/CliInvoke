/*
    AlastairLundy.CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;

// ReSharper disable RedundantBoolCompare

namespace AlastairLundy.CliInvoke.Helpers.Processes;

internal static class IsProcessRunningExtensions
{
    /// <summary>
    /// Check to see if a specified process is running or not.
    /// </summary>
    /// <param name="process">The process to be checked.</param>
    /// <returns>True if the specified process is running; returns false otherwise.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static bool IsRunning(this Process process) => 
        process.HasStarted() && process.HasExited() == false;

    /// <summary>
    /// Detects whether a process is running on a remote device.
    /// </summary>
    /// <param name="process"></param>
    /// <returns>True if the process is running on a remote device, false otherwise.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the process has been disposed of.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static bool IsRunningOnRemoteDevice(this Process process)
    {
        if (process.IsDisposed())
        {
            throw new InvalidOperationException();
        }
        
        return Process.GetProcesses().All(x => x.Id != process.Id) && 
               process.MachineName.Equals(Environment.MachineName) == false;
    }
}
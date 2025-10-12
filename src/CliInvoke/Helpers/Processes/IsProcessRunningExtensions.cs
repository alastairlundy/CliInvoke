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

namespace AlastairLundy.CliInvoke.Helpers.Processes;

internal static class IsProcessRunningExtensions
{
   
    /// <summary>
    /// Determines whether a process exists on a remote device or locally.
    /// </summary>
    /// <param name="process"></param>
    /// <returns>True if the process exists on a remote device, false if it exists locally.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the process has been disposed of.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static bool IsProcessOnRemoteDevice(this Process process)
    {
        if (process.IsDisposed())
            throw new InvalidOperationException();

        try
        {
            bool hasExited = process.HasExited;

            if (hasExited)
                return false;
            
            return hasExited;
        }
        catch (NotSupportedException exception)
        {
            return true;
        }
    }
}
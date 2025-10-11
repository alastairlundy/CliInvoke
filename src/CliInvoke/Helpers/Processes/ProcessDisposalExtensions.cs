/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;

namespace AlastairLundy.CliInvoke.Helpers.Processes;

/// <summary>
/// 
/// </summary>
public static class ProcessDisposalExtensions
{
    /// <summary>
    /// Determines if a process has been disposed.
    /// </summary>
    /// <param name="process">The process to check if the process has been disposed of.</param>
    /// <returns>True if the process has been disposed of, false otherwise.</returns>
    internal static bool IsDisposed(this Process process)
    {
        try
        {
            return string.IsNullOrEmpty(process.Id.ToString());
        }
        catch
        {
            return true;
        }
    }

}
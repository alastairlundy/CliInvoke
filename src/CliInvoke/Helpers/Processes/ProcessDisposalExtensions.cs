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
internal static class ProcessDisposalExtensions
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="process">The process to check if the process has been disposed of.</param>
    extension(Process process)
    {
        /// <summary>
        /// Determines if a process has been disposed of.
        /// </summary>
        /// <returns>True if the process has been disposed of, false otherwise.</returns>
        internal bool IsDisposed()
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
}

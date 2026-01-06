/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Helpers.Processes;

internal static class ProcessHasStartedExtensions
{
    /// <param name="process">The process to be checked.</param>
    extension(Process process)
    {
        /// <summary>
        /// Determines if a process has started.
        /// </summary>
        /// <returns>True if it has started; false otherwise.</returns>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
        internal bool HasStarted()
        {
            try
            {
                return process.StartTime.ToUniversalTime() <= DateTime.UtcNow;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}

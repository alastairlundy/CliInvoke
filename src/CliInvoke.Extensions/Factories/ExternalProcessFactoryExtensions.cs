/*
    CliInvoke.Extensions
     
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics;
using System.Diagnostics.Contracts;

using CliInvoke.Core.Processes;

namespace CliInvoke.Extensions.Factories;

/// <summary>
/// Extension methods for creating <see cref="IExternalProcess"/> instances.
/// </summary>
public static class ExternalProcessFactoryExtensions
{
    /// <summary>
    /// Extension methods for creating <see cref="IExternalProcess"/> instances using <see cref="ProcessStartInfo"/>.
    /// </summary>
    extension(IExternalProcessFactory externalProcessFactory)
    {
        /// <summary>
        /// Creates an instance of the <see cref="IExternalProcess"/> interface based on the provided
        /// <see cref="ProcessStartInfo"/>.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> used to configure the external process.</param>
        /// <returns>An implementation of <see cref="IExternalProcess"/> that represents the created external process.</returns>
        [Pure]
        public IExternalProcess CreateExternalProcess(ProcessStartInfo startInfo)
            => externalProcessFactory.CreateExternalProcess(ProcessConfiguration.FromStartInfo(startInfo));

        /// <summary>
        /// Creates an instance of the <see cref="IExternalProcess"/> interface based on the provided
        /// <see cref="ProcessStartInfo"/>.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> used to configure the external process.</param>
        /// <param name="exitConfiguration">The process exit configuration details for configuring process exit behaviour.</param>
        /// <returns>An implementation of <see cref="IExternalProcess"/> that represents the created external process.</returns>
        [Pure]
        public IExternalProcess CreateExternalProcess(ProcessStartInfo startInfo,
            ProcessExitConfiguration exitConfiguration) =>
            externalProcessFactory.CreateExternalProcess(ProcessConfiguration.FromStartInfo(startInfo),
                exitConfiguration);
    }
}
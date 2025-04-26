/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System;
using System.Diagnostics;
using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Core.Abstractions.Legacy;

/// <summary>
/// An interface to enable Creating Processes from Command Configuration inputs.
/// </summary>
public interface ICommandProcessFactory
{
    /// <summary>
    /// Creates a process with the specified process start information.
    /// </summary>
    /// <param name="processStartInfo">The process start information to be used to configure the process to be created.</param>
    /// <returns>The newly created Process with the specified start information.</returns>
    [Obsolete]
    Process CreateProcess(ProcessStartInfo processStartInfo);

    /// <summary>
    /// Creates Process Start Information based on specified Command configuration object values.
    /// </summary>
    /// <param name="commandConfiguration">The command configuration object to specify Process info.</param>
    /// <returns>A new ProcessStartInfo object configured with the specified Command object values.</returns>
    ProcessStartInfo ConfigureProcess(CliCommandConfiguration commandConfiguration);

    /// <summary>
    /// Creates Process Start Information based on specified parameters and Command configuration object values.
    /// </summary>
    /// <param name="commandConfiguration">The command configuration object to specify Process info.</param>
    /// <param name="redirectStandardOutput">Whether to redirect the Standard Output.</param>
    /// <param name="redirectStandardError">Whether to redirect the Standard Error.</param>
    /// <returns>A new ProcessStartInfo object configured with the specified parameters and Command object values.</returns>
    ProcessStartInfo ConfigureProcess(CliCommandConfiguration commandConfiguration, bool redirectStandardOutput, bool redirectStandardError);
}
/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace AlastairLundy.CliInvoke.Core.Extensibility.Factories;

/// <summary>
/// An interface to allow creating a ProcessConfiguration that can be run through other Process' ProcessConfiguration.
/// </summary>
public interface IRunnerProcessFactory
{
    /// <summary>
    /// Creates a ProcessConfiguration to be run from a runner configuration and an input ProcessConfiguration.
    /// </summary>
    /// <param name="processConfigToBeRun">The command to be run by the ProcessConfiguration Runner.</param>
    /// <param name="runnerProcessConfig">The process running configuration to use for the ProcessConfiguration that will run other ProcessConfigurations.</param>
    /// <returns>The built Command that will run the input command with the runner process configuration.</returns>
    ProcessConfiguration CreateRunnerConfiguration(ProcessConfiguration processConfigToBeRun,
        ProcessConfiguration runnerProcessConfig);
}
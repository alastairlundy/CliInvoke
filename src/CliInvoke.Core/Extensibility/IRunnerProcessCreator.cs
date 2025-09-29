/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Core.Extensibility;

/// <summary>
/// 
/// </summary>
public interface IRunnerProcessCreator
{
    /// <summary>
    /// Creates a ProcessConfiguration to be run from a runner configuration and an input ProcessConfiguration.
    /// </summary>
    /// <param name="inputCommand">The command to be run by the ProcessConfiguration Runner.</param>
    /// <returns>The built Command that will run the input command.</returns>
    ProcessConfiguration CreateRunnerProcess(ProcessConfiguration inputCommand);
}
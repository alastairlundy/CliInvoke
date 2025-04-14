/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using AlastairLundy.CliInvoke.Abstractions;

namespace AlastairLundy.CliInvoke.Extensibility.Abstractions.Invokers;

public interface ISpecializedCliCommandInvoker : ICliCommandInvoker
{
    /// <summary>
    /// Create the command to be run from the Command runner configuration and an input command.
    /// </summary>
    /// <param name="inputCommand">The command to be run by the Command Runner command.</param>
    /// <returns>The built Command that will run the input command.</returns>
    CliCommandConfiguration CreateRunnerCommand(CliCommandConfiguration inputCommand);
}
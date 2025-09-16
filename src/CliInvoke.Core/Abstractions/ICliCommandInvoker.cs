/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core.Internal;

using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

namespace AlastairLundy.CliInvoke.Core.Abstractions;

/// <summary>
/// An interface to specify the required Command Running functionality.
/// </summary>
[Obsolete(DeprecationMessages.InterfaceDeprecationV2)]
public interface ICliCommandInvoker
{
    /// <summary>
    /// Executes a command asynchronously and returns Command execution information as a ProcessResult.
    /// </summary>
    /// <param name="commandConfiguration">The command to be executed.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>A ProcessResult object containing the execution information of the command.</returns>
    [Obsolete(DeprecationMessages.InterfaceDeprecationV2)]
    Task<ProcessResult> ExecuteAsync(CliCommandConfiguration commandConfiguration, CancellationToken cancellationToken = default);
        
    /// <summary>
    ///Executes a command asynchronously and returns Command execution information and Command output as a BufferedProcessResult.
    /// </summary>
    /// <param name="commandConfiguration">The command to be executed.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>A BufferedProcessResult object containing the output of the command.</returns>
    [Obsolete(DeprecationMessages.InterfaceDeprecationV2)]
    Task<BufferedProcessResult> ExecuteBufferedAsync(CliCommandConfiguration commandConfiguration, CancellationToken cancellationToken = default);
}
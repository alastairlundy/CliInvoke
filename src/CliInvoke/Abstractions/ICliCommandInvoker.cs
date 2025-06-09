/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Results;


// ReSharper disable once CheckNamespace
namespace AlastairLundy.CliInvoke.Abstractions;

/// <summary>
/// An interface to specify the required Command Running functionality.
/// </summary>
public interface ICliCommandInvoker
{
    /// <summary>
    /// Executes a command asynchronously and returns Command execution information as a ProcessResult.
    /// </summary>
    /// <param name="processConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>A ProcessResult object containing the execution information of the command.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default);

    ///  <summary>
    /// Executes a command asynchronously and returns Command execution information and Command output as a BufferedProcessResult.
    ///  </summary>
    ///  <param name="processConfiguration"></param>
    ///  <param name="cancellationToken">A token to cancel the operation if required.</param>
    ///  <returns>A BufferedProcessResult object containing the output of the command.</returns>
    Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default);
}
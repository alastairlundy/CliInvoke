/*
    AlastairLundy.CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// Defines the contract for a class that executes processes.
/// </summary>
public interface IProcessInvoker
{
    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The process configuration to use.</param>
    /// <param name="processExitInfo">The process exit information to use.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from the running the process.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo">The process start information to use.</param>
    /// <param name="processExitInfo">The process exit information to use.</param>
    /// <param name="standardInput">The Stream to redirect to the Standard Input if not null.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from the running the process.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessStartInfo processStartInfo,
        ProcessExitConfiguration? processExitInfo = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The process configuration to use.</param>
    /// <param name="processExitInfo">The process exit information to use.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the process asynchronously, waits for exit, reads the standard output, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo">The process start information to use.</param>
    /// <param name="processExitInfo">The process exit information to use.</param>
    /// <param name="standardInput">The Stream to redirect to the Standard Input if not null.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Result that is returned from running the process.</returns>
    Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessStartInfo processStartInfo,
        ProcessExitConfiguration? processExitInfo = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pipes the Standard Input, runs the process asynchronously,
    /// waits for exit, pipes the standard output and error, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration for the process to be executed.</param>
    /// <param name="processExitInfo">The process exit information to use.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Result that is returned from running the process.</returns>
    Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pipes the Standard Input, runs the process asynchronously,
    /// waits for exit, pipes the standard output and error, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo">The process start information to use.</param>
    /// <param name="processExitInfo">The process exit information to use.</param>
    /// <param name="standardInput">The Stream to redirect to the Standard Input if not null.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Result that is returned from running the process.</returns>
    Task<PipedProcessResult> ExecutePipedAsync(ProcessStartInfo processStartInfo,
        ProcessExitConfiguration? processExitInfo = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);
}
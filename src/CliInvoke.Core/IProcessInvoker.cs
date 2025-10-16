/*
    AlastairLundy.CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading;
using System.Threading.Tasks;

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// Defines a service that creates and runs Processes from <see cref="ProcessConfiguration"/> objects.
/// </summary>
public interface IProcessInvoker
{
    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The process configuration to use.</param>
    /// <param name="processExitConfiguration">The process exiting configuration information to use.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from the running the process.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The process configuration to use.</param>
    /// <param name="processExitConfiguration">The process exiting configuration information to use.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pipes the Standard Input, runs the process asynchronously,
    /// waits for exit, pipes the standard output and error, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration for the process to be executed.</param>
    /// <param name="processExitConfiguration">The process exiting configuration information to use.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Result that is returned from running the process.</returns>
    Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default);
}
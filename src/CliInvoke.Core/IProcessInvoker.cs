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
/// Defines the contract for a class that creates and runs Process objects from <see cref="ProcessStartInfo"/> objects.
/// </summary>
public interface IProcessInvoker
{
    
    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="startInfo">The start info to use for <see cref="Process"/> creation.</param>
    /// <param name="processResourcePolicy">The resource policy to use for <see cref="Process"/> creation.</param>
    /// <param name="processTimeoutPolicy">The timeout policy to use when waiting for <see cref="Process"/> exit.</param>
    /// <param name="processResultValidation"></param>
    /// <param name="standardInput">The standard input to pipe to the Process, if specified.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from the running the process.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessStartInfo startInfo, ProcessResourcePolicy processResourcePolicy,
        ProcessTimeoutPolicy processTimeoutPolicy, ProcessResultValidation processResultValidation,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="startInfo">The start info to use for <see cref="Process"/> creation.</param>
    /// <param name="processResourcePolicy">The resource policy to use for <see cref="Process"/> creation.</param>
    /// <param name="processTimeoutPolicy">The timeout policy to use when waiting for <see cref="Process"/> exit.</param>
    /// <param name="processResultValidation"></param>
    /// <param name="standardInput">The standard input to pipe to the Process, if specified.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessStartInfo startInfo,
        ProcessResourcePolicy processResourcePolicy, ProcessTimeoutPolicy processTimeoutPolicy,
        ProcessResultValidation processResultValidation,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pipes the Standard Input (if applicable), runs the process asynchronously,
    /// waits for exit, pipes the standard output and error, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="startInfo">The start info to use for <see cref="Process"/> creation.</param>
    /// <param name="processResourcePolicy">The resource policy to use for <see cref="Process"/> creation.</param>
    /// <param name="processTimeoutPolicy">The timeout policy to use when waiting for <see cref="Process"/> exit.</param>
    /// <param name="processResultValidation"></param>
    /// <param name="standardInput">The standard input to pipe to the Process, if specified.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Result that is returned from running the process.</returns>
    Task<PipedProcessResult> ExecutePipedAsync(ProcessStartInfo startInfo,
        ProcessResourcePolicy processResourcePolicy, ProcessTimeoutPolicy processTimeoutPolicy,
        ProcessResultValidation processResultValidation,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);
}
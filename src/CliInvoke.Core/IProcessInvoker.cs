﻿/*
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
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// Defines the contract for a class that executes processes.
/// </summary>
public interface IProcessInvoker
{
    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from the running the process.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processResultValidation">The process result validation to be used.</param>
    /// <param name="processResourcePolicy">The process resource policy to be set if it is not null.</param>
    /// <param name="userCredential"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from the running the process.</returns>
    Task<ProcessResult> ExecuteAsync(ProcessStartInfo processStartInfo,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null,
        UserCredential? userCredential = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processResultValidation">The process result validation to be used.</param>
    /// <param name="processResourcePolicy">The process resource policy to be set if it is not null.</param>
    /// <param name="userCredential"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessStartInfo processStartInfo,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null,
        UserCredential? userCredential = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processResultValidation"></param>
    /// <param name="processResourcePolicy"></param>
    /// <param name="userCredential"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<PipedProcessResult> ExecutePipedAsync(ProcessStartInfo processStartInfo,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null,
        UserCredential? userCredential = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default);
}
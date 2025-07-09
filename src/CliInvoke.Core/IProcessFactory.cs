/*
    AlastairLundy.CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.DotPrimitives.Processes;
using AlastairLundy.DotPrimitives.Processes.Policies;
using AlastairLundy.DotPrimitives.Processes.Results;

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// Create and manage processes efficiently.
/// </summary>
public interface IProcessFactory
{
    /// <summary>
    /// Creates a process from the specified start info.
    /// </summary>
    /// <param name="startInfo">The start information to use for the Process.</param>
    /// <returns>The newly created Process.</returns>
    Process From(ProcessStartInfo startInfo);
    
    /// <summary>
    /// Creates a process from the specified start info and UserCredential.
    /// </summary>
    /// <param name="startInfo">The start information to use for the Process.</param>
    /// <param name="credential">The credential to use when creating the Process.</param>
    /// <returns>The newly created Process.</returns>
    Process From(ProcessStartInfo startInfo, UserCredential credential);
    
    /// <summary>
    /// Creates a process from the specified process configuration.
    /// </summary>
    /// <param name="configuration">The configuration information to use to configure the Process.</param>
    /// <returns>The newly created Process with the configuration.</returns>
    Process From(ProcessConfiguration configuration);
    
    /// <summary>
    /// Creates and starts a new Process with the specified Process Start Info.
    /// </summary>
    /// <param name="startInfo">The start info to use when creating and starting the new Process.</param>
    /// <returns>The newly created and started Process with the start info.</returns>
    Process StartNew(ProcessStartInfo startInfo);
    
    /// <summary>
    /// Creates and starts a new Process with the specified Process Start Info and credential.
    /// </summary>
    /// <param name="startInfo">The start info to use when creating and starting the new Process.</param>
    /// <param name="credential">The credential to use when creating and starting the Process.</param>
    /// <returns>The newly created and started Process with the start info and credential.</returns>
    Process StartNew(ProcessStartInfo startInfo, UserCredential credential);
    
    /// <summary>
    /// Creates and starts a new Process with the specified Process Start Info and Process Resource policy.
    /// </summary>
    /// <param name="startInfo">The start info to use when creating and starting the new Process.</param>
    /// <param name="resourcePolicy">The process resource policy to use when creating and starting the new Process.</param>
    /// <returns>The newly created and started Process with the start info and Process Resource Policy.</returns>
    Process StartNew(ProcessStartInfo startInfo, ProcessResourcePolicy resourcePolicy);
    
    /// <summary>
    /// Creates and starts a new Process with the specified Process Start Info, credential, and Process Resource policy.
    /// </summary>
    /// <param name="startInfo">The start info to use when creating and starting the new Process.</param>
    /// <param name="resourcePolicy">The process resource policy to use when creating and starting the new Process.</param>
    /// <param name="credential">The credential to use when creating and starting the Process.</param>
    /// <returns>The newly created and started Process with the start info and Process Resource Policy.</returns>
    Process StartNew(ProcessStartInfo startInfo, ProcessResourcePolicy resourcePolicy, UserCredential credential);
    
    /// <summary>
    /// Creates and starts a new Process with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration to use when creating and starting the process.</param>
    /// <returns>The newly created and started Process with the specified configuration.</returns>
    Process StartNew(ProcessConfiguration configuration);

    /// <summary>
    /// Creates a Task that returns a ProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The task and ProcessResult that are returned upon the process' exit.</returns>
    Task<ProcessResult> ContinueWhenExitAsync(Process process, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Task that returns a ProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="resultValidation">Whether to perform Result validation on the process' exit code.</param>
    /// <param name="processTimeoutPolicy">The process timeout policy to use when waiting for the process to exit.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The task and ProcessResult that are returned upon the process' exit.</returns>
    Task<ProcessResult> ContinueWhenExitAsync(Process process, ProcessResultValidation resultValidation,
        ProcessTimeoutPolicy? processTimeoutPolicy = null, CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Creates a Task that returns a ProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="processConfiguration">The configuration to use when running and waiting for the process to exit.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The task and ProcessResult that are returned upon the process' exit.</returns>
    Task<ProcessResult> ContinueWhenExitAsync(Process process, ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a Task that returns a BufferedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The task and BufferedProcessResult that are returned upon the process' exit.</returns>
    Task<BufferedProcessResult> ContinueWhenExitBufferedAsync(Process process, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Task that returns a BufferedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="resultValidation">Whether to perform Result validation on the process' exit code.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <param name="processTimeoutPolicy">The process timeout policy to use when waiting for the process to exit.</param>
    /// <returns>The task and BufferedProcessResult that are returned upon the process' exit.</returns>
    Task<BufferedProcessResult> ContinueWhenExitBufferedAsync(Process process,
        ProcessResultValidation resultValidation, ProcessTimeoutPolicy? processTimeoutPolicy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Task that returns a BufferedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="processConfiguration">The configuration to use when running and waiting for the process to exit.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The task and BufferedProcessResult that are returned upon the process' exit.</returns>
    Task<BufferedProcessResult> ContinueWhenExitBufferedAsync(Process process,
        ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default);  
    
    /// <summary>
    /// Creates a Task that returns a PipedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The Buffered Process Result that is returned from running the process.</returns>
    Task<PipedProcessResult> ContinueWhenExitPipedAsync(Process process, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Task that returns a PipedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="resultValidation">Whether to perform Result validation on the process' exit code.</param>
    /// <param name="processTimeoutPolicy">The process timeout policy to use when waiting for the process to exit.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The Piped Process Result that is returned from running the process.</returns>
    Task<PipedProcessResult> ContinueWhenExitPipedAsync(Process process, ProcessResultValidation resultValidation,
        ProcessTimeoutPolicy? processTimeoutPolicy = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a Task that returns a PipedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="processConfiguration">The configuration to use when running and waiting for the process to exit.</param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The Piped Process Result that is returned from running the process.</returns>
    Task<PipedProcessResult> ContinueWhenExitPipedAsync(Process process,
        ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default);  

}
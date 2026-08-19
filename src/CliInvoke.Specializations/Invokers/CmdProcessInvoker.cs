/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Factories;
using CliInvoke.Core.Middleware;
using CliInvoke.Specializations.Configurations;
using CliInvoke.Specializations.Middleware;

namespace CliInvoke.Specializations;

/// <summary>
///     Represents a specialised invoker for executing command-line processes on Windows systems.
///     Provides functionality to execute processes either with raw output, buffered output, or piped
///     streams.
/// </summary>
/// <remarks>
///     The <c>CmdProcessInvoker</c> is now a thin convenience wrapper around
///     <see cref="CliInvoke.ProcessInvoker"/> with <see cref="CliInvoke.Specializations.Middleware.CmdMiddleware"/>
///     applied. The middleware (which delegates shell-flag and target resolution to
///     <see cref="CmdProcessConfiguration"/>) is the single source of truth for CMD wrapping; this
///     class simply forwards each invocation. This implementation is supported only on the Windows
///     operating system and explicitly excludes support for other platforms.
/// </remarks>
public class CmdProcessInvoker : IProcessInvoker
{
    private readonly ProcessInvoker _processInvoker;

    /// <summary>
    ///     Represents a process invoker specialised for running processes through CMD on Windows
    ///     platforms.
    /// </summary>
    /// <remarks>
    ///     This class provides a specialisation of the <see cref="IProcessInvoker" /> for
    ///     executing
    ///     command-line processes through CMD with additional configuration options such as window
    ///     creation and output redirection.
    ///     This implementation is supported only on the Windows operating system and explicitly excludes
    ///     support for other platforms.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public CmdProcessInvoker(IExternalProcessFactory externalProcessFactory)
    {
        IReadOnlyList<IProcessMiddleware> middlewares = [new CmdMiddleware()];
        _processInvoker = new ProcessInvoker(externalProcessFactory, middlewares);
    }

    /// <summary>
    ///     Executes a process asynchronously with support for specific platform constraints.
    /// </summary>
    /// <param name="processConfiguration">The configuration for the process to be executed.</param>
    /// <param name="processExitConfiguration">
    ///     Optional configuration for handling the process exit behaviour.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation, where the result
    ///     contains
    ///     the execution outcome of the process.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    ///     Thrown if the operating system is not Windows, as this method
    ///     is only supported on Windows platforms.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public Task<ProcessResult> ExecuteAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        return _processInvoker.ExecuteAsync(processConfiguration, processExitConfiguration, cancellationToken);
    }

    /// <summary>
    ///     Executes a process asynchronously with buffering for output and error streams.
    /// </summary>
    /// <param name="processConfiguration"> The configuration for the process to be executed. </param>
    /// <param name="processExitConfiguration">
    ///     Optional configuration for handling the process exit
    ///     behaviour.
    /// </param>
    /// <param name="cancellationToken"> A token to monitor for cancellation requests. </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation, where the result
    ///     contains the buffered output, error streams, and exit information for the executed process.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    ///     Thrown if the operating system is not Windows, as this method is only supported on Windows
    ///     platforms.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        return _processInvoker.ExecuteBufferedAsync(processConfiguration, processExitConfiguration, cancellationToken);
    }

    /// <summary>
    ///     Executes a process asynchronously while piping the output and error streams for processing.
    /// </summary>
    /// <param name="processConfiguration">The configuration for the process to be executed.</param>
    /// <param name="processExitConfiguration">
    ///     Optional configuration for handling the process exit behaviour.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation, where the result
    ///     includes the piped output, error streams, and exit information for the executed process.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    ///     Thrown if the operating system is not Windows, as this method
    ///     is only supported on Windows platforms.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        return _processInvoker.ExecutePipedAsync(processConfiguration, processExitConfiguration, cancellationToken);
    }
}
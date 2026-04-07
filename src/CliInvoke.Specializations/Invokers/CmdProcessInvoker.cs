/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Threading;
using System.Threading.Tasks;

using CliInvoke.Specializations.Configurations;

using WhatExec.Lib.Abstractions.Resolvers;

namespace CliInvoke.Specializations;

/// <summary>
///     Represents a specialized invoker for executing command-line processes on Windows systems.
///     Provides functionality to execute processes either with raw output, buffered output, or piped
///     streams.
/// </summary>
public class CmdProcessInvoker : ProcessInvoker
{
    private readonly IRunnerConfigurationFactory _runnerConfigurationFactory;

    /// <summary>
    ///     Represents a process invoker specialized for running processes through CMD on Windows
    ///     platforms.
    /// </summary>
    /// <remarks>
    ///     This class provides a specialization of the <see cref="IProcessInvoker" /> for
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
    public CmdProcessInvoker(IRunnerConfigurationFactory runnerConfigurationFactory, IExecutableFileResolver executableFileResolver) :
        base(executableFileResolver)
    {
        _runnerConfigurationFactory = runnerConfigurationFactory;
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
    public new async Task<ProcessResult> ExecuteAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null, 
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupported();

        using ProcessConfiguration runnerConfiguration =
            _runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration,
                new CmdProcessConfiguration(processConfiguration.Arguments, processConfiguration.RedirectStandardInput,
                    OutputRedirectionMode.None));

        return await base.ExecuteAsync(runnerConfiguration, processExitConfiguration, cancellationToken);
    }

    private static void ThrowIfUnsupported()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(Resources
                .Exceptions_Cmd_OnlySupportedOnWindows);
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
    public new async Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupported();
        
        using ProcessConfiguration runnerConfiguration =
            _runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration,
                new CmdProcessConfiguration(processConfiguration.Arguments, processConfiguration.RedirectStandardInput,
                    OutputRedirectionMode.None));

        return await base.ExecuteBufferedAsync(runnerConfiguration, processExitConfiguration, cancellationToken);
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
    public new async Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupported();

        using ProcessConfiguration runnerConfiguration =
            _runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration,
                new CmdProcessConfiguration(processConfiguration.Arguments, processConfiguration.RedirectStandardInput,
                    OutputRedirectionMode.None));

        return await base.ExecutePipedAsync(runnerConfiguration, processExitConfiguration, cancellationToken);
    }
}
/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;
using CliInvoke.Factories;

namespace CliInvoke;

/// <summary>
/// The default implementation of <see cref="IProcessInvoker"/>, a safer way to execute processes.
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IExternalProcessFactory _externalProcessFactory;
    
    /// <summary>
    /// Instantiates a <see cref="ProcessInvoker"/> for creating and executing processes.
    /// </summary>
    /// <param name="filePathResolver">The file path resolver to be used.</param>
    /// <param name="processPipeHandler">The pipe handler to be used for managing the input/output streams of the processes.</param>
    public ProcessInvoker(
        IFilePathResolver filePathResolver,
        IProcessPipeHandler processPipeHandler)
    {
        IFilePathResolver filePathResolver1 = filePathResolver;
        IProcessPipeHandler processPipeHandler1 = processPipeHandler;

        _externalProcessFactory = new ExternalProcessFactory(filePathResolver1, processPipeHandler1);
    }
    
    public ProcessInvoker(IExternalProcessFactory externalProcessFactory)
    {
        _externalProcessFactory = externalProcessFactory;
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration">The exit configuration to use for the process, or the default if null.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file, with the file name of the process to be executed, is not found.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ProcessResult> ExecuteAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default)
    {
        processExitConfiguration ??= ProcessExitConfiguration.Default;
        
        using IExternalProcess externalProcess = _externalProcessFactory
            .CreateExternalProcess(processConfiguration, processExitConfiguration);
        
        await externalProcess.StartAsync(cancellationToken);

        return await externalProcess.WaitForExitOrTimeoutAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration">The exit configuration to use for the process, or the default if null.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default)
    {
        processExitConfiguration ??= ProcessExitConfiguration.Default;
        
        using IExternalProcess externalProcess = _externalProcessFactory
            .CreateExternalProcess(processConfiguration, processExitConfiguration);
        
        await externalProcess.StartAsync(cancellationToken);

        return await externalProcess.WaitForBufferedExitOrTimeoutAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Streams, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration">The exit configuration to use for the process, or the default if null.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Results from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default)
    {
        processExitConfiguration ??= ProcessExitConfiguration.Default;
        
        using IExternalProcess externalProcess = _externalProcessFactory
            .CreateExternalProcess(processConfiguration, processExitConfiguration);
        
        await externalProcess.StartAsync(cancellationToken);

        return await externalProcess.WaitForPipedExitOrTimeoutAsync(cancellationToken);
    }
}
/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;

namespace CliInvoke;

/// <summary>
///     The default implementation of <see cref="IProcessInvoker" />, a safer way to execute processes.
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IExternalProcessFactory _externalProcessFactory;

    /// <summary>
    ///     Instantiates a <see cref="ProcessInvoker" /> for creating and executing processes.
    /// </summary>
    /// <param name="externalProcessFactory"></param>
    public ProcessInvoker(IExternalProcessFactory externalProcessFactory)
    {
        _externalProcessFactory = externalProcessFactory;
    }

    /// <summary>
    ///     Runs the process asynchronously, waits for exit, and safely disposes of the Process before
    ///     returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration">
    ///     The exit configuration to use for the process, or the
    ///     default if null.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">
    ///     Thrown if the file, with the file name of the process to be
    ///     executed, is not found.
    /// </exception>
    /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">
    ///     Thrown if the result validation requires the
    ///     process to exit with exit code zero and the process exits with a different exit code.
    /// </exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        IExternalProcess externalProcess = _externalProcessFactory.CreateExternalProcess(processConfiguration,
            processExitConfiguration ?? ProcessExitConfiguration.Default);

        try
        {
            await externalProcess.StartAsync(cancellationToken);

            return await externalProcess.WaitForExitOrTimeoutAsync(cancellationToken);
        }
        finally
        {
            externalProcess.Dispose();
        }
    }

    /// <summary>
    ///     Runs the process asynchronously with Standard Output and Standard Error Redirection,
    ///     gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the
    ///     Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration">
    ///     The exit configuration to use for the process, or the
    ///     default if null.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">
    ///     Thrown if the result validation requires the
    ///     process to exit with exit code zero and the process exits with a different exit code.
    /// </exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        IExternalProcess externalProcess = _externalProcessFactory.CreateExternalProcess(processConfiguration,
            processExitConfiguration ?? ProcessExitConfiguration.Default);
        
        try
        {
            await externalProcess.StartAsync(cancellationToken);

            return await externalProcess.CaptureBufferedResultAsync(cancellationToken);
        }
        finally
        {
            externalProcess.Dispose();
        }
    }

    /// <summary>
    ///     Runs the process asynchronously with Standard Output and Standard Error Redirection,
    ///     gets Standard Output and Standard Error as Streams, waits for exit, and safely disposes of the
    ///     Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Results from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">
    ///     Thrown if the result validation requires the
    ///     process to exit with exit code zero and the process exits with a different exit code.
    /// </exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? exitConfiguration = null, CancellationToken cancellationToken = default)
    {
        IExternalProcess externalProcess = _externalProcessFactory.CreateExternalProcess(
            processConfiguration, exitConfiguration ??  ProcessExitConfiguration.Default);

        try
        {
            await externalProcess.StartAsync(cancellationToken);

            return await externalProcess.CapturePipedResultAsync(cancellationToken);
        }
        finally
        {
            externalProcess.Dispose();
        }
    }
}
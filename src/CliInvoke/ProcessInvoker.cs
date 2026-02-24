/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes;

using WhatExec.Lib.Abstractions;

namespace CliInvoke;

/// <summary>
/// The default implementation of <see cref="IProcessInvoker"/>, a safer way to execute processes.
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IProcessPipeHandler _processPipeHandler;

    private readonly IExecutableFileResolver _executableFileResolver;

    /// <summary>
    /// Instantiates a <see cref="ProcessInvoker"/> for creating and executing processes.
    /// </summary>
    /// <param name="executableFileResolver">The file path resolver to be used.</param>
    /// <param name="processPipeHandler">The pipe handler to be used for managing the input/output streams of the processes.</param>
    public ProcessInvoker(
        IExecutableFileResolver executableFileResolver,
        IProcessPipeHandler processPipeHandler)
    {
        _executableFileResolver = executableFileResolver;
        _processPipeHandler = processPipeHandler;
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
        processExitConfiguration = await ValidateConfigurationsAsync(processConfiguration, processExitConfiguration, cancellationToken);

        ProcessWrapper process = new(processConfiguration, processConfiguration.ResourcePolicy);

        if (processConfiguration.StandardInput is not null
            && processConfiguration.StandardInput != StreamWriter.Null)
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        try
        {
            process.Start();

            await PipeStandardInputAsync(processConfiguration, process, cancellationToken);

            await process.WaitForExitOrTimeoutAsync(processExitConfiguration, cancellationToken);

            ProcessResult result = new(
                process.ProcessName,
                process.ExitCode, process.Id,
                process.StartTime,
                process.ExitTime
            );

            //TODO: Add support for <see cref="IProcessResultValidator"/>
            if (processExitConfiguration.CancellationExceptionBehavior
                !=  ProcessCancellationExceptionBehavior.SuppressException)
            {
                ThrowProcessNotSuccessfulException(result, processConfiguration);
            }

            return result;
        }
        finally
        {
            DisposeProcessAndConfig(processConfiguration, disposeOfConfig, process);
        }
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
        CancellationToken cancellationToken = default
    )
    {
        processExitConfiguration = await ValidateConfigurationsAsync(processConfiguration, processExitConfiguration, cancellationToken);

        ProcessWrapper process = new(processConfiguration, processConfiguration.ResourcePolicy);

        if (
            processConfiguration.StandardInput is not null
            && processConfiguration.StandardInput != StreamWriter.Null
        )
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        try
        {
            process.Start();
            
            if(processConfiguration.RedirectStandardInput)
                await PipeStandardInputAsync(processConfiguration, process, cancellationToken);

            Task<string> standardOut = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);

            Task waitForExit = process.WaitForExitOrTimeoutAsync(
                processExitConfiguration,
                cancellationToken
            );

            await Task.WhenAll(standardOut, standardError, waitForExit);

            BufferedProcessResult result = new(
                process.ProcessName,
                process.ExitCode,
                process.Id,
                await standardOut,
                await standardError,
                process.StartTime,
                process.ExitTime
            );

            //TODO: Add support for <see cref="IProcessResultValidator"/>

            if (processExitConfiguration.CancellationExceptionBehavior
                != ProcessCancellationExceptionBehavior.SuppressException)
            {
                ThrowProcessNotSuccessfulException(result, processConfiguration);
            }

            DisposeCompletedStreams(standardOut, standardError);

            return result;
        }
        finally
        {
            DisposeProcessAndConfig(processConfiguration, disposeOfConfig, process);
        }
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
        CancellationToken cancellationToken = default
    )
    {
        processExitConfiguration = await ValidateConfigurationsAsync(processConfiguration, processExitConfiguration, cancellationToken);

        ProcessWrapper process = new(processConfiguration, processConfiguration.ResourcePolicy);

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        
        try
        {
            process.Start();
            
            await PipeStandardInputAsync(processConfiguration, process, cancellationToken);

            Task<Stream> standardOutput = _processPipeHandler.PipeStandardOutputAsync(process, cancellationToken);
            Task<Stream> standardError = _processPipeHandler.PipeStandardErrorAsync(process, cancellationToken);

            Task waitForExit = process.WaitForExitOrTimeoutAsync(
                processExitConfiguration,
                cancellationToken
            );

            await Task.WhenAll(standardOutput, standardError, waitForExit);

            PipedProcessResult result = new(
                process.ProcessName,
                process.ExitCode,
                process.Id,
                process.StartTime,
                process.ExitTime,
                await standardOutput,
                await standardError
            );

            //TODO: Add support for <see cref="IProcessResultValidator"/>
            if (processExitConfiguration.
                    CancellationExceptionBehavior != ProcessCancellationExceptionBehavior.SuppressException)
            {
                ThrowProcessNotSuccessfulException(result, processConfiguration);
            }
            
            DisposeCompletedStreams(standardOutput, standardError);

            return result;
        }
        finally
        {
            DisposeProcessAndConfig(processConfiguration, disposeOfConfig, process);
        }
    }

    #region Class private helpers
    private static void DisposeCompletedStreams(Task<string> standardOut, Task<string> standardError)
    {
        if (standardOut.IsCompleted)
            standardOut.Dispose();

        if (standardError.IsCompleted)
            standardError.Dispose();
    }
    
    private void DisposeCompletedStreams(Task<Stream> standardOutput, Task<Stream> standardError)
    {
        if (standardOutput.IsCompleted)
            standardOutput.Dispose();

        if (standardError.IsCompleted)
            standardError.Dispose();
    }

    private static void ThrowProcessNotSuccessfulException(ProcessResult result,
        ProcessConfiguration configuration)
    {
        throw new ProcessNotSuccessfulException(
            new ProcessExceptionInfo(result, configuration)
        );
    }

    private async Task PipeStandardInputAsync(ProcessConfiguration processConfiguration,
        ProcessWrapper process, CancellationToken cancellationToken)
    {
        if (
            processConfiguration.StandardInput is not null
            && processConfiguration.StandardInput != StreamWriter.Null
        )
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        if (process.StartInfo.RedirectStandardInput
            && processConfiguration.StandardInput is not null)
        {
            await _processPipeHandler.PipeStandardInputAsync(
                processConfiguration.StandardInput.BaseStream,
                process, cancellationToken);
        }
    }
    
    private async Task<ProcessExitConfiguration> ValidateConfigurationsAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(processConfiguration);
        
        FileInfo fileInfo = await _executableFileResolver.LocateExecutableAsync(
            processConfiguration.TargetFilePath, SearchOption.AllDirectories, cancellationToken);

        processConfiguration.TargetFilePath = fileInfo.FullName;
        
        processExitConfiguration ??= ProcessExitConfiguration.Default;

        ThrowFileNotFoundException(processConfiguration);
        return processExitConfiguration;
    }

    private void ThrowFileNotFoundException(ProcessConfiguration processConfiguration)
    {
        if (!File.Exists(processConfiguration.TargetFilePath))
        {
            throw new FileNotFoundException(
                Resources.Exceptions_FileNotFound.Replace(
                    "{file}",
                    processConfiguration.TargetFilePath
                )
            );
        }
    }
    
    private static void DisposeProcessAndConfig(ProcessConfiguration processConfiguration,
        bool disposeOfConfig,
        ProcessWrapper process)
    {
        process.Dispose();

        if (disposeOfConfig)
            processConfiguration.Dispose();
    }
    #endregion
}
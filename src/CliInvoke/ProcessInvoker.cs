/*
    AlastairLundy.CliInvoke  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

#nullable enable

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Piping;

using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.CliInvoke.Exceptions;
using AlastairLundy.CliInvoke.Internal.Localizations;

using System.Runtime.Versioning;

using AlastairLundy.CliInvoke.Magic.Processes;

namespace AlastairLundy.CliInvoke;

/// <summary>
/// The default implementation of IProcessInvoker, a safer way to execute processes.
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IProcessPipeHandler _processPipeHandler;
    
    private readonly IFilePathResolver _filePathResolver;

    /// <summary>  
    /// Instantiates an invoker for invoking processes, providing a centralized way to execute external commands.
    /// </summary>
    /// <param name="filePathResolver"></param>
    /// <param name="processPipeHandler">The pipe handler to be used for managing the input/output streams of the processes.</param>
    public ProcessInvoker(IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler)
    {
        _filePathResolver = filePathResolver;
        _processPipeHandler = processPipeHandler;
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file, with the file name of the process to be executed, is not found.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration,
        CancellationToken cancellationToken = default)
    {
        processConfiguration.TargetFilePath = _filePathResolver.ResolveFilePath(processConfiguration.TargetFilePath);
        
        if (processExitConfiguration is null)
            processExitConfiguration = ProcessExitConfiguration.Default;
        
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(
                Resources.Exceptions_FileNotFound.Replace("{file}",
                    processConfiguration.TargetFilePath));
        }

        Process process = new Process()
        {
            StartInfo = processConfiguration.ToProcessStartInfo(
                processConfiguration.StandardOutput is not null &&
                processConfiguration.StandardOutput != StreamReader.Null,
                processConfiguration.StandardError is not null &&
                processConfiguration.StandardError != StreamReader.Null)
        };
        
        if (processConfiguration.StandardInput is not null && processConfiguration.StandardInput != StreamWriter.Null)
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        if (process.StartInfo.RedirectStandardInput && processConfiguration.StandardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(processConfiguration.StandardInput.BaseStream,
                process);
        }

        ProcessResult result;

        try
        {
            process.Start();

            process.SetResourcePolicy(processConfiguration.ResourcePolicy);

            Task waitForExit = processExitConfiguration.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None
                ? process.WaitForExitAsync(cancellationToken)
                : process.WaitForExitOrTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold);

            await waitForExit;
            
             result = new ProcessResult(process.StartInfo.FileName,
                process.ExitCode, process.StartTime, process.ExitTime);

            if (processExitConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
            {
                throw new ProcessNotSuccessfulException(process: process,
                    exitCode: process.ExitCode);
            }
        }
        finally
        {
            process.Dispose();
        }
        
        return result;
    }
    

    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration,
        CancellationToken cancellationToken = default)
    {
        processConfiguration.TargetFilePath = _filePathResolver.ResolveFilePath(processConfiguration.TargetFilePath);
        
        processExitConfiguration ??= ProcessExitConfiguration.Default;
        
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound
                .Replace("{file}",
                    processConfiguration.TargetFilePath));
        }
        
        Process process = new Process()
        {
            StartInfo = processConfiguration.ToProcessStartInfo(true, true),
        };
        
        if (processConfiguration.StandardInput is not null && processConfiguration.StandardInput != StreamWriter.Null)
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        if (process.StartInfo.RedirectStandardInput && processConfiguration.StandardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(processConfiguration.StandardInput.BaseStream,
                process);
        }

        BufferedProcessResult result;

        try
        {
            process.Start();

            process.SetResourcePolicy(processConfiguration.ResourcePolicy);

            Task<string> standardOut = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);

            Task waitForExit = processExitConfiguration.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None
                ? process.WaitForExitAsync(cancellationToken)
                : process.WaitForExitOrTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold);

            await Task.WhenAll(standardOut, standardError, waitForExit);

            result = new BufferedProcessResult(
                process.StartInfo.FileName,
                process.ExitCode,
                await standardOut,
                await standardError,
                process.StartTime,
                process.ExitTime);
        }
        finally
        {
            process.Dispose();
        }
        
        return result;
    }
    
    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Streams, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Results from running the process.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration,
        CancellationToken cancellationToken = default)
    {
        processConfiguration.TargetFilePath = _filePathResolver.ResolveFilePath(processConfiguration.TargetFilePath);
        
        processExitConfiguration ??= ProcessExitConfiguration.Default;

        Process process = new Process()
        {
            StartInfo = processConfiguration.ToProcessStartInfo(true, true)
        };
        
        if (processConfiguration.StandardInput is not null && processConfiguration.StandardInput != StreamWriter.Null)
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        if (process.StartInfo.RedirectStandardInput && processConfiguration.StandardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(processConfiguration.StandardInput.BaseStream,
                process);
        }

        PipedProcessResult result;

        try
        {
            process.Start();

            process.SetResourcePolicy(processConfiguration.ResourcePolicy);


            Task<Stream> standardOutput = _processPipeHandler.PipeStandardOutputAsync(process);
            Task<Stream> standardError = _processPipeHandler.PipeStandardErrorAsync(process);

            Task waitForExit = processExitConfiguration.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None
                ? process.WaitForExitAsync(cancellationToken)
                : process.WaitForExitOrTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold);

            await Task.WhenAll(standardOutput, standardError, waitForExit);

            result = new PipedProcessResult(process.StartInfo.FileName,
            process.ExitCode, process.StartTime, process.ExitTime,
            await standardOutput, await standardError);
        }
        finally
        {
            process.Dispose();
        }
        
        return result;
    }
}
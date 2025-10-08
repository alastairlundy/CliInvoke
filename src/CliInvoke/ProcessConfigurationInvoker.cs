/*
    AlastairLundy.CliInvoke  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

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
using AlastairLundy.CliInvoke.Helpers.Processes;
using AlastairLundy.DotExtensions.Processes;

namespace AlastairLundy.CliInvoke;

/// <summary>
/// The default implementation of <see cref="IProcessConfigurationInvoker"/>, a safer way to execute processes.
/// </summary>
public class ProcessConfigurationInvoker : IProcessConfigurationInvoker
{
    private readonly IProcessPipeHandler _processPipeHandler;
    
    private readonly IFilePathResolver _filePathResolver;

    /// <summary>  
    /// Instantiates a <see cref="ProcessConfigurationInvoker"/> for creating and executing processes.
    /// </summary>
    /// <param name="filePathResolver">The file path resolver to be used.</param>
    /// <param name="processPipeHandler">The pipe handler to be used for managing the input/output streams of the processes.</param>
    public ProcessConfigurationInvoker(IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler)
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
        
        processExitConfiguration ??= ProcessExitConfiguration.Default;
        
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(
                Resources.Exceptions_FileNotFound.Replace("{file}",
                    processConfiguration.TargetFilePath));
        }

        Process process = new Process()
        {
            StartInfo = processConfiguration.ToProcessStartInfo(false,
                false),
            EnableRaisingEvents = true
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

        try
        {
            process.Start();
            
            if(process.HasStarted() && process.HasExited() == false)
                process.SetResourcePolicy(processConfiguration.ResourcePolicy);

            await process.WaitForExitOrTimeoutAsync(processExitConfiguration, cancellationToken);
            
             ProcessResult result = new ProcessResult(process.StartInfo.FileName,
                 process.ExitCode, process.StartTime, process.ExitTime);

            if (processExitConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
            {
                throw new ProcessNotSuccessfulException(process: process,
                    exitCode: process.ExitCode);
            }
            
            return result;
        }
        finally
        {
            process.Dispose();
        }
    }
    

    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
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
            StartInfo = processConfiguration.ToProcessStartInfo(true,
                true),
            EnableRaisingEvents = true
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

        try
        {
            process.Start();

            if(process.HasStarted() && process.HasExited() == false)
                process.SetResourcePolicy(processConfiguration.ResourcePolicy);

            Task<string> standardOut = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);

            Task waitForExit = process.WaitForExitOrTimeoutAsync(processExitConfiguration, cancellationToken);

            await Task.WhenAll(standardOut, standardError, waitForExit);

            BufferedProcessResult result = new BufferedProcessResult(
                process.StartInfo.FileName,
                process.ExitCode,
                await standardOut,
                await standardError,
                process.StartTime,
                process.ExitTime);
            
            if (processExitConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
            {
                throw new ProcessNotSuccessfulException(process: process,
                    exitCode: process.ExitCode);
            }
            
            if(standardOut.IsCompleted)
                standardOut.Dispose();
            
            if(standardError.IsCompleted)
                standardError.Dispose();
            
            return result;
        }
        finally
        {
            process.Dispose();
        }
    }
    
    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Streams, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Results from running the process.</returns>
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
    public async Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration,
        CancellationToken cancellationToken = default)
    {
        processConfiguration.TargetFilePath = _filePathResolver.ResolveFilePath(processConfiguration.TargetFilePath);
        
        processExitConfiguration ??= ProcessExitConfiguration.Default;

        Process process = new Process()
        {
            StartInfo = processConfiguration.ToProcessStartInfo(true,
                true),
            EnableRaisingEvents = true
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
        
        try
        {
            process.Start();

            if(process.HasStarted() && process.HasExited() == false)
                process.SetResourcePolicy(processConfiguration.ResourcePolicy);

            Task<Stream> standardOutput = _processPipeHandler.PipeStandardOutputAsync(process);
            Task<Stream> standardError = _processPipeHandler.PipeStandardErrorAsync(process);

            Task waitForExit = process.WaitForExitOrTimeoutAsync(processExitConfiguration, cancellationToken);

            await Task.WhenAll(standardOutput, standardError, waitForExit);

            PipedProcessResult result = new PipedProcessResult(process.StartInfo.FileName,
                process.ExitCode, process.StartTime, process.ExitTime,
                await standardOutput, await standardError);
            
            if (processExitConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
            {
                throw new ProcessNotSuccessfulException(process: process,
                    exitCode: process.ExitCode);
            }
            
            if(standardOutput.IsCompleted)
                standardOutput.Dispose();
            
            if(standardError.IsCompleted)
                standardError.Dispose();
            
            return result;
        }
        finally
        {
            process.Dispose();
        }
    }
}
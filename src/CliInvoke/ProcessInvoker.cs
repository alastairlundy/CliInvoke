/*
    AlastairLundy.CliInvoke  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Piping;
using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.CliInvoke.Exceptions;
using AlastairLundy.CliInvoke.Helpers.Processes;

using AlastairLundy.DotExtensions.Processes;

namespace AlastairLundy.CliInvoke;

/// <summary>
/// The default implementation of <see cref="IProcessInvoker"/>,
/// an interface that creates and runs Process objects from <see cref="ProcessStartInfo"/> objects.
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IFilePathResolver _filePathResolver;
    private readonly IProcessPipeHandler _processPipeHandler;


    /// <summary>  
    /// Instantiates a <see cref="ProcessInvoker"/> for creating and executing processes.
    /// </summary>
    /// <param name="filePathResolver">The file path resolver to be used.</param>
    /// <param name="processPipeHandler">The pipe handler to be used for managing the input/output streams of the processes.</param>
    public ProcessInvoker(IFilePathResolver filePathResolver,
        IProcessPipeHandler processPipeHandler)
    {
        _filePathResolver = filePathResolver;
        _processPipeHandler = processPipeHandler;
    }
    
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
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<ProcessResult> ExecuteAsync(ProcessStartInfo startInfo,
        ProcessResourcePolicy processResourcePolicy,
        ProcessTimeoutPolicy processTimeoutPolicy, ProcessResultValidation processResultValidation,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        startInfo.FileName = _filePathResolver.ResolveFilePath(startInfo.FileName);
        
        Process process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };
        
        if (process.StartInfo.RedirectStandardInput && standardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream,
                process);
        }

        ProcessResult result;
        
        try
        {
            process.Start();
            
            if(process.HasStarted() && process.HasExited() == false)
                process.SetResourcePolicy(processResourcePolicy);
            
            await process.WaitForExitOrTimeoutAsync(new ProcessExitConfiguration(processTimeoutPolicy, processResultValidation,
                ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected), cancellationToken: cancellationToken);
            
            result = new ProcessResult(process.StartInfo.FileName,
                process.ExitCode, process.StartTime, process.ExitTime);

            if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
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
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="startInfo">The start info to use for <see cref="Process"/> creation.</param>
    /// <param name="processResourcePolicy">The resource policy to use for <see cref="Process"/> creation.</param>
    /// <param name="processTimeoutPolicy">The timeout policy to use when waiting for <see cref="Process"/> exit.</param>
    /// <param name="processResultValidation"></param>
    /// <param name="standardInput">The standard input to pipe to the Process, if specified.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessStartInfo startInfo,
        ProcessResourcePolicy processResourcePolicy,
        ProcessTimeoutPolicy processTimeoutPolicy, ProcessResultValidation processResultValidation,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        startInfo.FileName = _filePathResolver.ResolveFilePath(startInfo.FileName);

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        
        Process process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };
        
        if (process.StartInfo.RedirectStandardInput && standardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream,
                process);
        }
        
        BufferedProcessResult result;

        try
        {
            process.Start();
            
            if(process.HasStarted() && process.HasExited() == false)
                process.SetResourcePolicy(processResourcePolicy);
            
            Task waitForExit = process.WaitForExitOrTimeoutAsync(new ProcessExitConfiguration(processTimeoutPolicy, processResultValidation,
                ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected), cancellationToken: cancellationToken);

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(waitForExit, standardOutputTask, standardErrorTask);
            
            result = new BufferedProcessResult(process.StartInfo.FileName,
                process.ExitCode, await standardOutputTask,
                await standardErrorTask,
                 process.StartTime, process.ExitTime);

            if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
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
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<PipedProcessResult> ExecutePipedAsync(ProcessStartInfo startInfo,
        ProcessResourcePolicy processResourcePolicy,
        ProcessTimeoutPolicy processTimeoutPolicy, ProcessResultValidation processResultValidation,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        startInfo.FileName = _filePathResolver.ResolveFilePath(startInfo.FileName);
        
        Process process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };
        
        if (process.StartInfo.RedirectStandardInput && standardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream,
                process);
        }
        
        PipedProcessResult result;

        try
        {
            process.Start();

            if(process.HasStarted() && process.HasExited() == false)
                process.SetResourcePolicy(processResourcePolicy);

            Task<Stream> standardOutput = _processPipeHandler.PipeStandardOutputAsync(process);
            Task<Stream> standardError = _processPipeHandler.PipeStandardErrorAsync(process);

            Task waitForExit = process.WaitForExitOrTimeoutAsync(new ProcessExitConfiguration(processTimeoutPolicy, processResultValidation,
                ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected), cancellationToken: cancellationToken);
            
            await Task.WhenAll(standardOutput, standardError, waitForExit);

            result = new PipedProcessResult(process.StartInfo.FileName,
                process.ExitCode, process.StartTime, process.ExitTime,
                await standardOutput, await standardError);
            
            if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
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
}
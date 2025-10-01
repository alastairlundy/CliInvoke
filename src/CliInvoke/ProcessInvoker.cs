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
using AlastairLundy.CliInvoke.Internal.Processes;

namespace AlastairLundy.CliInvoke;

/// <summary>
/// 
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IFilePathResolver _filePathResolver;
    private readonly IProcessPipeHandler _processPipeHandler;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathResolver"></param>
    /// <param name="processPipeHandler"></param>
    public ProcessInvoker(IFilePathResolver filePathResolver,
        IProcessPipeHandler processPipeHandler)
    {
        _filePathResolver = filePathResolver;
        _processPipeHandler = processPipeHandler;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="startInfo"></param>
    /// <param name="processResourcePolicy"></param>
    /// <param name="processTimeoutPolicy"></param>
    /// <param name="processResultValidation"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
            
            await process.WaitForExitOrTimeoutAsync(processTimeoutPolicy, cancellationToken);
            
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
    /// 
    /// </summary>
    /// <param name="startInfo"></param>
    /// <param name="processResourcePolicy"></param>
    /// <param name="processTimeoutPolicy"></param>
    /// <param name="processResultValidation"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
            
            Task waitForExit = process.WaitForExitOrTimeoutAsync(processTimeoutPolicy, cancellationToken);
            
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
    /// 
    /// </summary>
    /// <param name="startInfo"></param>
    /// <param name="processResourcePolicy"></param>
    /// <param name="processTimeoutPolicy"></param>
    /// <param name="processResultValidation"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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

            Task waitForExit = process.WaitForExitOrTimeoutAsync(processTimeoutPolicy,
                cancellationToken);

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
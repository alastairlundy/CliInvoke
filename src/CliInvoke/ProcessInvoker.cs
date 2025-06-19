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
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Extensions.Processes;
using AlastairLundy.CliInvoke.Core.Piping.Abstractions;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Exceptions;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

using AlastairLundy.CliInvoke.Internal.Localizations;

namespace AlastairLundy.CliInvoke;

/// <summary>
/// The default implementation of IProcessInvoker, a safer way to execute processes.
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IProcessPipeHandler _processPipeHandler;
    
    private readonly IProcessFactory _processFactory;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processFactory"></param>
    public ProcessInvoker(IProcessFactory processFactory, IProcessPipeHandler processPipeHandler)
    {
        _processFactory = processFactory;
        _processPipeHandler = processPipeHandler;
    }

    /// <summary>
    /// Runs the process synchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file, with the file name of the process to be executed, is not found.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif
    public async Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(
                Resources.Exceptions_FileNotFound.Replace("{file}", processConfiguration.TargetFilePath));
        }
        
        Process process = _processFactory.StartNew(processConfiguration);

        ProcessResult result = await _processFactory.ContinueWhenExitAsync(process,
            processConfiguration.ResultValidation, cancellationToken);
       
        if (processConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process, exitCode: process.ExitCode);
        }

        return result;
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processResultValidation">The process result validation to be used.</param>
    /// <param name="processResourcePolicy">The process resource policy to be set if not null.</param>
    /// <param name="userCredential"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from the running the process.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file, with the file name of the process to be executed, is not found.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif
    public async Task<ProcessResult> ExecuteAsync(ProcessStartInfo processStartInfo,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null,
        UserCredential? userCredential = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(processStartInfo.FileName) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound
                .Replace("{file}", processStartInfo.FileName));
        }
        
        Process process = _processFactory.From(processStartInfo, 
            userCredential ?? UserCredential.Null);
        
        if (processStartInfo.RedirectStandardInput && standardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream, process);
        }

        process.Start();
        
        if(processResourcePolicy is not null)
            process.SetResourcePolicy(processResourcePolicy);

        
        ProcessResult result =
            await _processFactory.ContinueWhenExitAsync(process, processResultValidation, cancellationToken);

        return result;
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound
                .Replace("{file}", processConfiguration.TargetFilePath));
        }

        ProcessStartInfo startInfo = processConfiguration.ToProcessStartInfo(processConfiguration.StandardInput is not null,
            true, true);

        Process process = _processFactory.StartNew(processConfiguration);
                              
        BufferedProcessResult result = await _processFactory.ContinueWhenExitBufferedAsync(process, processConfiguration.ResultValidation,
            cancellationToken);
                              
        return result;
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processResultValidation">The process result validation to be used.</param>
    /// <param name="processResourcePolicy">The resource policy to be set if not null.</param>
    /// <param name="userCredential"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file, with the file name of the process to be executed, is not found.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessStartInfo processStartInfo,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null,
        UserCredential? userCredential = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        processStartInfo.RedirectStandardInput = standardInput is not null;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;
        
       Process process = _processFactory.From(processStartInfo,
           userCredential ?? UserCredential.Null);

       if (processStartInfo.RedirectStandardInput && standardInput is not null)
       {
           process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream, process);
       }

       process.Start();
        
       if(processResourcePolicy is not null)
           process.SetResourcePolicy(processResourcePolicy);

        BufferedProcessResult result =
            await _processFactory.ContinueWhenExitBufferedAsync(process, processResultValidation, cancellationToken);

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif
    public async Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = processConfiguration.ToProcessStartInfo(
            processConfiguration.StandardInput is not null,
            true, true);
        
        Process process = _processFactory.StartNew(startInfo);

        PipedProcessResult result = await _processFactory.ContinueWhenExitPipedAsync(process, processConfiguration.ResultValidation,
            cancellationToken);
        
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processResultValidation"></param>
    /// <param name="processResourcePolicy"></param>
    /// <param name="userCredential"></param>
    /// <param name="standardInput"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif
    public async Task<PipedProcessResult> ExecutePipedAsync(ProcessStartInfo processStartInfo,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null, UserCredential? userCredential = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(processStartInfo.FileName) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound
                .Replace("{file}", processStartInfo.FileName));
        }
        
        processStartInfo.RedirectStandardOutput = standardInput is not null;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;
        
        Process process = _processFactory.From(processStartInfo, 
            userCredential ?? UserCredential.Null);

        if (processStartInfo.RedirectStandardInput && standardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream, process);
        }

        process.Start();
        
        if(processResourcePolicy is not null)
            process.SetResourcePolicy(processResourcePolicy);
        
        PipedProcessResult result =
            await _processFactory.ContinueWhenExitPipedAsync(process, processResultValidation, cancellationToken);

        return result;
    }
}
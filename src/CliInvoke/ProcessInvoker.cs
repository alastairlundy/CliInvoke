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

using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Abstractions.Piping;
using AlastairLundy.CliInvoke.Core.Extensions.Processes;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Exceptions;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

using AlastairLundy.CliInvoke.Internal.Localizations;


#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AlastairLundy.CliInvoke;

/// <summary>
/// The default implementation of IProcessInvoker, a safer way to execute processes.
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IProcessPipeHandler _processPipeHandler;
    
    private readonly IProcessFactory _processFactory;

    /// <summary>  
    /// Instantiates an invoker for invoking processes, providing a centralized way to execute external commands.
    /// </summary>
    /// <param name="processFactory">The process factory to be used to create and run the invoked processes.</param>
    /// <param name="processPipeHandler">The pipe handler to be used for managing the input/output streams of the processes.</param>
    public ProcessInvoker(IProcessFactory processFactory, IProcessPipeHandler processPipeHandler)
    {
        _processFactory = processFactory;
        _processPipeHandler = processPipeHandler;
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="process"></param>
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
    public async Task<ProcessResult> ExecuteProcessAsync(Process process, ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(
                Resources.Exceptions_FileNotFound.Replace("{file}",
                    processConfiguration.TargetFilePath));
        }
        
        ProcessResult result = await _processFactory.ContinueWhenExitAsync(process,
            processConfiguration.ResultValidation,
            cancellationToken: cancellationToken);
       
        if (processConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process,
                exitCode: process.ExitCode);
        }

        return result;
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="process"></param>
    /// <param name="processResultValidation">The process result validation to be used.</param>
    /// <param name="processResourcePolicy">The process resource policy to be set if not null.</param>
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
    public async Task<ProcessResult> ExecuteProcessAsync(Process process, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null, CancellationToken cancellationToken = default)
    {
        if (File.Exists(process.StartInfo.FileName) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound
                .Replace("{file}",
                    process.StartInfo.FileName));
        }

        process.Start();
        
        if(processResourcePolicy is not null)
            process.SetResourcePolicy(processResourcePolicy);

        
        ProcessResult result =
            await _processFactory.ContinueWhenExitAsync(process,
                processResultValidation,
                cancellationToken: cancellationToken);

        return result;
    }

    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process, 
        ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound
                .Replace("{file}",
                    processConfiguration.TargetFilePath));
        }

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

                            
        BufferedProcessResult result = await _processFactory.ContinueWhenExitBufferedAsync(process,
            processConfiguration.ResultValidation,
            cancellationToken);
                              
        return result;
    }
    
    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="process"></param>
    /// <param name="processResultValidation">The process result validation to be used.</param>
    /// <param name="processResourcePolicy">The resource policy to be set if not null.</param>
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
    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process, 
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null, 
        CancellationToken cancellationToken = default)
    {
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.Start();
        
        if(processResourcePolicy is not null)
            process.SetResourcePolicy(processResourcePolicy);

        BufferedProcessResult result =
            await _processFactory.ContinueWhenExitBufferedAsync(process,
                processResultValidation,
                cancellationToken);

        return result;
    }
}
/*
    AlastairLundy.CliInvoke  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Abstractions.Legacy.Utilities;
using AlastairLundy.CliInvoke.Core.Extensions.Processes;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Exceptions;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;
using AlastairLundy.CliInvoke.Internal;
using AlastairLundy.CliInvoke.Internal.Localizations;

namespace AlastairLundy.CliInvoke.Legacy;

/// <summary>
/// The default implementation of IProcessRunner, a safer way to execute processes.
/// </summary>
[Obsolete(DeprecationMessages.ClassDeprecationV2)]
public class ProcessRunner : IProcessInvoker
{
    private readonly IProcessRunnerUtility? _processRunnerUtils;

    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public ProcessRunner()
    {
        _processRunnerUtils = null;
    }
    
    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public ProcessRunner(IProcessRunnerUtility processRunnerUtils)
    {
        _processRunnerUtils = processRunnerUtils;
    }
    
    /// <summary>
    /// Runs the process synchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <remarks>Use the Async version of this method where possible to avoid UI freezes and other potential issues.</remarks>
    /// <param name="process">The process to be run.</param>
    /// <param name="processResultValidation">The process result validation to be used.</param>
    /// <param name="processResourcePolicy">The process resource policy to be set if it is not null.</param>
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
    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public ProcessResult ExecuteProcess(Process process, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null)
    {
        if (File.Exists(process.StartInfo.FileName) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{file}", process.StartInfo.FileName));
        }
        
        if(_processRunnerUtils is not null)
            _processRunnerUtils.Execute(process, processResultValidation, processResourcePolicy);
        else
        {
            process.Start();
            process.SetResourcePolicy(processResourcePolicy);
            process.WaitForExit();
        }
        
        if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process, exitCode: process.ExitCode);
        }

        if(_processRunnerUtils is not null)
            return _processRunnerUtils.GetResult(process, disposeOfProcess: true);
        else
        {
            ProcessResult result = new ProcessResult(process.StartInfo.FileName,
                process.ExitCode, process.StartTime, process.ExitTime);
            
            process.Dispose();

            return result;
        }
    }

    /// <summary>
    /// Runs the process synchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <remarks>Use the Async version of this method where possible to avoid UI freezes and other potential issues.</remarks>
    /// <param name="process">The process to be run.</param>
    /// <param name="processResultValidation">The process result validation to be used.</param>
    /// <param name="processResourcePolicy">The process resource policy to set if it is not null.</param>
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
    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public BufferedProcessResult ExecuteBufferedProcess(Process process,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null)
    {
        if (File.Exists(process.StartInfo.FileName) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{file}", process.StartInfo.FileName));
        }
        
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        
        if(_processRunnerUtils is not null)
            _processRunnerUtils.Execute(process, processResultValidation, processResourcePolicy);
        else
        {
            process.Start();
            process.SetResourcePolicy(processResourcePolicy);
            
            process.WaitForExit();
        }
        
        if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process, exitCode: process.ExitCode);
        }
        
        if(_processRunnerUtils is not null)
            return _processRunnerUtils.GetBufferedResult(process, disposeOfProcess: true);
        else
        {
            BufferedProcessResult result = new BufferedProcessResult(process.StartInfo.FileName,
                process.ExitCode, process.StandardOutput.ReadToEnd(),
                process.StandardError.ReadToEnd(),
                process.StartTime, process.ExitTime);
            
            process.Dispose();

            return result;
        }
    }

    /// <summary>
    /// Runs the process synchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="process">The process to be run.</param>
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
    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public async Task<ProcessResult> ExecuteProcessAsync(Process process, ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(process.StartInfo.FileName) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{file}", process.StartInfo.FileName));
        }
        
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (_processRunnerUtils is not null)
            await _processRunnerUtils.ExecuteAsync(process, processConfiguration.ResultValidation,
                processConfiguration.ResourcePolicy, cancellationToken);
        else
        {
            process.Start();
            process.SetResourcePolicy(processConfiguration.ResourcePolicy);
        }
        
        if (processConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process, exitCode: process.ExitCode);
        }
        
        if(_processRunnerUtils is not null)
           return await _processRunnerUtils.GetBufferedResultAsync(process, disposeOfProcess: true);
        else
        {
           Task waitForExit = process.WaitForExitAsync(cancellationToken);
           
           Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
           Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
           
           await Task.WhenAll(waitForExit, standardOutput, standardError);

           BufferedProcessResult result = new BufferedProcessResult(process.StartInfo.FileName,
            process.ExitCode, await standardOutput, await standardError,
            process.StartTime, process.ExitTime);
           
           process.Dispose();
           
           return result;
        }
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="process">The process to be run.</param>
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
    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public async Task<ProcessResult> ExecuteProcessAsync(Process process,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null,
        CancellationToken cancellationToken = default)
    {
        if (_processRunnerUtils is not null)
        {
            await _processRunnerUtils.ExecuteAsync(process, processResultValidation, processResourcePolicy , cancellationToken);
            
            return await _processRunnerUtils.GetBufferedResultAsync(process, disposeOfProcess: true);
        }
        else
        {
            process.Start();
            process.SetResourcePolicy(processResourcePolicy ?? ProcessResourcePolicy.Default);
            
            await process.WaitForExitAsync(cancellationToken);
            
            if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
            {
                throw new ProcessNotSuccessfulException(process.ExitCode, process);
            }
            
            ProcessResult result = new ProcessResult(process.StartInfo.FileName,
                process.ExitCode, process.StartTime, process.ExitTime);
           
            process.Dispose();
           
            return result;
        }
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="process">The process to be run.</param>
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
    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process,
        ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        
        if (_processRunnerUtils is not null)
        {
            await _processRunnerUtils.ExecuteAsync(process, processConfiguration.ResultValidation,
                processConfiguration.ResourcePolicy,
                cancellationToken);
            
            return await _processRunnerUtils.GetBufferedResultAsync(process, disposeOfProcess: true);
        }
        else
        {
            process.Start();
            process.SetResourcePolicy(processConfiguration.ResourcePolicy);
            
            Task waitForExit = process.WaitForExitAsync(cancellationToken);
           
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
           
            await Task.WhenAll(waitForExit, standardOutput, standardError);

            if (processConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
            {
                throw new ProcessNotSuccessfulException(process.ExitCode, process);
            }
            
            BufferedProcessResult result = new BufferedProcessResult(process.StartInfo.FileName,
                process.ExitCode, await standardOutput, await standardError,
                process.StartTime, process.ExitTime);
           
            process.Dispose();
           
            return result;
        }
    }


    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="process">The process to be run.</param>
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
    [Obsolete(DeprecationMessages.ClassDeprecationV2)]
    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null,
        CancellationToken cancellationToken = default)
    {
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        
        if (_processRunnerUtils is not null)
        {
            await _processRunnerUtils.ExecuteAsync(process, processResultValidation, processResourcePolicy, cancellationToken);
            
            return await _processRunnerUtils.GetBufferedResultAsync(process, disposeOfProcess: true);
        }
        else
        {
            process.Start();
            process.SetResourcePolicy(processResourcePolicy ?? ProcessResourcePolicy.Default);
            
            Task waitForExit = process.WaitForExitAsync(cancellationToken);
           
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
           
            await Task.WhenAll(waitForExit, standardOutput, standardError);

            if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
            {
                throw new ProcessNotSuccessfulException(process.ExitCode, process);
            }
            
            BufferedProcessResult result = new BufferedProcessResult(process.StartInfo.FileName,
                process.ExitCode, await standardOutput, await standardError,
                process.StartTime, process.ExitTime);
           
            process.Dispose();
           
            return result;
        }
    }
}
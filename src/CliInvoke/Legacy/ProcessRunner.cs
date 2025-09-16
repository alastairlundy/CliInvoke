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
    private readonly IProcessRunnerUtility _processRunnerUtils;
    
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

        _processRunnerUtils.Execute(process, processResultValidation, processResourcePolicy);

        if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process, exitCode: process.ExitCode);
        }

        return _processRunnerUtils.GetResult(process, disposeOfProcess: true);
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
        
        _processRunnerUtils.Execute(process, processResultValidation, processResourcePolicy);
       
        if (processResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process, exitCode: process.ExitCode);
        }
        
        return _processRunnerUtils.GetBufferedResult(process, disposeOfProcess: true);
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
        
        await _processRunnerUtils.ExecuteAsync(process, processConfiguration.ResultValidation, processConfiguration.ResourcePolicy, cancellationToken);
       
        if (processConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process, exitCode: process.ExitCode);
        }
        
        return await _processRunnerUtils.GetBufferedResultAsync(process, disposeOfProcess: true);
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
        await _processRunnerUtils.ExecuteAsync(process, processResultValidation, processResourcePolicy , cancellationToken);
       
        return await _processRunnerUtils.GetResultAsync(process, disposeOfProcess: true);
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
        
        await _processRunnerUtils.ExecuteAsync(process, processConfiguration.ResultValidation,
            processConfiguration.ResourcePolicy,
            cancellationToken);
        
        return await _processRunnerUtils.GetBufferedResultAsync(process, disposeOfProcess: true);
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
        
        await _processRunnerUtils.ExecuteAsync(process, processResultValidation, processResourcePolicy, cancellationToken);
        
        return await _processRunnerUtils.GetBufferedResultAsync(process, disposeOfProcess: true);
    }
}
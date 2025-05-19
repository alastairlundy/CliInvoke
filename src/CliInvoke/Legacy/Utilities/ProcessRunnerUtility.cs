/*
    AlastairLundy.Extensions.Processes  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

#if NETSTANDARD2_0 || NETSTANDARD2_1
#else
using System.Runtime.Versioning;
#endif
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Core.Abstractions.Legacy.Utilities;
using AlastairLundy.CliInvoke.Core.Extensions.Processes;
using AlastairLundy.CliInvoke.Core.Primitives.Exceptions;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;
using AlastairLundy.DotExtensions.Processes;
using AlastairLundy.Extensions.IO.Files.Abstractions;

namespace AlastairLundy.CliInvoke.Legacy.Utilities;

/// <summary>
/// A Process Running Utility to easily create different Process Runners.
/// </summary>
/// <remarks>This class is primarily intended for internal use OR use when creating a Process Runner or Command Runner implementation.</remarks>
[Obsolete]
public class ProcessRunnerUtility : IProcessRunnerUtility
{
    private readonly IFilePathResolver _filePathResolver;
    
    public ProcessRunnerUtility(IFilePathResolver filePathResolver)
    {
        _filePathResolver = filePathResolver;
    }
    
    /// <summary>
    /// Starts a Process and synchronously waits for it to exit before returning.
    /// </summary>
    /// <param name="process">The process to be executed.</param>
    /// <exception cref="InvalidOperationException">Thrown if the specified process has not exited.</exception>
    /// <returns>The process' exit code.</returns>
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
    public int Execute(Process process)
    {
        return Execute(process, ProcessResultValidation.None);
    }

    /// <summary>
    /// Starts a Process and synchronously waits for it to exit before returning.
    /// </summary>
    /// <param name="process">The process to be executed.</param>
    /// <param name="processResultValidation">Whether validation should be performed on the exit code.</param>
    /// <param name="processResourcePolicy">The process resource policy to be set if it is not null.</param>
    /// <exception cref="InvalidOperationException">Thrown if the specified process has not exited.</exception>
    /// <returns>The process' exit code.</returns>
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
    public int Execute(Process process, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null)
    {
        _filePathResolver.ResolveFilePath(process.StartInfo.FileName, out string resolvedFilePath);
        process.StartInfo.FileName = resolvedFilePath;
        
            if (process.HasStarted() == false)
            {
                process.Start();
            }

            if (processResourcePolicy != null)
            {
                process.SetResourcePolicy(processResourcePolicy);
            }

            process.WaitForExit();

            return process.ExitCode;
    }

    /// <summary>
    /// Starts a Process and asynchronously waits for it to exit before returning.
    /// </summary>
    /// <param name="process">The process to be executed.</param>
    /// <param name="cancellationToken">The cancellation token to use to cancel the waiting for process exit if required.</param>
    /// <exception cref="InvalidOperationException">Thrown if the specified process has not exited.</exception>
    /// <returns>The process' exit code.</returns>
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
    public async Task<int> ExecuteAsync(Process process, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(process, ProcessResultValidation.None,null, cancellationToken);
    }

    /// <summary>
    /// Starts a Process and asynchronously waits for it to exit before returning.
    /// </summary>
    /// <param name="process">The process to be executed.</param>
    /// <param name="processResourcePolicy">The process resource policy to be set for the Process.</param>
    /// <param name="cancellationToken">The cancellation token to use to cancel the waiting for process exit if required.</param>
    /// <param name="processResultValidation">Whether validation should be performed on the exit code.</param>
    /// <returns>The process' exit code.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified process has not exited.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the process result validation was performed and the exit code is not zero.</exception>
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
    public async Task<int> ExecuteAsync(Process process,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy? processResourcePolicy = null,
        CancellationToken cancellationToken = default)
    {
        _filePathResolver.ResolveFilePath(process.StartInfo.FileName, out string resolvedFilePath);
        process.StartInfo.FileName = resolvedFilePath;

        if (process.HasStarted() == false)
        {
            process.Start();
        }

        if (processResourcePolicy != null)
        {
            process.SetResourcePolicy(processResourcePolicy);
        }
            
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0 && processResultValidation == ProcessResultValidation.ExitCodeZero)
        {
            throw new ProcessNotSuccessfulException(exitCode: process.ExitCode, process: process);
        }
            
        return process.ExitCode;
    }

    /// <summary>
    /// Disposes of the specified process.
    /// </summary>
    /// <param name="process">The process to be disposed of.</param>
    public void DisposeOfProcess(Process process)
    {
        if (process.HasExited == false)
        {
            process.Kill();
        }
        
        process.Close();
        process.Dispose();
    }
    
    /// <summary>
    /// Gets the results from an exited Process.
    /// </summary>
    /// <param name="process">The process to retrieve results from.</param>
    /// <param name="disposeOfProcess">Whether to dispose of the Process before returning.</param>
    /// <returns>The results from an exited process.</returns>
    public ProcessResult GetResult(Process process, bool disposeOfProcess)
    {
        if (process.HasStarted() == false)
        {
            if (Process.GetProcesses().Any(x => x.Equals(process)) == false)
            {
                _filePathResolver.ResolveFilePath(process.StartInfo.FileName, out string resolvedFilePath);
                
                if (resolvedFilePath.Equals(process.StartInfo.FileName) == false)
                {
                    process.StartInfo.FileName = resolvedFilePath;    
                }
                
                process.Start();
            }
            
            process.WaitForExit();
        }
        
        ProcessResult processResult = new ProcessResult(process.StartInfo.FileName, process.ExitCode, process.StartTime,
            process.ExitTime);

        if (disposeOfProcess)
        {
            DisposeOfProcess(process);
        }
        
        return processResult;
    }

    /// <summary>
    /// Gets the BufferedProcessResults results from an exited Process.
    /// </summary>
    /// <param name="process">The process to retrieve results from.</param>
    /// <param name="disposeOfProcess">Whether to dispose of the Process before returning.</param>
    /// <returns>The results from an exited process.</returns>
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
    public BufferedProcessResult GetBufferedResult(Process process, bool disposeOfProcess)
    {
        if (process.HasStarted() == false)
        {
            if (Process.GetProcesses().Any(x => x.Equals(process)) == false)
            {
                _filePathResolver.ResolveFilePath(process.StartInfo.FileName, out string resolvedFilePath);

                if (resolvedFilePath.Equals(process.StartInfo.FileName) == false)
                {
                    process.StartInfo.FileName = resolvedFilePath;    
                }
                
                process.Start();
            }
            
            process.WaitForExit();
        }
        
        BufferedProcessResult processResult = new BufferedProcessResult(
            process.StartInfo.FileName, process.ExitCode,
             process.StandardOutput.ReadToEnd(),  process.StandardError.ReadToEnd(),
            process.StartTime, process.ExitTime);

        if (disposeOfProcess)
        {
            DisposeOfProcess(process);
        }
        
        return processResult;
    }

    /// <summary>
    /// Asynchronously gets the ProcessResult results from an exited Process.
    /// </summary>
    /// <param name="process">The process to retrieve results from.</param>
    /// <param name="disposeOfProcess">Whether to dispose of the Process before returning.</param>
    /// <returns>The results from an exited process.</returns>
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
    public async Task<ProcessResult> GetResultAsync(Process process, bool disposeOfProcess)
    {
        if (process.HasStarted() == false)
        {
            if (Process.GetProcesses().Any(x => x.Equals(process)) == false)
            {
               await ExecuteAsync(process);
            }
            else
            {
                await process.WaitForExitAsync();
            }
        }
        
        ProcessResult processResult = new ProcessResult(process.StartInfo.FileName, process.ExitCode, process.StartTime,
            process.ExitTime);

        if (disposeOfProcess)
        {
            DisposeOfProcess(process);
        }
        
        return processResult;
    }

    /// <summary>
    /// Asynchronously gets the BufferedProcessResult results from an exited Process.
    /// </summary>
    /// <param name="process">The process to retrieve results from.</param>
    /// <param name="disposeOfProcess">Whether to dispose of the Process before returning.</param>
    /// <returns>The results from an exited process.</returns>
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
    public async Task<BufferedProcessResult> GetBufferedResultAsync(Process process, bool disposeOfProcess)
    {
        if (process.HasStarted() == false)
        {
            if (Process.GetProcesses().Contains(process) == false)
            {
                process.Start();
            }
            
            await process.WaitForExitAsync();
        }
        
        BufferedProcessResult processResult = new BufferedProcessResult(
            process.StartInfo.FileName, process.ExitCode,
            await process.StandardOutput.ReadToEndAsync(),
            await process.StandardError.ReadToEndAsync(),
            process.StartTime, process.ExitTime);

        if (disposeOfProcess)
        {
            DisposeOfProcess(process);
        }
        
        return processResult;
    }
}
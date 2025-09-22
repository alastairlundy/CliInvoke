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
using AlastairLundy.CliInvoke.Internal;

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
    /// <param name="processExitInfo"></param>
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
        ProcessExitConfiguration? processExitInfo,
        CancellationToken cancellationToken = default)
    {
        processConfiguration.TargetFilePath = _filePathResolver.ResolveFilePath(processConfiguration.TargetFilePath);
        
        if (processExitInfo is null)
            processExitInfo = ProcessExitConfiguration.Default;
        
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(
                Resources.Exceptions_FileNotFound.Replace("{file}",
                    processConfiguration.TargetFilePath));
        }

        Process process = new Process();
        
        process.ApplyProcessConfiguration(processConfiguration, 
            processConfiguration.StandardOutput is not null && processConfiguration.StandardOutput != StreamReader.Null,
            processConfiguration.StandardError is not null && processConfiguration.StandardError != StreamReader.Null);
        
        process.Start();
        
        process.SetResourcePolicy(processConfiguration.ResourcePolicy);

        await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        
        ProcessResult result = new ProcessResult(process.StartInfo.FileName,
            process.ExitCode, process.StartTime, process.ExitTime);
       
        if (processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero && process.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(process: process,
                exitCode: process.ExitCode);
        }

        process.Dispose();
        
        return result;
    }

    //TODO: Implement
    public Task<ProcessResult> ExecuteAsync(ProcessStartInfo processStartInfo, ProcessExitConfiguration? processExitInfo = null,
        StreamWriter? standardInput = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processExitInfo"></param>
    /// <param name="processResourcePolicy">The process resource policy to be set if not null.</param>
    /// <param name="userCredential">The credential to use when creating and starting the Process.</param>
    /// <param name="standardInput">The Stream to redirect to the Standard Input if not null.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from the running the process.</returns>
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
    public async Task<ProcessResult> ExecuteAsync(ProcessStartInfo processStartInfo,
        ProcessExitConfiguration? processExitInfo = null,
        ProcessResourcePolicy? processResourcePolicy = null,
        UserCredential? userCredential = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        processStartInfo.FileName = _filePathResolver.ResolveFilePath(processStartInfo.FileName);

        if(processExitInfo is null)
            processExitInfo = ProcessExitConfiguration.Default;
        
        if (File.Exists(processStartInfo.FileName) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound
                .Replace("{file}",
                    processStartInfo.FileName));
        }

        if (standardInput is not null && standardInput != StreamWriter.Null)
        {
            processStartInfo.RedirectStandardInput = true;
        }
        
        if(userCredential is not null)
            if(userCredential.IsSupportedOnCurrentOS())
#pragma warning disable CA1416
                processStartInfo.ApplyUserCredential(userCredential);
#pragma warning restore CA1416
        
        Process process = new Process()
        {
            StartInfo = processStartInfo,
        };
        
        if (processStartInfo.RedirectStandardInput && standardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream,
                process);
        }

        process.Start();
        
        if(processResourcePolicy is not null)
            process.SetResourcePolicy(processResourcePolicy);
        
        await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        
        ProcessResult result = new ProcessResult(executableFilePath: process.StartInfo.FileName,
            exitCode: process.ExitCode,
            exitTime: process.ExitTime,
            startTime: process.StartTime);

        process.Dispose();
        
        return result;
    }

    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitInfo"></param>
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
        ProcessExitConfiguration? processExitInfo,
        CancellationToken cancellationToken = default)
    {
        processConfiguration.TargetFilePath = _filePathResolver.ResolveFilePath(processConfiguration.TargetFilePath);
        
        if (processExitInfo is null) 
            processExitInfo = ProcessExitConfiguration.Default;
        
        if (File.Exists(processConfiguration.TargetFilePath) == false)
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound
                .Replace("{file}",
                    processConfiguration.TargetFilePath));
        }

        Process process = new Process();

        process.ApplyProcessConfiguration(processConfiguration, true,
            true);

        process.Start();
        
        process.SetResourcePolicy(processConfiguration.ResourcePolicy);
        
        await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);

        BufferedProcessResult result = new BufferedProcessResult(
            process.StartInfo.FileName,
            process.ExitCode,
            await process.StandardOutput.ReadToEndAsync(cancellationToken),
            await process.StandardError.ReadToEndAsync(cancellationToken),
            process.StartTime,
            process.ExitTime);
                              
        process.Dispose();
        
        return result;
    }

    //TODO: Implement
    public Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessStartInfo processStartInfo, ProcessExitConfiguration? processExitInfo = null,
        StreamWriter? standardInput = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processExitInfo"></param>
    /// <param name="processResourcePolicy">The resource policy to be set if not null.</param>
    /// <param name="userCredential">The credential to use when creating and starting the Process.</param>
    /// <param name="standardInput">The Stream to redirect to the Standard Input if not null.</param>
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
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessStartInfo processStartInfo,
        ProcessExitConfiguration? processExitInfo,
        ProcessResourcePolicy? processResourcePolicy = null,
        UserCredential? userCredential = null,
        StreamWriter? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        processStartInfo.FileName = _filePathResolver.ResolveFilePath(processStartInfo.FileName);
        
        if(processExitInfo is null)
            processExitInfo = ProcessExitConfiguration.Default;
        
        processStartInfo.RedirectStandardInput = standardInput is not null;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;

                
        if(userCredential is not null)
            if(userCredential.IsSupportedOnCurrentOS())
#pragma warning disable CA1416
                processStartInfo.ApplyUserCredential(userCredential);
#pragma warning restore CA1416
        
        Process process = new Process()
        {
            StartInfo = processStartInfo
        };
        
        if (processStartInfo.RedirectStandardInput && standardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream,
                process);
        }

        process.Start();
        
        if(processResourcePolicy is not null)
            process.SetResourcePolicy(processResourcePolicy);

        await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        
        BufferedProcessResult result = new BufferedProcessResult(
            process.StartInfo.FileName, process.ExitCode,
            await process.StandardOutput.ReadToEndAsync(cancellationToken),
            await process.StandardError.ReadToEndAsync(cancellationToken),
            process.StartTime, process.ExitTime);

        process.Dispose();
        
        return result;
    }

    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Streams, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitInfo"></param>
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
        ProcessExitConfiguration? processExitInfo,
        CancellationToken cancellationToken = default)
    {
        processConfiguration.TargetFilePath = _filePathResolver.ResolveFilePath(processConfiguration.TargetFilePath);
        
        if (processExitInfo is null) 
            processExitInfo = ProcessExitConfiguration.Default;
        
        Process process = new Process();
        
        process.ApplyProcessConfiguration(processConfiguration, true,
            true);

        process.Start();
        
        process.SetResourcePolicy(processConfiguration.ResourcePolicy);
        
        await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);

        Stream standardOutput = await _processPipeHandler.PipeStandardOutputAsync(process);
        Stream standardError = await _processPipeHandler.PipeStandardErrorAsync(process);
        
        PipedProcessResult result = new PipedProcessResult(process.StartInfo.FileName,
            process.ExitCode, process.StartTime, process.ExitTime,
            standardOutput, standardError);
        
        process.Dispose();
        
        return result;
    }

    
    /// <summary>
    /// Runs the process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processExitInfo"></param>
    /// <param name="standardInput">The Stream to redirect to the Standard Input if not null.</param>
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
    public async Task<PipedProcessResult> ExecutePipedAsync(ProcessStartInfo processStartInfo, 
        ProcessExitConfiguration? processExitInfo = null,
        StreamWriter? standardInput = null, CancellationToken cancellationToken = default)
    {
        processStartInfo.FileName = _filePathResolver.ResolveFilePath(processStartInfo.FileName);
        
        processExitInfo ??= ProcessExitConfiguration.Default;
        
        processStartInfo.RedirectStandardOutput = standardInput is not null;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;
        
        Process process = new Process()
        {
            StartInfo = processStartInfo
        };

        if (processStartInfo.RedirectStandardInput && standardInput is not null)
        {
            process = await _processPipeHandler.PipeStandardInputAsync(standardInput.BaseStream,
                process);
        }

        process.Start();
        
      //  process.SetResourcePolicy(processResourcePolicy);
        
        await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);

        Stream standardOutput = await _processPipeHandler.PipeStandardOutputAsync(process);
        Stream standardError = await _processPipeHandler.PipeStandardErrorAsync(process);
        
        PipedProcessResult result = new PipedProcessResult(process.StartInfo.FileName,
            process.ExitCode, process.StartTime, process.ExitTime,
            standardOutput, standardError);
        
        process.Dispose();

        return result;
    }


}
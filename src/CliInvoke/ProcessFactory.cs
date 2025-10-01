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

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Piping;
using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.CliInvoke.Exceptions;
using AlastairLundy.CliInvoke.Internal.Localizations;
using AlastairLundy.CliInvoke.Internal.Processes;


// ReSharper disable UnusedType.Global

namespace AlastairLundy.CliInvoke;

/// <summary>
/// The default implementation of <see cref="IProcessFactory"/>, an easy and safe way to create, run, and dispose of Processes.
/// </summary>
public class ProcessFactory : IProcessFactory
{
    private readonly IFilePathResolver _filePathResolver;
    
    private readonly IProcessPipeHandler _processPipeHandler;

    /// <summary>
    /// Instantiates a ProcessFactory to be used for creating and running Processes, as well as safely disposing of a Process when it exits.
    /// </summary>
    /// <param name="filePathResolver">The file path resolver to use.</param>
    /// <param name="processPipeHandler">The pipe handler to be used for managing the input/output streams of the processes.</param>
    public ProcessFactory(IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler)
    {
        _filePathResolver = filePathResolver;
        _processPipeHandler = processPipeHandler;
    }

    /// <summary>
    /// Creates a process from the specified start info.
    /// </summary>
    /// <param name="processStartInfo">The start information to use for the Process.</param>
    /// <returns>The newly created Process.</returns>
    /// <exception cref="ArgumentException">Thrown if the process start info FileName is empty.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Process From(ProcessStartInfo processStartInfo)
    {
        if (string.IsNullOrEmpty(processStartInfo.FileName))
        {
            throw new ArgumentException(Resources.Process_FileName_Empty);
        }

        if (Path.IsPathFullyQualified(processStartInfo.FileName) == false)
        {
            string resolvedFilePath = _filePathResolver.ResolveFilePath(processStartInfo.FileName);
            processStartInfo.FileName = resolvedFilePath;
        }

        Process output = new Process
        {
            StartInfo = processStartInfo,
        };

        return output;
    }

    /// <summary>
    /// Creates a process from the specified start info and UserCredential.
    /// </summary>
    /// <param name="startInfo">The start information to use for the Process.</param>
    /// <param name="credential">The credential to use when creating the Process.</param>
    /// <returns>The newly created Process.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Process From(ProcessStartInfo startInfo, UserCredential credential)
    {
#pragma warning disable CA1416
            startInfo.SetUserCredential(credential);
#pragma warning restore CA1416
        
        Process output = From(startInfo);
        
        return output;
    }

    /// <summary>
    /// Creates a process from the specified process configuration.
    /// </summary>
    /// <param name="configuration">The configuration information to use to configure the Process.</param>
    /// <returns>The newly created Process with the configuration.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Process From(ProcessConfiguration configuration)
    {
        Process output = new Process()
        {
            StartInfo = configuration.ToProcessStartInfo(configuration.RedirectStandardOutput,
                configuration.RedirectStandardError),
        };
        
        return output;
    }

    /// <summary>
    /// Creates and starts a new Process with the specified Process Start Info.
    /// </summary>
    /// <param name="startInfo">The start info to use when creating and starting the new Process.</param>
    /// <returns>The newly created and started Process with the start info.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Process StartNew(ProcessStartInfo startInfo)
    {
        Process process = From(startInfo);
        
        process.Start();
        
        return process;
    }

    /// <summary>
    /// Creates and starts a new Process with the specified Process Start Info and credential.
    /// </summary>
    /// <param name="startInfo">The start info to use when creating and starting the new Process.</param>
    /// <param name="credential">The credential to use when creating and starting the Process.</param>
    /// <returns>The newly created and started Process with the start info and credential.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Process StartNew(ProcessStartInfo startInfo,
        UserCredential credential)
    {
        Process process = From(startInfo,
            credential);
        
        process.Start();
        
        return process;
    }

    /// <summary>
    /// Creates and starts a new Process with the specified Process Start Info and Process Resource policy.
    /// </summary>
    /// <param name="startInfo">The start info to use when creating and starting the new Process.</param>
    /// <param name="resourcePolicy">The process resource policy to use when creating and starting the new Process.</param>
    /// <returns>The newly created and started Process with the start info and Process Resource Policy.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Process StartNew(ProcessStartInfo startInfo,
        ProcessResourcePolicy resourcePolicy)
    {
        Process process = From(startInfo);

        process.Start();
        
        if(process.HasStarted() && process.HasStarted() == false)
            process.SetResourcePolicy(resourcePolicy);
        
        return process;
    }

    /// <summary>
    /// Creates and starts a new Process with the specified Process Start Info, credential, and Process Resource policy.
    /// </summary>
    /// <param name="startInfo">The start info to use when creating and starting the new Process.</param>
    /// <param name="resourcePolicy">The process resource policy to use when creating and starting the new Process.</param>
    /// <param name="credential">The credential to use when creating and starting the Process.</param>
    /// <returns>The newly created and started Process with the start info and Process Resource Policy.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Process StartNew(ProcessStartInfo startInfo, 
        ProcessResourcePolicy resourcePolicy,
        UserCredential credential)
    {
        Process process = From(startInfo,
            credential);
        
        process.Start();
        
        if(process.HasStarted() && process.HasStarted() == false)
            process.SetResourcePolicy(resourcePolicy);

        return process;
    }

    /// <summary>
    /// Creates and starts a new Process with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration to use when creating and starting the process.</param>
    /// <returns>The newly created and started Process with the specified configuration.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public Process StartNew(ProcessConfiguration configuration)
    {
        Process process = From(configuration);

        if (configuration.StandardInput is not null &&
            process.StartInfo.RedirectStandardInput)
        {
            configuration.StandardInput.BaseStream.CopyTo(process.StandardInput.BaseStream);
        }
        
        process.Start();

        if(process.HasStarted() && process.HasStarted() == false)
            process.SetResourcePolicy(configuration.ResourcePolicy);

        return process;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="process"></param>
    /// <param name="processExitInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ProcessNotSuccessfulException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ProcessResult> WaitForExitAsync(Process process, 
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        processExitInfo ??= ProcessExitConfiguration.Default;

        ProcessResult processResult;

        try
        {
            if (process.HasStarted() == false)
                process = StartNew(process.StartInfo,
                    ProcessResourcePolicy.Default);
            
            await process.WaitForExitOrTimeoutAsync(processExitInfo.TimeoutPolicy, cancellationToken);

            if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
            {
                throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                    process: process);
            }

            processResult = new ProcessResult(
                process.StartInfo.FileName,
                process.ExitCode,
                process.StartTime,
                process.ExitTime);
        }
        finally
        {
            process.Dispose();
        }
        
        return processResult;
    }

    /// <summary>
    /// Creates a Task that returns a ProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitInfo"></param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The task and ProcessResult that are returned upon completion of the task.</returns>
    /// <exception cref="ProcessNotSuccessfulException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ProcessResult> WaitForExitAsync(Process process, ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        processExitInfo ??= ProcessExitConfiguration.Default;

        ProcessResult processResult;

        Stream standardOutput = Stream.Null;
        Stream standardError = Stream.Null;
        
        try
        {
            if(process.HasStarted() == false)
                process = StartNew(process.StartInfo,
                    processConfiguration.ResourcePolicy, processConfiguration.Credential ?? UserCredential.Null);
        
            process.StartInfo.RedirectStandardOutput = processConfiguration.StandardOutput is not null &&
                                                       processConfiguration.StandardOutput != StreamReader.Null;
            process.StartInfo.RedirectStandardError = processConfiguration.StandardError is not null &&
                                                      processConfiguration.StandardError != StreamReader.Null;
            
            if (process.StartInfo.RedirectStandardOutput)
            {
                standardOutput = await _processPipeHandler.PipeStandardOutputAsync(process);

                if (processConfiguration.StandardOutput is not null)
                {
                    await standardOutput.CopyToAsync(processConfiguration.StandardOutput.BaseStream,
                        cancellationToken);
                }
            }

            if (process.StartInfo.RedirectStandardError)
            {
                standardError = await _processPipeHandler.PipeStandardErrorAsync(process);

                if (processConfiguration.StandardError is not null)
                {
                    await standardError.CopyToAsync(processConfiguration.StandardError.BaseStream,
                        cancellationToken);
                }
            }
            
            await process.WaitForExitOrTimeoutAsync(processExitInfo.TimeoutPolicy, cancellationToken);

            if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
            {
                throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                    process: process);
            }
        
            processResult = new ProcessResult(process.StartInfo.FileName,
                process.ExitCode,
                process.StartTime,
                process.ExitTime);
        }
        finally
        {
            process.Dispose();
            await standardOutput.DisposeAsync();
            await standardError.DisposeAsync(); 
        }
        
        return processResult;
    }
    

    /// <summary>
    /// A Task that returns a BufferedProcessResult when the specified process exits.
    /// </summary>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the process exit code is not zero AND exit code validation is performed.</exception>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="processExitInfo"></param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The task and BufferedProcessResult that are returned upon completion of the task.</returns>
    /// <exception cref="ProcessNotSuccessfulException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<BufferedProcessResult> WaitForBufferedExitAsync(Process process, 
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        processExitInfo ??= ProcessExitConfiguration.Default;

        BufferedProcessResult processResult;
        
        try
        {
            if (process.HasStarted() == false)
            {
                process = StartNew(process.StartInfo, ProcessResourcePolicy.Default);
            }        
            
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            
            Task<string> standardOutStringTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardErrorStringTask = process.StandardError.ReadToEndAsync(cancellationToken);

            Task waitForExit = process.WaitForExitOrTimeoutAsync(processExitInfo.TimeoutPolicy,
                cancellationToken);

            await Task.WhenAll(standardOutStringTask, standardErrorStringTask, waitForExit);
            
            if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
            {
                throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                    process: process);
            }
        
            processResult = new BufferedProcessResult(
                process.StartInfo.FileName,
                process.ExitCode,
                await standardOutStringTask,
                await standardErrorStringTask,
                process.StartTime,
                process.ExitTime);
        }
        finally
        {
            process.Dispose();
        }
       
        return processResult;
    }
    
    /// <summary>
    /// A Task that returns a BufferedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="processConfiguration">The configuration to use when running and waiting for the process to exit.</param>
    /// <param name="processExitInfo"></param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The task and BufferedProcessResult that are returned upon the process' exit.</returns>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the process exit code is not zero AND exit code validation is performed.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<BufferedProcessResult> ContinueWhenExitBufferedAsync(Process process, ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        processExitInfo ??= ProcessExitConfiguration.Default;

        BufferedProcessResult processResult;

        Stream standardOutputStream = Stream.Null;
        Stream standardErrorStream = Stream.Null;
        
        try
        {
            if(process.HasStarted() == false)
                process = StartNew(process.StartInfo, processConfiguration.ResourcePolicy,
                    processConfiguration.Credential ?? UserCredential.Null);
        
            process.SetResourcePolicy(processConfiguration.ResourcePolicy);
        
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            
            Task<Stream> standardOutputStreamTask = _processPipeHandler.PipeStandardOutputAsync(process);
            Task<Stream> standardErrorStreamTask = _processPipeHandler.PipeStandardErrorAsync(process);

            Task<string> standardOutStringTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardErrorStringTask = process.StandardError.ReadToEndAsync(cancellationToken);
            
             Task waitForExit = process.WaitForExitOrTimeoutAsync(processExitInfo.TimeoutPolicy,
                 cancellationToken);
            
            await Task.WhenAll(standardOutputStreamTask, standardErrorStreamTask,
                standardOutStringTask, standardErrorStringTask, waitForExit);
            
            if (process.ExitCode != 0 &&
                processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
            {
                throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                    process: process);
            }
            
            standardOutputStream = await  standardOutputStreamTask;
            standardErrorStream = await standardErrorStreamTask;

            if (processConfiguration.StandardOutput is not null && processConfiguration.RedirectStandardOutput)
            {
                await standardOutputStream.CopyToAsync(processConfiguration.StandardOutput.BaseStream,
                    cancellationToken);
            }
            
            if (processConfiguration.StandardError is not null && processConfiguration.RedirectStandardError)
            {
                await standardErrorStream.CopyToAsync(processConfiguration.StandardError.BaseStream,
                    cancellationToken);
            }

            processResult = new BufferedProcessResult(
                process.StartInfo.FileName,
                process.ExitCode,
                await standardOutStringTask,
                await standardErrorStringTask,
                process.StartTime,
                process.ExitTime);
        }
        finally
        {
            process.Dispose();

           await standardOutputStream.DisposeAsync();
           await standardErrorStream.DisposeAsync();
        }
        
        return processResult;
    }

    /// <summary>
    /// A Task that returns a PipedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="processExitInfo"></param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The Piped Process Result that is returned from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the process exit code is not zero AND exit code validation is performed.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<PipedProcessResult> WaitForPipedExitAsync(Process process,
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        processExitInfo ??= ProcessExitConfiguration.NoTimeoutDefault;
        
        PipedProcessResult processResult;
        
        Stream outputStream = Stream.Null;
        Stream errorStream = Stream.Null;
        
        try{
            if(process.HasStarted() == false)
                process = StartNew(process.StartInfo, 
                    ProcessResourcePolicy.Default);
        
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            Task<Stream> outputTask =  _processPipeHandler.PipeStandardOutputAsync(process);
            Task<Stream> errorTask = _processPipeHandler.PipeStandardErrorAsync(process);

            Task waitForExit = process.WaitForExitOrTimeoutAsync(processExitInfo.TimeoutPolicy,
                cancellationToken);

            await Task.WhenAll(outputTask, errorTask, waitForExit);

            outputStream = await outputTask;
            errorStream = await errorTask;
            
            if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
            {
                throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                    process: process);
            }
        
            processResult = new PipedProcessResult(
                process.StartInfo.FileName, process.ExitCode,
                process.StartTime, process.ExitTime,
                outputStream, errorStream);
        }
        finally
        {
            process.Dispose();
            
            await outputStream.DisposeAsync();
            await errorStream.DisposeAsync();
        }
        
        return processResult;
    }

    /// <summary>
    /// A Task that returns a PipedProcessResult when the specified process exits.
    /// </summary>
    /// <param name="process">The process to continue and wait for exit.</param>
    /// <param name="processConfiguration">The configuration to use when running and waiting for the process to exit.</param>
    /// <param name="processExitInfo"></param>
    /// <param name="cancellationToken">The cancellation token to use in case cancellation is requested.</param>
    /// <returns>The Piped Process Result that is returned from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the process exit code is not zero AND exit code validation is performed.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<PipedProcessResult> WaitForPipedExitAsync(Process process,
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        processExitInfo ??= ProcessExitConfiguration.NoTimeoutDefault;

        PipedProcessResult processResult;
        
        Stream outputStream = Stream.Null;
        Stream errorStream = Stream.Null;
        
        try
        {
            if(process.HasStarted() == false)
                process = StartNew(process.StartInfo, 
                    processConfiguration.ResourcePolicy,
                    processConfiguration.Credential ?? UserCredential.Null);
        
            process.SetResourcePolicy(processConfiguration.ResourcePolicy);
        
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            Task<Stream> outputTask =  _processPipeHandler.PipeStandardOutputAsync(process);
            Task<Stream> errorTask = _processPipeHandler.PipeStandardErrorAsync(process);

            Task waitForExit = process.WaitForExitOrTimeoutAsync(processExitInfo.TimeoutPolicy, cancellationToken);

            await Task.WhenAll(outputTask, errorTask, waitForExit);
            
            outputStream = await outputTask;
            errorStream = await errorTask;
        
            if (processConfiguration.StandardOutput is not null && processConfiguration.RedirectStandardOutput)
                await outputStream.CopyToAsync(processConfiguration.StandardOutput.BaseStream,
                    cancellationToken);
        
            if (processConfiguration.StandardError is not null && processConfiguration.RedirectStandardError)
                await errorStream.CopyToAsync(processConfiguration.StandardError.BaseStream,
                    cancellationToken);
        
            if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
            {
                throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                    process: process);
            }
        
            processResult = new PipedProcessResult(
                process.StartInfo.FileName, process.ExitCode,
                process.StartTime, process.ExitTime,
                outputStream, errorStream);
        }
        finally
        {
            process.Dispose();
            
            await outputStream.DisposeAsync();
            await errorStream.DisposeAsync();
        }
        
        return processResult;
    }
}
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
using AlastairLundy.CliInvoke.Internal;
using AlastairLundy.CliInvoke.Internal.Localizations;

using AlastairLundy.DotExtensions.Processes;
using ApplyConfigurationToProcess = AlastairLundy.CliInvoke.Internal.ApplyConfigurationToProcess;

// ReSharper disable UnusedType.Global

namespace AlastairLundy.CliInvoke;

/// <summary>
/// The default implementation of <see cref="IProcessFactory"/>, an easy and safe way to create, run, and dispose of ProcessPrimitives.
/// </summary>
public class ProcessFactory : IProcessFactory
{
    private readonly IFilePathResolver _filePathResolver;
    
    private readonly IProcessPipeHandler _processPipeHandler;

    /// <summary>
    /// Instantiates a ProcessFactory to be used for creating and running ProcessPrimitives, as well as safely disposing of a Process when it exits.
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
        Process output = From(startInfo);

        if (credential.IsSupportedOnCurrentOS())
        {
#pragma warning disable CA1416
            output.ApplyUserCredential(credential);
#pragma warning restore CA1416
        }

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
        Process output = new Process();
        
        output.ApplyProcessConfiguration(configuration, 
            configuration.StandardOutput is not null && configuration.StandardOutput != StreamReader.Null,
            configuration.StandardError is not null && configuration.StandardError != StreamReader.Null);

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
        
        if (configuration.ResourcePolicy is not null)
        {
            process.SetResourcePolicy(configuration.ResourcePolicy);
        }
        
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
    public async Task<ProcessResult> ContinueWhenExitAsync(Process process, 
        ProcessExitInfo? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        if(processExitInfo is null)
            processExitInfo = ProcessExitInfo.Default;
        
        if(process.HasStarted() == false)
            process = StartNew(process.StartInfo,
                ProcessResourcePolicy.Default);

        if (processExitInfo.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        else
        {
            await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        }
        
        if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
        {
            throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                process: process);
        }
        
        ProcessResult processResult = new ProcessResult(
            process.StartInfo.FileName,
            process.ExitCode,
            process.StartTime,
            process.ExitTime);
        
        process.Dispose();
        
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
    public async Task<ProcessResult> ContinueWhenExitAsync(Process process, ProcessConfiguration processConfiguration,
        ProcessExitInfo? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        if (processExitInfo is null)
            processExitInfo = ProcessExitInfo.Default;
        
        if(process.HasStarted() == false)
            process = StartNew(process.StartInfo,
                processConfiguration.ResourcePolicy ?? ProcessResourcePolicy.Default,
                processConfiguration.Credential ?? UserCredential.Null);
        
        process.StartInfo.RedirectStandardOutput = processConfiguration.StandardOutput is not null &&
                                                   processConfiguration.StandardOutput != StreamReader.Null;
        process.StartInfo.RedirectStandardError = processConfiguration.StandardError is not null &&
                                                  processConfiguration.StandardError != StreamReader.Null;

        if (processExitInfo.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        else
        {
            await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        }

        if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
        {
            throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                process: process);
        }

        if (process.StartInfo.RedirectStandardOutput)
        {
            Stream standardOutput = await _processPipeHandler.PipeStandardOutputAsync(process);

            if (processConfiguration.StandardOutput != StreamReader.Null && processConfiguration.StandardOutput is not null)
            {
                await standardOutput.CopyToAsync(processConfiguration.StandardOutput.BaseStream,
                    cancellationToken);
            }
        }

        if (process.StartInfo.RedirectStandardError)
        {
            Stream standardError = await _processPipeHandler.PipeStandardErrorAsync(process);

            if (processConfiguration.StandardError != StreamReader.Null && processConfiguration.StandardError is not null)
            {
                await standardError.CopyToAsync(processConfiguration.StandardError.BaseStream,
                    cancellationToken);
            }
        }
        
        ProcessResult processResult = new ProcessResult(process.StartInfo.FileName,
            process.ExitCode,
            process.StartTime,
            process.ExitTime);
        
        process.Dispose();
        
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
    public async Task<BufferedProcessResult> ContinueWhenExitBufferedAsync(Process process, 
        ProcessExitInfo? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        if(processExitInfo is null)
            processExitInfo = ProcessExitInfo.Default;
        
        if(process.HasStarted() == false)
            process = StartNew(process.StartInfo,
                ProcessResourcePolicy.Default);
        
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (processExitInfo.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        else
        {
            await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        }
        
        if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
        {
            throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                process: process);
        }
        
        BufferedProcessResult processResult = new BufferedProcessResult(
            process.StartInfo.FileName,
            process.ExitCode,
            await process.StandardOutput.ReadToEndAsync(cancellationToken),
            await process.StandardError.ReadToEndAsync(cancellationToken),
            process.StartTime,
            process.ExitTime);
        
        process.Dispose();
        
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
        ProcessExitInfo? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        if(processExitInfo is null)
            processExitInfo = ProcessExitInfo.Default;

        if(process.HasStarted() == false)
            process = StartNew(process.StartInfo, 
                processConfiguration.ResourcePolicy ?? ProcessResourcePolicy.Default,
                processConfiguration.Credential ?? UserCredential.Null);
        
        if(processConfiguration.ResourcePolicy is not null)
            process.SetResourcePolicy(processConfiguration.ResourcePolicy);
        
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (processExitInfo.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        else
        {
            await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        }
        
        if (process.ExitCode != 0 &&
            processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
        {
            throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                process: process);
        }

        Stream standardOutput = await _processPipeHandler.PipeStandardOutputAsync(process);
        Stream standardError = await _processPipeHandler.PipeStandardErrorAsync(process);

        if (processConfiguration.StandardOutput != StreamReader.Null &&
            processConfiguration.StandardOutput is not null)
        {
           await standardOutput.CopyToAsync(processConfiguration.StandardOutput.BaseStream,
               cancellationToken);
        }

        if (processConfiguration.StandardError != StreamReader.Null &&
            processConfiguration.StandardError is not null)
        {
            await standardError.CopyToAsync(processConfiguration.StandardError.BaseStream,
                cancellationToken);
        }
        
        BufferedProcessResult processResult = new BufferedProcessResult(
            process.StartInfo.FileName,
            process.ExitCode,
            await process.StandardOutput.ReadToEndAsync(cancellationToken),
            await process.StandardError.ReadToEndAsync(cancellationToken),
            process.StartTime,
            process.ExitTime);
        
        process.Dispose();
        
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
    public async Task<PipedProcessResult> ContinueWhenExitPipedAsync(Process process,
        ProcessExitInfo? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        if(processExitInfo is null)
            processExitInfo = ProcessExitInfo.Default;
        
        if(process.HasStarted() == false)
            process = StartNew(process.StartInfo, 
                ProcessResourcePolicy.Default);
        
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (processExitInfo.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        else
        {
            await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        }

        if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
        {
            throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                process: process);
        }

        Stream output = await _processPipeHandler.PipeStandardOutputAsync(process);
        Stream error = await _processPipeHandler.PipeStandardErrorAsync(process);
        
        PipedProcessResult processResult = new PipedProcessResult(
            process.StartInfo.FileName,
            process.ExitCode,
            process.StartTime,
            process.ExitTime,
            output,
            error);
        
        process.Dispose();
        
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
    public async Task<PipedProcessResult> ContinueWhenExitPipedAsync(Process process,
        ProcessConfiguration processConfiguration,
        ProcessExitInfo? processExitInfo = null,
        CancellationToken cancellationToken = default)
    {
        if(processExitInfo is null)
            processExitInfo = ProcessExitInfo.Default;
        
        if(process.HasStarted() == false)
            process = StartNew(process.StartInfo, 
                processConfiguration.ResourcePolicy ?? ProcessResourcePolicy.Default,
                processConfiguration.Credential ?? UserCredential.Null);
        
        if(processConfiguration.ResourcePolicy is not null)
            process.SetResourcePolicy(processConfiguration.ResourcePolicy);
        
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (processExitInfo.TimeoutPolicy.CancellationMode == ProcessCancellationMode.None)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        else
        {
            await process.WaitForExitAsync(processExitInfo.TimeoutPolicy, cancellationToken);
        }
        
        if (process.ExitCode != 0 && processExitInfo.ResultValidation == ProcessResultValidation.ExitCodeZero)
        {
            throw new ProcessNotSuccessfulException(exitCode: process.ExitCode,
                process: process);
        }

        Stream output = await _processPipeHandler.PipeStandardOutputAsync(process);
        Stream error = await _processPipeHandler.PipeStandardErrorAsync(process);

        if (processConfiguration.StandardOutput is not null && processConfiguration.StandardOutput != StreamReader.Null)
        {
            await output.CopyToAsync(processConfiguration.StandardOutput.BaseStream,
                cancellationToken);
        }

        if (processConfiguration.StandardError is not null && processConfiguration.StandardError != StreamReader.Null)
        {
            await error.CopyToAsync(processConfiguration.StandardError.BaseStream,
                cancellationToken);
        }
        
        PipedProcessResult processResult = new PipedProcessResult(
            process.StartInfo.FileName,
            process.ExitCode,
            process.StartTime,
            process.ExitTime,
            output,
            error);
        
        process.Dispose();
        
        return processResult;
    }
}
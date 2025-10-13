/*
    AlastairLundy.CliInvoke.Extensions
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;

#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif

using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Builders;

namespace AlastairLundy.CliInvoke.Extensions;

/// <summary>
/// A class providing syntactic sugar to make simple uses cases super easy with CliInvoke.
/// </summary>
public static class Cli
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfigurationInvoker"></param>
    /// <param name="targetFilePath"></param>
    /// <param name="arguments"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET8_0_OR_GREATER
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
    public static async Task<ProcessResult> InvokeAsync(IProcessConfigurationInvoker processConfigurationInvoker,
        string targetFilePath,
        IEnumerable<string> arguments, string? workingDirectory = null,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(targetFilePath)
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory);

        ProcessConfiguration configuration = processConfigurationBuilder.Build();
        
        ProcessExitConfiguration processExitConfiguration;

        if (exitConfiguration is null)
        {
            processExitConfiguration = new ProcessExitConfiguration(ProcessTimeoutPolicy.Default,
                ProcessResultValidation.ExitCodeZero, ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected);
        }
        else
        {
            processExitConfiguration = exitConfiguration;
        }
        
        return await processConfigurationInvoker.ExecuteAsync(configuration, processExitConfiguration,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfigurationInvoker"></param>
    /// <param name="targetFilePath"></param>
    /// <param name="arguments"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET8_0_OR_GREATER
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
    public static async Task<BufferedProcessResult> InvokeBufferedAsync(IProcessConfigurationInvoker processConfigurationInvoker,
        string targetFilePath,
        IEnumerable<string> arguments, string? workingDirectory = null,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(targetFilePath)
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory)
            .RedirectStandardOutput(true)
            .RedirectStandardError(true)
            .WithWindowCreation(false);

        ProcessConfiguration configuration = processConfigurationBuilder.Build();

        ProcessExitConfiguration processExitConfiguration;
        
        if (exitConfiguration is null)
        {
            processExitConfiguration = new ProcessExitConfiguration(ProcessTimeoutPolicy.Default,
                ProcessResultValidation.ExitCodeZero, ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected);
        }
        else
        {
            processExitConfiguration = exitConfiguration;
        }
        
        return await processConfigurationInvoker.ExecuteBufferedAsync(configuration, processExitConfiguration,
            cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfigurationInvoker"></param>
    /// <param name="targetFilePath"></param>
    /// <param name="arguments"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET8_0_OR_GREATER
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
    public static async Task<PipedProcessResult> InvokePipedAsync(IProcessConfigurationInvoker processConfigurationInvoker,
        string targetFilePath,
        IEnumerable<string> arguments, string? workingDirectory = null,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(targetFilePath)
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory)
            .RedirectStandardOutput(true)
            .RedirectStandardError(true)
            .WithWindowCreation(false);

        ProcessConfiguration configuration = processConfigurationBuilder.Build();

        ProcessExitConfiguration processExitConfiguration;
        
        if (exitConfiguration is null)
        {
            processExitConfiguration = new ProcessExitConfiguration(ProcessTimeoutPolicy.Default,
                ProcessResultValidation.ExitCodeZero, ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected);
        }
        else
        {
            processExitConfiguration = exitConfiguration;
        }
        
        return await processConfigurationInvoker.ExecutePipedAsync(configuration, processExitConfiguration,
            cancellationToken: cancellationToken);
    }
}
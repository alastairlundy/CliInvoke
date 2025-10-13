/*
    AlastairLundy.CliInvoke.Extensions
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;

#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif

using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Exceptions;

namespace AlastairLundy.CliInvoke.Extensions;

/// <summary>
/// A class providing syntactic sugar to make simple process running uses cases easy with CliInvoke.
/// </summary>
public static partial class Cli
{
    /// <summary>
    /// Runs a process asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfigurationInvoker">The process configuration invoker to use to execute the command.</param>
    /// <param name="processConfiguration"></param>
    /// <param name="exitConfiguration">The exit configuration to use for the process, or the default exit configuration if null.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file, with the file name of the process to be executed, is not found.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
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
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        ProcessExitConfiguration processExitConfiguration = exitConfiguration ?? ProcessExitConfiguration.Default;
        
        return await processConfigurationInvoker.ExecuteAsync(processConfiguration, processExitConfiguration,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs a process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfigurationInvoker">The process configuration invoker to use to execute the command.</param>
    /// <param name="processConfiguration"></param>
    /// <param name="exitConfiguration">The exit configuration to use for the process, or the default exit configuration if null.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file, with the file name of the process to be executed, is not found.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
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
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        ProcessExitConfiguration processExitConfiguration = exitConfiguration ?? ProcessExitConfiguration.Default;
        
        return await processConfigurationInvoker.ExecuteBufferedAsync(processConfiguration, processExitConfiguration,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs a process asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfigurationInvoker">The process configuration invoker to use to execute the command.</param>
    /// <param name="processConfiguration"></param>
    /// <param name="exitConfiguration">The exit configuration to use for the process, or the default exit configuration if null.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file, with the file name of the process to be executed, is not found.</exception>
    /// <exception cref="ProcessNotSuccessfulException">Thrown if the result validation requires the process to exit with exit code zero and the process exits with a different exit code.</exception>
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
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        ProcessExitConfiguration processExitConfiguration = exitConfiguration ?? ProcessExitConfiguration.Default;
        
        return await processConfigurationInvoker.ExecutePipedAsync(processConfiguration, processExitConfiguration,
            cancellationToken: cancellationToken);
    }
}
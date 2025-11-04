/*
    AlastairLundy.CliInvoke.Extensions
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Exceptions;
#if NET8_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AlastairLundy.CliInvoke.Extensions.Invokation;

/// <summary>
/// Provides extension methods for executing process configurations using various execution models,
/// such as standard, buffered, or piped processing.
/// </summary>
public static class ConfigurationInvokationExtensions
{
    /// <summary>
    /// Runs a process configuration asynchronously, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processConfigurationInvoker">The process configuration invoker to use to execute the command.</param>
    /// <param name="processExitConfiguration">The exit configuration to use for the process, or the default if null.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
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
    public static async Task<ProcessResult> ExecuteAsync(
        this ProcessConfiguration processConfiguration,
        IProcessInvoker processConfigurationInvoker,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default
    )
    {
        return await processConfigurationInvoker.ExecuteAsync(
            processConfiguration,
            processExitConfiguration,
            disposeOfConfig,
            cancellationToken
        );
    }

    /// <summary>
    /// Runs a process configuration asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processConfigurationInvoker">The process configuration invoker to use to execute the command.</param>
    /// <param name="processExitConfiguration">The exit configuration to use for the process, or the default if null.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
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
    public static async Task<BufferedProcessResult> ExecuteBufferedAsync(
        this ProcessConfiguration processConfiguration,
        IProcessInvoker processConfigurationInvoker,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default
    )
    {
        return await processConfigurationInvoker.ExecuteBufferedAsync(
            processConfiguration,
            processExitConfiguration,
            disposeOfConfig,
            cancellationToken
        );
    }

    /// <summary>
    /// Runs a process configuration asynchronously with Standard Output and Standard Error Redirection,
    /// gets Standard Output and Standard Error as Streams, waits for exit, and safely disposes of the Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processConfigurationInvoker">The process configuration invoker to use to execute the command.</param>
    /// <param name="processExitConfiguration">The exit configuration to use for the process, or the default if null.</param>
    /// <param name="disposeOfConfig">Whether to dispose of the provided <see cref="ProcessConfiguration"/> after use or not, defaults to false.</param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Results from running the process.</returns>
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
    public static async Task<PipedProcessResult> ExecutePipedAsync(
        this ProcessConfiguration processConfiguration,
        IProcessInvoker processConfigurationInvoker,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = false,
        CancellationToken cancellationToken = default
    )
    {
        return await processConfigurationInvoker.ExecutePipedAsync(
            processConfiguration,
            processExitConfiguration,
            disposeOfConfig,
            cancellationToken
        );
    }
}

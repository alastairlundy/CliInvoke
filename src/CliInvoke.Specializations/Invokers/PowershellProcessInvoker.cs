/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/


using CliInvoke.Core.Factories;
using CliInvoke.Specializations.Middleware;

namespace CliInvoke.Specializations;

/// <summary>
///     Represents an invoker for executing PowerShell processes, providing additional configurations
///     and
///     methods to run processes in buffered, piped, or standard modes.
/// </summary>
/// <remarks>
///     The <c>PowershellProcessInvoker</c> is now a thin convenience wrapper around
///     <see cref="CliInvoke.ProcessInvoker"/> with <see cref="CliInvoke.Specializations.Middleware.PowerShellMiddleware"/>
///     applied. The middleware (which delegates shell-flag and target resolution to
///     <see cref="CliInvoke.Specializations.Configurations.PowershellProcessConfiguration"/>) is the single source of truth for PowerShell
///     wrapping; this class simply forwards each invocation.
///     <para>
///         Window creation and shell-execution semantics use the unified defaults
///         (<c>windowCreation = false</c>, <c>useShellExecution = false</c>), matching
///         <see cref="CliInvoke.Specializations.Middleware.PowerShellMiddleware"/> and
///         <see cref="ProcessConfiguration"/>. To run a command inside PowerShell with non-default
///         window-creation or shell-execution behaviour, prefer the
///         <see cref="CliInvoke.ProcessInvoker"/> middleware path directly via
///         <c>UsePowerShell(windowCreation, useShellExecution)</c>.
///     </para>
/// </remarks>
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("macos")]
[SupportedOSPlatform("linux")]
public class PowershellProcessInvoker : IProcessInvoker
{
    private readonly ProcessInvoker _processInvoker;

    /// <summary>
    ///     Initialises a new instance of the <see cref="PowershellProcessInvoker"/> class.
    /// </summary>
    /// <param name="filePathResolver">
    ///     The resolver used to locate the <c>pwsh</c> / <c>pwsh.exe</c> executable.
    /// </param>
    /// <param name="externalProcessFactory">The factory used to create external processes.</param>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public PowershellProcessInvoker(
        IFilePathResolver filePathResolver,
        IExternalProcessFactory externalProcessFactory)
    {
        _processInvoker =
            new ProcessInvoker(externalProcessFactory)
                .UsePowerShell(filePathResolver, windowCreation: false, useShellExecution: false);
    }

    /// <summary>
    ///     Executes a PowerShell process asynchronously using the specified configuration.
    /// </summary>
    /// <param name="processConfiguration">The configuration of the process to execute.</param>
    /// <param name="processExitConfiguration">
    ///     Optional configuration for handling the process exit
    ///     behaviour. Defaults to null.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to cancel the asynchronous operation. Defaults to
    ///     CancellationToken.None.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a
    ///     <see cref="ProcessResult" /> object with the details of the process execution outcome.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    ///     Thrown when the execution is attempted on an
    ///     unsupported platform such as Android, iOS, tvOS, or a browser environment.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        return _processInvoker.ExecuteAsync(processConfiguration, processExitConfiguration, cancellationToken);
    }

    /// <summary>
    ///     Executes a PowerShell process asynchronously with buffered input and output.
    /// </summary>
    /// <param name="processConfiguration">The configuration of the process to execute.</param>
    /// <param name="processExitConfiguration">
    ///     Optional configuration for handling the process exit
    ///     behaviour. Defaults to null.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to cancel the asynchronous operation. Defaults to
    ///     CancellationToken.None.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a
    ///     <see cref="BufferedProcessResult" /> object with the details of the process execution outcome.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    ///     Thrown when the execution is attempted on an
    ///     unsupported platform such as Android, iOS, tvOS, or a browser environment.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        return _processInvoker.ExecuteBufferedAsync(processConfiguration, processExitConfiguration, cancellationToken);
    }

    /// <summary>
    ///     Executes a PowerShell process asynchronously with piped input and output.
    /// </summary>
    /// <param name="processConfiguration">The configuration of the process to execute.</param>
    /// <param name="processExitConfiguration">
    ///     Optional configuration for handling the process exit
    ///     behaviour. Defaults to null.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token to cancel the asynchronous operation. Defaults to
    ///     CancellationToken.None.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a
    ///     <see cref="PipedProcessResult" /> object with the details of the process execution outcome.
    /// </returns>
    /// <exception cref="PlatformNotSupportedException">
    ///     Thrown when the execution is attempted on an
    ///     unsupported platform such as Android, iOS, tvOS, or a browser environment.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        return _processInvoker.ExecutePipedAsync(processConfiguration, processExitConfiguration, cancellationToken);
    }
}
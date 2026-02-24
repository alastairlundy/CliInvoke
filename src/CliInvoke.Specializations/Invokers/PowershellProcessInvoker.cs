/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/


using System.Threading;
using System.Threading.Tasks;

using CliInvoke.Specializations.Configurations;

using WhatExec.Lib.Abstractions;

namespace CliInvoke.Specializations;

/// <summary>
/// Represents an invoker for executing PowerShell processes, providing additional configurations and
/// methods to run processes in buffered, piped, or standard modes.
/// </summary>
/// <remarks>
/// The <c>PowershellProcessInvoker</c> class specializes in executing commands via PowerShell, utilizing the
/// underlying process invoker functionality. It is designed for scenarios where PowerShell-specific process
/// handling and configurations are required, such as redirecting outputs or managing window creation.
/// </remarks>
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("macos")]
[SupportedOSPlatform("linux")]
public class PowershellProcessInvoker : RunnerProcessInvokerBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processInvoker"></param>
    /// <param name="runnerProcessFactory"></param>
    /// <param name="filePathResolver"></param>
    /// <param name="windowCreation"></param>
    /// <param name="redirectOutputs"></param>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    public PowershellProcessInvoker(IProcessInvoker processInvoker,
        IRunnerProcessFactory runnerProcessFactory, IExecutableFileResolver filePathResolver,
        bool windowCreation = true, bool redirectOutputs = true)
        : base(processInvoker, runnerProcessFactory, new PowershellProcessConfiguration(
            filePathResolver, "", false, redirectOutputs, redirectOutputs,
            windowCreation: windowCreation))
    {
    }

    /// <summary>
    /// Executes a PowerShell process asynchronously using the specified configuration.
    /// </summary>
    /// <param name="processConfiguration">The configuration of the process to execute.</param>
    /// <param name="processExitConfiguration">Optional configuration for handling the process exit behaviour. Defaults to null.</param>
    /// <param name="disposeOfConfig">Specifies whether to dispose of the configuration after execution. Defaults to true.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ProcessResult"/> object with the details of the process execution outcome.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the execution is attempted on an unsupported platform such as Android, iOS, tvOS, or a browser environment.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    public new Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null, bool disposeOfConfig = true,
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException();
        
        return base.ExecuteAsync(processConfiguration, processExitConfiguration, disposeOfConfig, cancellationToken);
    }

    /// <summary>
    /// Executes a PowerShell process asynchronously with buffered input and output.
    /// </summary>
    /// <param name="processConfiguration">The configuration of the process to execute.</param>
    /// <param name="processExitConfiguration">Optional configuration for handling the process exit behaviour. Defaults to null.</param>
    /// <param name="disposeOfConfig">Specifies whether to dispose of the configuration after execution. Defaults to true.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="BufferedProcessResult"/> object with the details of the process execution outcome.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the execution is attempted on an unsupported platform such as Android, iOS, tvOS, or a browser environment.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    public new Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null, bool disposeOfConfig = true,
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException(Resources.Exceptions_Powershell_OnlySupportedOnDesktop);
        
        return base.ExecuteBufferedAsync(processConfiguration, processExitConfiguration, disposeOfConfig, cancellationToken);
    }

    /// <summary>
    /// Executes a PowerShell process asynchronously with piped input and output.
    /// </summary>
    /// <param name="processConfiguration">The configuration of the process to execute.</param>
    /// <param name="processExitConfiguration">Optional configuration for handling the process exit behaviour. Defaults to null.</param>
    /// <param name="disposeOfConfig">Specifies whether to dispose of the configuration after execution. Defaults to true.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation. Defaults to CancellationToken.None.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="PipedProcessResult"/> object with the details of the process execution outcome.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the execution is attempted on an unsupported platform such as Android, iOS, tvOS, or a browser environment.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    public new Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null, bool disposeOfConfig = true,
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException(Resources.Exceptions_Powershell_OnlySupportedOnDesktop);

        return base.ExecutePipedAsync(processConfiguration, processExitConfiguration, disposeOfConfig, cancellationToken);
    }
}
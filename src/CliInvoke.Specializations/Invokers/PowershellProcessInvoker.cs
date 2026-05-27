/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/


using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;
using CliInvoke.Specializations.Configurations;

namespace CliInvoke.Specializations;

/// <summary>
///     Represents an invoker for executing PowerShell processes, providing additional configurations
///     and
///     methods to run processes in buffered, piped, or standard modes.
/// </summary>
/// <remarks>
///     The <c>PowershellProcessInvoker</c> class specialises in executing commands via PowerShell,
///     utilising the
///     underlying process invoker functionality. It is designed for scenarios where
///     PowerShell-specific process
///     handling and configurations are required, such as redirecting outputs or managing window
///     creation.
/// </remarks>
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("macos")]
[SupportedOSPlatform("linux")]
public class PowershellProcessInvoker : IProcessInvoker
{
    private readonly IRunnerConfigurationFactory _runnerConfigurationFactory;
    
    private readonly bool _windowCreation;
    private readonly bool _useShellExecution;
    private readonly IFilePathResolver _filePathResolver;
    private readonly IExternalProcessFactory _externalProcessFactory;

    /// <summary>
    /// </summary>
    /// <param name="runnerConfigurationFactory"></param>
    /// <param name="filePathResolver"></param>
    /// <param name="externalProcessFactory"></param>
    /// <param name="windowCreation"></param>
    /// <param name="useShellExecution"></param>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public PowershellProcessInvoker(
        IRunnerConfigurationFactory runnerConfigurationFactory, IFilePathResolver filePathResolver,
        IExternalProcessFactory externalProcessFactory,
        bool windowCreation = true, bool useShellExecution = false)
    {
        _runnerConfigurationFactory = runnerConfigurationFactory;
        _windowCreation = windowCreation;
        _useShellExecution = useShellExecution;

        _filePathResolver = filePathResolver;
        _externalProcessFactory = externalProcessFactory;
    }

    private ProcessConfiguration GetPowershellProcessConfiguration(bool redirectOutputs)
    {
        return new PowershellProcessConfiguration(
            _filePathResolver, arguments: "-NoProfile -NonInteractive -Command", false, redirectOutputs,
            Directory.GetCurrentDirectory(),
            windowCreation: _windowCreation, useShellExecution: _useShellExecution);
    }
    
    private static void ThrowIfUnsupported()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException();
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
    public async Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupported();
        
        using ProcessConfiguration runnerConfiguration =
            _runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration,
                GetPowershellProcessConfiguration(false));

        using IExternalProcess externalProcess = _externalProcessFactory.CreateExternalProcess(processConfiguration,
            processExitConfiguration ?? ProcessExitConfiguration.Default);

        await externalProcess.StartAsync(cancellationToken);

        return await externalProcess.WaitForExitOrTimeoutAsync(cancellationToken);
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
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupported();
        
        using ProcessConfiguration runnerConfiguration =
            _runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration,
                GetPowershellProcessConfiguration(true));

        using IExternalProcess externalProcess = _externalProcessFactory.CreateExternalProcess(processConfiguration,
            processExitConfiguration ?? ProcessExitConfiguration.Default);

        await externalProcess.StartAsync(cancellationToken);

        return await externalProcess.CaptureBufferedResultAsync(cancellationToken);
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
    public async Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null, 
        CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupported();
        
        using ProcessConfiguration runnerConfiguration =
            _runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration,
                GetPowershellProcessConfiguration(true));

        using IExternalProcess externalProcess = _externalProcessFactory.CreateExternalProcess(processConfiguration,
            processExitConfiguration ?? ProcessExitConfiguration.Default);

        await externalProcess.StartAsync(cancellationToken);

        return await externalProcess.CapturePipedResultAsync(cancellationToken);
    }
}
/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Extensibility.Factories;

namespace CliInvoke.Core.Extensibility;

/// <summary>
/// An abstract invoker class that runs other Process Configurations through its own Process.
/// </summary>
/// <remarks>Users should implement this abstract class when exposing an invoker that is easier to set up with Dependency Injection is desired
/// </remarks>
public abstract class RunnerProcessInvokerBase : IProcessInvoker, IDisposable
{
    private readonly IProcessInvoker _processInvoker;
    private readonly IRunnerProcessFactory _runnerProcessFactory;

    /// <summary>
    /// Gets the <see cref="ProcessConfiguration"/> object that represents the configuration
    /// for the runner process.
    /// </summary>
    public ProcessConfiguration RunnerProcessConfiguration { get; }

    /// <summary>
    /// Represents an abstract base class for implementing process invokers that leverage their own configuration and execution pipeline
    /// to invoke other processes with a variety of configurations.
    /// </summary>
    /// <remarks>
    /// Note to Implementers: Implementing classes don't need to publicly require users to provide their own <paramref name="runnerProcessConfiguration"/> if the class
    /// provides a <see cref="ProcessConfiguration"/> for the runner process configuration.
    /// </remarks>
    /// <param name="processInvoker">The <see cref="IProcessInvoker"/> to use to invoke the actual runner configuration.</param>
    /// <param name="runnerProcessFactory">The <see cref="IRunnerProcessFactory"/> to use to create the actual runner configuration from the
    /// input process configuration and the runner process configuration.</param>
    /// <param name="runnerProcessConfiguration">The process configuration of the process to run other process configurations through.</param>
    protected RunnerProcessInvokerBase(
        IProcessInvoker processInvoker,
        IRunnerProcessFactory runnerProcessFactory,
        ProcessConfiguration runnerProcessConfiguration
    )
    {
        _processInvoker = processInvoker;
        _runnerProcessFactory = runnerProcessFactory;
        RunnerProcessConfiguration = runnerProcessConfiguration;
    }

    /// <summary>
    /// Executes a process using the specified configurations and returns the result of the process execution.
    /// </summary>
    /// <param name="processConfiguration">The configuration for the process that will be executed.</param>
    /// <param name="processExitConfiguration">The configuration defining behaviour for process exit, if any.</param>
    /// <param name="disposeOfConfig">Specifies whether the provided process configuration should be disposed of after execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the process execution.</param>
    /// <returns>A task representing the asynchronous execution, containing the result of the executed process.</returns>
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("watchos")]
    [Pure]
    public virtual async Task<ProcessResult> ExecuteAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = true,
        CancellationToken cancellationToken = default
    )
    {
        ProcessConfiguration runnerConfiguration = _runnerProcessFactory.CreateRunnerConfiguration(
            processConfiguration,
            RunnerProcessConfiguration
        );

        return await _processInvoker.ExecuteAsync(
            runnerConfiguration,
            processExitConfiguration,
            disposeOfConfig,
            cancellationToken
        );
    }

    /// <summary>
    /// Executes a process using the specified configurations and returns the buffered result of the process execution.
    /// </summary>
    /// <param name="processConfiguration">The configuration for the process to be executed.</param>
    /// <param name="processExitConfiguration">Optional configuration that defines behaviour for process exit.</param>
    /// <param name="disposeOfConfig">Specifies whether the provided process configuration should be disposed of after execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the process execution.</param>
    /// <returns>A task representing the asynchronous execution, containing the buffered result of the executed process.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("watchos")]
    [Pure]
    public virtual async Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = true,
        CancellationToken cancellationToken = default
    )
    {
        ProcessConfiguration runnerConfiguration = _runnerProcessFactory.CreateRunnerConfiguration(
            processConfiguration,
            RunnerProcessConfiguration
        );

        return await _processInvoker.ExecuteBufferedAsync(
            runnerConfiguration,
            processExitConfiguration,
            disposeOfConfig,
            cancellationToken
        );
    }

    /// <summary>
    /// Executes a piped process using the specified configurations and returns the result of the process execution.
    /// </summary>
    /// <param name="processConfiguration">The configuration for the process that will be executed.</param>
    /// <param name="processExitConfiguration">The configuration defining behaviour for process exit, if any. This parameter is optional.</param>
    /// <param name="disposeOfConfig">Specifies whether the provided process configuration should be disposed of after execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the process execution. This parameter is optional.</param>
    /// <returns>A task representing the asynchronous execution, containing the piped result of the executed process.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("watchos")]
    [Pure]
    public virtual async Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        bool disposeOfConfig = true,
        CancellationToken cancellationToken = default
    )
    {
        ProcessConfiguration runnerConfiguration = _runnerProcessFactory.CreateRunnerConfiguration(
            processConfiguration,
            RunnerProcessConfiguration
        );

        return await _processInvoker.ExecutePipedAsync(
            runnerConfiguration,
            processExitConfiguration,
            disposeOfConfig,
            cancellationToken
        );
    }

    /// <summary>
    /// Releases the resources used by the RunnerProcessInvoker and associated configurations.
    /// </summary>
    public void Dispose()
    {
        RunnerProcessConfiguration.Dispose();
    }
}

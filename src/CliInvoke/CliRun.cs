/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Factories;
using CliInvoke.Factories;
using CliInvoke.Piping;

namespace CliInvoke;

/// <summary>
/// Provides static beginner-friendly methods for executing command-line processes
/// with various configurations and behaviours.
/// </summary>
public static class CliRun
{
    private static Func<IProcessInvoker> _processInvokerFactory = () => new 
        ProcessInvoker(FilePathResolver.Shared, ProcessPipeHandler.Shared);

    private static Func<IProcessConfigurationFactory> _processConfigMetaFactory = ()
        => new ProcessConfigurationFactory();
    
    /// <summary>
    /// Configures the process invoker to be used for executing command-line processes.
    /// </summary>
    /// <param name="processInvoker">
    /// An implementation of the <see cref="IProcessInvoker"/> interface, which defines the logic for creating
    /// and managing process executions. This parameter allows customisation of process invocation behaviour.
    /// </param>
    public static void UseProcessInvoker(IProcessInvoker processInvoker)
    {
        _processInvokerFactory = () => processInvoker;
    }

    /// <summary>
    /// Configures the factory to be used for creating instances of <see cref="ProcessConfiguration"/>.
    /// </summary>
    /// <param name="processConfigurationFactory">
    /// An implementation of the <see cref="IProcessConfigurationFactory"/> interface, which facilitates
    /// the creation of <see cref="ProcessConfiguration"/> objects. This parameter allows for defining
    /// custom logic in creating process configurations.
    /// </param>
    public static void UseProcessConfigurationFactory(
        IProcessConfigurationFactory processConfigurationFactory)
    {
        _processConfigMetaFactory = () => processConfigurationFactory;
    }
    
    private static IProcessInvoker GetInvoker() 
        => _processInvokerFactory.Invoke();
    
    private static IProcessConfigurationFactory GetProcessConfigFactory()
        => _processConfigMetaFactory.Invoke();

    /// <summary>
    /// Executes a specified process with the provided parameters asynchronously and returns the resulting process data.
    /// </summary>
    /// <param name="targetFilePath">
    /// The path of the executable file to be run.
    /// </param>
    /// <param name="arguments">
    /// Command-line arguments for the executable. Defaults to an empty string if not specified.
    /// </param>
    /// <param name="workingDirectory">
    /// The directory in which the process will be executed. If null, the current directory is used.
    /// </param>
    /// <param name="timeoutTimeSpan">
    /// The maximum duration that the process is allowed to run before it times out. If null, a default value is applied.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can signal the cancellation of the operation before its completion.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a <see cref="ProcessResult"/> object
    /// containing details of the executed process, including exit status and runtime information.
    /// </returns>
    public static async Task<ProcessResult> RunAsync(string targetFilePath,
        string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        workingDirectory ??= Environment.CurrentDirectory;

        using ProcessConfiguration configuration = GetProcessConfigFactory().Create(targetFilePath,
            arguments, workingDirectory);
        
        timeoutTimeSpan ??= ProcessTimeoutPolicy.Default.TimeoutThreshold;

        ProcessExitConfiguration exitConfiguration = new ProcessExitConfiguration(
            ProcessTimeoutPolicy.FromTimeSpan((TimeSpan)timeoutTimeSpan));
        
        return await GetInvoker().ExecuteAsync(configuration, exitConfiguration, false,
            cancellationToken);
    }

    /// <summary>
    /// Executes a process asynchronously with the specified configuration.
    /// </summary>
    /// <param name="configuration">
    /// The process configuration defining how to run the process, including settings such as working directory, timeout, and other parameters.
    /// </param>
    /// <param name="exitConfiguration">
    /// The configuration that determines how the process is terminated; defaults to a graceful configuration if not provided.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that, if cancelled, will be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The result of the process execution.
    /// </returns>
    public static async Task<ProcessResult> RunAsync(ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        exitConfiguration ??= ProcessExitConfiguration.Default;

        return await GetInvoker().ExecuteAsync(configuration, 
            exitConfiguration, false, cancellationToken);
    }

    /// <summary>
    /// Executes a specified process asynchronously with the provided parameters and returns the buffered process result.
    /// </summary>
    /// <param name="targetFilePath">
    /// The path of the executable file to run.
    /// </param>
    /// <param name="arguments">
    /// Command-line arguments to pass to the executable. Defaults to an empty string if not specified.
    /// </param>
    /// <param name="workingDirectory">
    /// The working directory for the process. If null, the current directory is used.
    /// </param>
    /// <param name="timeoutTimeSpan">
    /// The maximum duration that the process can run before timing out. If null, a default timeout is applied.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be cancelled before it completes.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a <see cref="BufferedProcessResult"/>
    /// object containing the full output of the process and execution details.
    /// </returns>
    public static async Task<BufferedProcessResult> RunBufferedAsync(string targetFilePath,
        string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        workingDirectory ??= Environment.CurrentDirectory;

        using ProcessConfiguration configuration = GetProcessConfigFactory().Create(targetFilePath,
            arguments, workingDirectory);
        
        timeoutTimeSpan ??= ProcessTimeoutPolicy.Default.TimeoutThreshold;

        ProcessExitConfiguration exitConfiguration = new ProcessExitConfiguration(
            ProcessTimeoutPolicy.FromTimeSpan((TimeSpan)timeoutTimeSpan));
        
        return await GetInvoker().ExecuteBufferedAsync(configuration, exitConfiguration, false,
            cancellationToken);
    }


    /// <summary>
    /// Executes a process asynchronously with the specified configuration and returns the buffered process result.
    /// </summary>
    /// <param name="configuration">
    /// The process configuration defining how to run the process, including settings such as working directory, timeout, and other parameters.
    /// </param>
    /// <param name="exitConfiguration">
    /// The configuration that determines how the process is terminated; defaults to a graceful configuration if not provided.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that, if cancelled, will be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a <see cref="BufferedProcessResult"/>
    /// object containing the full output of the process and execution details.
    /// </returns>
    public static async Task<BufferedProcessResult> RunBufferedAsync(
        ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null, CancellationToken cancellationToken = default)
    {
        exitConfiguration ??= ProcessExitConfiguration.Default;

        return await GetInvoker().ExecuteBufferedAsync(configuration, 
            exitConfiguration, false, cancellationToken);
    }

    /// <summary>
    /// Executes a process with the specified parameters and returns a result containing the process's piped data and exit information.
    /// </summary>
    /// <param name="targetFilePath">
    /// The file path of the target executable to be run.
    /// </param>
    /// <param name="arguments">
    /// The command-line arguments to pass to the executable. Defaults to an empty string if not specified.
    /// </param>
    /// <param name="workingDirectory">
    /// The working directory in which the process will run. If null, the current directory is used.
    /// </param>
    /// <param name="timeoutTimeSpan">
    /// The maximum allowed duration for the process to complete before timing out. If null, a default timeout is applied.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be cancelled before completion.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="PipedProcessResult"/> object
    /// with details about the executed process, including piped output and exit status.
    /// </returns>
    public static async Task<PipedProcessResult> RunPipedAsync(string targetFilePath,
        string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        workingDirectory ??= Environment.CurrentDirectory;
        
        using ProcessConfiguration configuration = GetProcessConfigFactory().Create(targetFilePath,
            arguments, workingDirectory);
        
        timeoutTimeSpan ??= ProcessTimeoutPolicy.Default.TimeoutThreshold;

        ProcessExitConfiguration exitConfiguration = new ProcessExitConfiguration(
            ProcessTimeoutPolicy.FromTimeSpan((TimeSpan)timeoutTimeSpan));
        
        return await GetInvoker().ExecutePipedAsync(configuration, exitConfiguration, false,
            cancellationToken);
    }

    /// <summary>
    /// Executes a process using the specified configuration and returns a result containing piped process data.
    /// </summary>
    /// <param name="configuration">
    /// The configuration for the process to be executed, including details such as file path, arguments, and environment settings.
    /// </param>
    /// <param name="exitConfiguration">
    /// An optional configuration for managing the process exit behaviour. If null, a default exit configuration is used.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be cancelled before completion.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a <see cref="PipedProcessResult"/> object
    /// with details about the executed process, including piped output and exit information.
    /// </returns>
    public static async Task<PipedProcessResult> RunPipedAsync(
        ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        exitConfiguration ??= ProcessExitConfiguration.Default;
        
        return await GetInvoker().ExecutePipedAsync(configuration,
            exitConfiguration, false, cancellationToken);
    }
}
/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Processes;
using CliInvoke.Factories;

namespace CliInvoke;

/// <summary>
/// Provides static beginner-friendly methods for executing command-line processes
/// with various configurations and behaviours.
/// </summary>
/// <remarks>
/// <see cref="CliRun"/> is a batteries-included defaults facade. Every call allocates a
/// fresh <see cref="ProcessInvocationPipeline"/> (and, underneath, a fresh
/// <see cref="ExternalProcessFactory"/> with a default <see cref="IFilePathResolver"/>),
/// so there is no process-wide mutable state to configure or to leak between calls.
/// Callers that need a custom factory or resolver should construct an
/// <see cref="IProcessInvoker"/> (or resolve one from the DI container) instead.
/// </remarks>
public static class CliRun
{
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
    /// The maximum duration that the process is allowed to run before it times out. If null,
    /// the default is <see cref="ProcessTimeoutPolicy.Default"/>: enabled, with a 2-minute
    /// threshold. Reaching the threshold attempts a graceful interrupt and, if the process does
    /// not exit within a short grace period, force-terminates it. With the default graceful exit
    /// configuration no exception is thrown: the invocation completes normally with a result
    /// whose <c>Canceled</c> is <see langword="true" /> and the kill exit code.
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
        (ProcessConfiguration configuration, ProcessExitConfiguration exitConfiguration) =
            BuildStringArgsConfig(targetFilePath, arguments, workingDirectory, timeoutTimeSpan, outputRedirection: false);

        return await RunAsync(configuration, exitConfiguration, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a specified process with the provided argument list asynchronously and returns the resulting process data.
    /// </summary>
    /// <remarks>
    /// Each entry in <paramref name="arguments"/> is passed as an individual element of
    /// <see cref="ProcessConfiguration.ArgumentList"/>, so the operating-system command-line
    /// parser delivers each entry to the child process unmodified. This is the safe path for
    /// shell wrappers whose own parser would otherwise re-interpret a single tokenised string.
    /// </remarks>
    /// <param name="targetFilePath">
    /// The path of the executable file to be run.
    /// </param>
    /// <param name="arguments">
    /// A sequence of individual arguments to pass to the executable.
    /// </param>
    /// <param name="workingDirectory">
    /// The directory in which the process will be executed. If null, the current directory is used.
    /// </param>
    /// <param name="timeoutTimeSpan">
    /// The maximum duration that the process is allowed to run before it times out. If null,
    /// the default is <see cref="ProcessTimeoutPolicy.Default"/> (enabled, 2-minute threshold):
    /// reaching the threshold attempts a graceful interrupt, then force-terminates the process,
    /// and the default graceful exit configuration returns a <c>Canceled</c> result with the
    /// kill exit code instead of throwing.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can signal the cancellation of the operation before its completion.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a <see cref="ProcessResult"/> object
    /// containing details of the executed process, including exit status and runtime information.
    /// </returns>
    public static async Task<ProcessResult> RunAsync(string targetFilePath,
        IEnumerable<string> arguments, string? workingDirectory = null,
        TimeSpan? timeoutTimeSpan = null, CancellationToken cancellationToken = default)
    {
        (ProcessConfiguration configuration, ProcessExitConfiguration exitConfiguration) =
            BuildArgumentListConfig(targetFilePath, arguments, workingDirectory,
                timeoutTimeSpan, outputRedirection: false);

        return await RunAsync(configuration, exitConfiguration, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a process asynchronously with the specified configuration.
    /// </summary>
    /// <remarks>
    ///     Raw mode does not capture output. When <paramref name="configuration"/> has
    ///     <see cref="ProcessConfiguration.OutputRedirection"/> enabled, the pipeline
    ///     forces it off for the invocation so the child inherits the parent's handles
    ///     instead of writing to a pipe that Raw mode never drains (the raw-mode pipe
    ///     deadlock). Nothing readable is lost: no public Raw-mode surface exposes
    ///     output. To capture output, use
    ///     <see cref="RunBufferedAsync(ProcessConfiguration, ProcessExitConfiguration?, CancellationToken)"/>.
    ///     Direct <see cref="IExternalProcess"/> users bypass this normalisation — see
    ///     <see cref="IExternalProcess.WaitForExitOrTimeoutAsync(CancellationToken)"/>.
    /// </remarks>
    /// <param name="configuration">
    /// The process configuration defining how to run the process, including settings such as working directory, timeout, and other parameters.
    /// </param>
    /// <param name="exitConfiguration">
    /// The configuration that determines how the process is terminated; defaults to
    /// <see cref="ProcessExitConfigurationCreationExtensions.CreateGraceful()"/> if not provided, which
    /// suppresses unexpected-exit exceptions and returns a <c>Canceled</c> result on
    /// timeout or cancellation. Pass <c>ProcessExitConfiguration.Default</c>
    /// (equivalent to <c>new ProcessExitConfiguration()</c>) to allow unexpected-exit
    /// exceptions instead.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that, if cancelled, will be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The result of the process execution.
    /// </returns>
    public static Task<ProcessResult> RunAsync(ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        InvocationContext ctx = new(
            configuration,
            exitConfiguration ?? ProcessExitConfiguration.CreateGraceful(),
            InvocationMode.Raw,
            cancellationToken);

        return new ProcessInvocationPipeline(new ExternalProcessFactory()).InvokeAsync<ProcessResult>(ctx);
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
    /// The maximum duration that the process can run before timing out. If null, the default
    /// is <see cref="ProcessTimeoutPolicy.Default"/> (enabled, 2-minute threshold): reaching
    /// the threshold attempts a graceful interrupt, then force-terminates the process, and the
    /// default graceful exit configuration returns a <c>Canceled</c> result with the kill exit
    /// code instead of throwing.
    /// </param>
    /// <param name="maxBufferedOutputBytes">
    /// The maximum number of bytes to buffer before truncation is applied, applied
    /// independently to standard output and to standard error. If null, the default cap is
    /// used. Applies only to buffered capture.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be cancelled before it completes.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a <see cref="BufferedProcessResult"/>
    /// object containing the full output of the process and execution details. When the operation is
    /// cancelled mid-run, a normal result with <c>Canceled</c> set to <see langword="true"/> and empty
    /// captured output is returned instead of throwing, because the applied graceful exit configuration
    /// leaves <see cref="ProcessExitConfiguration.CancellationThrowsException"/> false; to receive an
    /// <see cref="OperationCanceledException"/> instead, use the
    /// <see cref="RunBufferedAsync(ProcessConfiguration, ProcessExitConfiguration?, CancellationToken)"/>
    /// overload and set <c>CancellationThrowsException</c> to <see langword="true"/>.
    /// </returns>
    public static async Task<BufferedProcessResult> RunBufferedAsync(string targetFilePath,
        string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
        long? maxBufferedOutputBytes = null, CancellationToken cancellationToken = default)
    {
        (ProcessConfiguration configuration, ProcessExitConfiguration exitConfiguration) =
            BuildStringArgsConfig(targetFilePath, arguments, workingDirectory, timeoutTimeSpan, outputRedirection: true, maxBufferedOutputBytes);

        return await RunBufferedAsync(configuration, exitConfiguration, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a specified process with the provided argument list asynchronously and returns the buffered process result.
    /// </summary>
    /// <remarks>
    /// Each entry in <paramref name="arguments"/> is passed as an individual element of
    /// <see cref="ProcessConfiguration.ArgumentList"/>, so the operating-system command-line
    /// parser delivers each entry to the child process unmodified. This is the safe path for
    /// shell wrappers whose own parser would otherwise re-interpret a single tokenised string.
    /// </remarks>
    /// <param name="targetFilePath">
    /// The path of the executable file to run.
    /// </param>
    /// <param name="arguments">
    /// A sequence of individual arguments to pass to the executable.
    /// </param>
    /// <param name="workingDirectory">
    /// The working directory for the process. If null, the current directory is used.
    /// </param>
    /// <param name="timeoutTimeSpan">
    /// The maximum duration that the process can run before timing out. If null, the default
    /// is <see cref="ProcessTimeoutPolicy.Default"/> (enabled, 2-minute threshold): reaching
    /// the threshold attempts a graceful interrupt, then force-terminates the process, and the
    /// default graceful exit configuration returns a <c>Canceled</c> result with the kill exit
    /// code instead of throwing.
    /// </param>
    /// <param name="maxBufferedOutputBytes">
    /// The maximum number of bytes to buffer before truncation is applied, applied
    /// independently to standard output and to standard error. If null, the default cap is
    /// used. Applies only to buffered capture.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be cancelled before it completes.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a <see cref="BufferedProcessResult"/>
    /// object containing the full output of the process and execution details. When the operation is
    /// cancelled mid-run, a normal result with <c>Canceled</c> set to <see langword="true"/> and empty
    /// captured output is returned instead of throwing, because the applied graceful exit configuration
    /// leaves <see cref="ProcessExitConfiguration.CancellationThrowsException"/> false; to receive an
    /// <see cref="OperationCanceledException"/> instead, use the
    /// <see cref="RunBufferedAsync(ProcessConfiguration, ProcessExitConfiguration?, CancellationToken)"/>
    /// overload and set <c>CancellationThrowsException</c> to <see langword="true"/>.
    /// </returns>
    public static async Task<BufferedProcessResult> RunBufferedAsync(
        string targetFilePath, IEnumerable<string> arguments,
        string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
        long? maxBufferedOutputBytes = null,
        CancellationToken cancellationToken = default)
    {
        (ProcessConfiguration configuration, ProcessExitConfiguration exitConfiguration) =
            BuildArgumentListConfig(targetFilePath, arguments, workingDirectory,
                timeoutTimeSpan, outputRedirection: true, maxBufferedOutputBytes);

        return await RunBufferedAsync(configuration, exitConfiguration, cancellationToken)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Executes a process asynchronously with the specified configuration and returns buffered results.
    /// </summary>
    /// <param name="configuration">
    /// The process configuration defining how to run the process.
    /// </param>
    /// <param name="exitConfiguration">
    /// The configuration that determines how the process is terminated; defaults to
    /// <see cref="ProcessExitConfigurationCreationExtensions.CreateGraceful()"/> if not provided, which
    /// suppresses unexpected-exit exceptions and returns a <c>Canceled</c> result on
    /// timeout or cancellation. Pass <c>ProcessExitConfiguration.Default</c>
    /// (equivalent to <c>new ProcessExitConfiguration()</c>) to allow unexpected-exit
    /// exceptions instead.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that, if cancelled, will be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The buffered result of the process execution. When cancellation occurs and
    /// <see cref="ProcessExitConfiguration.CancellationThrowsException"/> on
    /// <paramref name="exitConfiguration"/> is <see langword="false"/> (the default when no exit
    /// configuration is supplied), a normal result with <c>Canceled</c> set to
    /// <see langword="true"/> and empty captured output is returned instead of throwing; set
    /// <c>CancellationThrowsException</c> to <see langword="true"/> to receive an
    /// <see cref="OperationCanceledException"/> instead.
    /// </returns>
    public static Task<BufferedProcessResult> RunBufferedAsync(
        ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null, CancellationToken cancellationToken = default)
    {
        InvocationContext ctx = new(
            configuration,
            exitConfiguration ?? ProcessExitConfiguration.CreateGraceful(),
            InvocationMode.Buffered,
            cancellationToken);

        return new ProcessInvocationPipeline(new ExternalProcessFactory()).InvokeAsync<BufferedProcessResult>(ctx);
    }


    /// <summary>
    ///     Starts a process using the specified configuration and returns its process ID without
    ///     waiting for it to exit. The process handle is disposed after starting.
    /// </summary>
    /// <param name="configuration">The configuration settings for starting the process.</param>
    /// <returns>The process ID of the started process.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="configuration" /> enables
    ///     <see cref="ProcessConfiguration.OutputRedirection" /> or
    ///     <see cref="ProcessConfiguration.RedirectStandardInput" />. The process handle is
    ///     disposed immediately after start, so no one drains the redirected pipes and the
    ///     child would hang or fail silently; use
    ///     <see cref="RunAsync(ProcessConfiguration, ProcessExitConfiguration?, CancellationToken)" />
    ///     or
    ///     <see cref="RunBufferedAsync(ProcessConfiguration, ProcessExitConfiguration?, CancellationToken)" />
    ///     instead. The string-arguments overload forces output redirection off and is safe.
    ///     Note that the position-argument <see cref="ProcessConfiguration" /> constructors
    ///     default <c>outputRedirection</c> to <see langword="true" />, so pass
    ///     <see langword="false" /> explicitly when building a configuration for this method.
    /// </exception>
    public static int FireAndForget(ProcessConfiguration configuration)
    {
        if (configuration.OutputRedirection || configuration.RedirectStandardInput)
            throw new ArgumentException(
                Resources.Exceptions_Invocation_FireAndForget_RedirectionNotSupported,
                nameof(configuration));

        using IExternalProcess p = new ExternalProcessFactory().CreateExternalProcess(configuration);
        return p.Start();
    }

    /// <summary>
    ///     Starts a process using the specified file path and returns its process ID without
    ///     waiting for it to exit.
    /// </summary>
    /// <param name="targetFilePath">The path of the executable file to start.</param>
    /// <param name="arguments">Command-line arguments for the executable. Defaults to an empty string if not specified.</param>
    /// <param name="workingDirectory">The working directory for the process. If null, the current directory is used.</param>
    /// <returns>The process ID of the started process.</returns>
    public static int FireAndForget(string targetFilePath, string arguments = "", string? workingDirectory = null)
    {
        // ExitConfiguration is unused by FireAndForget
        workingDirectory ??= Environment.CurrentDirectory;

        ProcessConfiguration configuration = new(targetFilePath, arguments)
            { OutputRedirection = false, WorkingDirectoryPath = workingDirectory };

        return FireAndForget(configuration);
    }

    /// <summary>
    ///     Builds the <see cref="ProcessConfiguration" /> and <see cref="ProcessExitConfiguration" />
    ///     shared by the string-argument overloads, applying the default working directory and timeout policy.
    /// </summary>
    private static (ProcessConfiguration Configuration, ProcessExitConfiguration ExitConfiguration) BuildStringArgsConfig(
        string targetFilePath, string arguments, string? workingDirectory, TimeSpan? timeoutTimeSpan, bool outputRedirection,
        long? maxBufferedOutputBytes = null)
    {
        workingDirectory ??= Environment.CurrentDirectory;

        ProcessConfiguration configuration = new(
            targetFilePath, arguments, outputRedirection) { WorkingDirectoryPath = workingDirectory };

        ProcessExitConfiguration exitConfiguration = ProcessExitConfiguration.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(
                ProcessTimeoutPolicy.FromTimeSpan(timeoutTimeSpan ?? ProcessTimeoutPolicy.Default.TimeoutThreshold)),
            maxBufferedOutputBytes);

        return (configuration, exitConfiguration);
    }

    /// <summary>
    ///     Builds the <see cref="ProcessConfiguration" /> and <see cref="ProcessExitConfiguration" />
    ///     shared by the argument-list overloads, applying the default working directory and timeout policy.
    /// </summary>
    private static (ProcessConfiguration Configuration, ProcessExitConfiguration ExitConfiguration)
        BuildArgumentListConfig(
            string targetFilePath, IEnumerable<string> arguments,
            string? workingDirectory, TimeSpan? timeoutTimeSpan,
            bool outputRedirection, long? maxBufferedOutputBytes = null)
    {
        workingDirectory ??= Environment.CurrentDirectory;

        ProcessConfiguration configuration = new(
                targetFilePath, arguments, outputRedirection)
            { WorkingDirectoryPath = workingDirectory };

        ProcessExitConfiguration exitConfiguration =
            ProcessExitConfiguration.WithMaxBufferedOutputBytes(
                ProcessExitConfiguration.CreateGraceful(
                    ProcessTimeoutPolicy.FromTimeSpan(
                        timeoutTimeSpan ??
                        ProcessTimeoutPolicy.Default.TimeoutThreshold)),
                maxBufferedOutputBytes);

        return (configuration, exitConfiguration);
    }
}
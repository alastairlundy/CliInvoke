/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Processes;
using CliInvoke.Processes.Internal;

namespace CliInvoke.Processes;

/// <summary>
///     Wraps a <see cref="System.Diagnostics.Process"/> for managed lifecycle control.
/// </summary>
// ReSharper disable once RedundantExtendsListEntry
public sealed class ExternalProcess : ISuspendableExternalProcess, IExternalProcess
{
    private ProcessWrapper _processWrapper;
    private readonly object _lifecycleLock = new();
    private EventHandler? _startedHandler;
    private EventHandler? _exitedHandler;
    private bool _disposed;

    private readonly IFilePathResolver _filePathResolver;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathResolver"></param>
    /// <param name="configuration"></param>
    /// <param name="exitConfiguration"></param>
    public ExternalProcess(IFilePathResolver filePathResolver, ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null)
    {
        _filePathResolver = filePathResolver;
        _processWrapper = new ProcessWrapper(configuration, _filePathResolver.ResolveFilePath(configuration.TargetFilePath));
        Configuration = configuration;
        ExitConfiguration = exitConfiguration ?? ProcessExitConfiguration.CreateGraceful();

        _startedHandler = (sender, args) => Started?.Invoke(sender, args);
        _exitedHandler = (sender, args) => Exited?.Invoke(sender, args);
        _processWrapper.Started += _startedHandler;
        _processWrapper.Exited += _exitedHandler;
    }

/// <summary>
///     Gets the <see cref="ProcessConfiguration"/> for this external process.
/// </summary>
    public ProcessConfiguration Configuration { get; init; }

/// <summary>
///     Gets the <see cref="ProcessExitConfiguration"/> controlling exit handling.
/// </summary>
    public ProcessExitConfiguration ExitConfiguration { get; }

    /// <summary>
    ///     Indicates whether the external process has exited.
    /// </summary>
    public bool HasExited
    {
        get
        {
            lock (_lifecycleLock)
            {
                return _processWrapper.HasExited;
            }
        }
    }

    /// <summary>
    ///     Indicates whether the external process has started.
    /// </summary>
    public bool HasStarted
    {
        get
        {
            lock (_lifecycleLock)
            {
                return _processWrapper.HasStarted;
            }
        }
    }

/// <summary>
///     Occurs when the process starts.
/// </summary>
    public event EventHandler? Started;

/// <summary>
///     Occurs when the process exits.
/// </summary>
    public event EventHandler? Exited;

    /// <summary>
    ///     Synchronously starts the external process and returns its process ID.
    ///     Stdin piping is not performed by this method.
    /// </summary>
    /// <returns>The process ID of the started process.</returns>
    /// <remarks>
    /// Configuration is not mutated; the resolved file path is returned via the result.
    /// <see cref="ProcessResult.ExecutedFilePath"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the process has already been started.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public int Start()
    {
        ProcessWrapper wrapper;
        lock (_lifecycleLock)
        {
            if (_processWrapper.HasStarted)
                throw new InvalidOperationException("The process has already been started.");

            FileInfo filePath = _filePathResolver.ResolveFilePath(Configuration.TargetFilePath);

            _processWrapper.Started -= _startedHandler;
            _processWrapper.Exited -= _exitedHandler;
            _processWrapper.Dispose();
            _processWrapper = new ProcessWrapper(Configuration, filePath);

            _processWrapper.Started += _startedHandler;
            _processWrapper.Exited += _exitedHandler;

            _processWrapper.Start();

            wrapper = _processWrapper;
        }

        return wrapper.Id;
    }

    /// <summary>
    ///     Asynchronously starts the external process, feeding this instance's configured
    ///     standard input to the child when redirection is enabled.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task that completes once the process has been launched and any redirected
    ///     standard input has been piped to the child. The method does not wait for the
    ///     process to exit and produces no process result; obtain the result separately
    ///     via <see cref="WaitForExitOrTimeoutAsync(CancellationToken)"/> or
    ///     <see cref="CaptureBufferedResultAsync(CancellationToken, long?, long?)"/>.
    /// </returns>
    /// <remarks>
    /// Configuration is not mutated; the resolved file path surfaces on the result
    /// returned later via <see cref="WaitForExitOrTimeoutAsync(CancellationToken)"/> as
    /// <see cref="ProcessResult.ExecutedFilePath"/>.
    /// </remarks>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ProcessWrapper wrapper;
        lock (_lifecycleLock)
        {
            if (_processWrapper.HasStarted)
                throw new InvalidOperationException("The process has already been started.");

            FileInfo filePath = _filePathResolver.ResolveFilePath(Configuration.TargetFilePath);

            _processWrapper.Started -= _startedHandler;
            _processWrapper.Exited -= _exitedHandler;
            _processWrapper.Dispose();
            _processWrapper = new ProcessWrapper(Configuration, filePath);

            _processWrapper.Started += _startedHandler;
            _processWrapper.Exited += _exitedHandler;

            _processWrapper.Start();

            wrapper = _processWrapper;
        }

        if (Configuration.StandardInput is not null
            && wrapper.StartInfo.RedirectStandardInput)
        {
            try
            {
                await wrapper.PipeStandardInputAsync(Configuration.StandardInput.BaseStream,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (wrapper.Canceled)
            {
                // A copy fault that is an artifact of this instance's own kill machinery
                // (a broken pipe after ForcefulExit) is absorbed here; a genuine
                // source-stream read error propagates to the caller (ledger T006).
            }
        }
    }

    /// <summary>
    ///     Starts the external process asynchronously using the specified configuration,
    ///     feeding that configuration's standard input to the child.
    /// </summary>
    /// <param name="configuration">The configuration settings for starting the external process.</param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task that completes once the process has been launched and the supplied
    ///     configuration's standard input has been piped to the child. The method does
    ///     not wait for the process to exit and produces no process result; obtain the
    ///     result separately via <see cref="WaitForExitOrTimeoutAsync(CancellationToken)"/>
    ///     or <see cref="CaptureBufferedResultAsync(CancellationToken, long?, long?)"/>.
    /// </returns>
    /// <remarks>
    /// Configuration is not mutated; the resolved file path surfaces on the result
    /// returned later via <see cref="WaitForExitOrTimeoutAsync(CancellationToken)"/> as
    /// <see cref="ProcessResult.ExecutedFilePath"/>.
    /// </remarks>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync(ProcessConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ProcessWrapper wrapper;
        lock (_lifecycleLock)
        {
            if (_processWrapper.HasStarted)
                throw new InvalidOperationException("The process has already been started.");

            FileInfo filePath = _filePathResolver.ResolveFilePath(configuration.TargetFilePath);

            _processWrapper.Started -= _startedHandler;
            _processWrapper.Exited -= _exitedHandler;
            _processWrapper.Dispose();
            _processWrapper = new ProcessWrapper(configuration, filePath);

            _processWrapper.Started += _startedHandler;
            _processWrapper.Exited += _exitedHandler;

            if (configuration.StandardInput is not null
                && configuration.StandardInput != StreamWriter.Null)
                _processWrapper.StartInfo.RedirectStandardInput = true;

            _processWrapper.Start();

            wrapper = _processWrapper;
        }

        if (configuration.StandardInput is not null)
            await wrapper.PipeStandardInputAsync(configuration.StandardInput.BaseStream,
                cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously waits for the process to exit or a specified timeout period elapses.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The result contains the buffered
    ///     process result when the method completes.
    /// </returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken)
    {
        ProcessWrapper wrapper;
        lock (_lifecycleLock)
        {
            wrapper = _processWrapper;
        }

        await wrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken).ConfigureAwait(false);

        ProcessResult result = new(
            wrapper.StartInfo.FileName,
            wrapper.ExitCode,
            wrapper.Id,
            wrapper.StartTime,
            wrapper.ExitTime,
            canceled: wrapper.Canceled,
            signal: wrapper.Signal
        );

        return result;
    }

    /// <summary>
    ///     Asynchronously feeds <see cref="Configuration"/>'s redirected standard input to
    ///     the child concurrently with capturing its output, then waits for the external
    ///     process to exit or a specified timeout period to elapse.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <param name="maxStandardOutputBytes">
    ///     An optional maximum number of bytes to capture from standard output before truncating.
    ///     <c>null</c> means no cap is applied.
    /// </param>
    /// <param name="maxStandardErrorBytes">
    ///     An optional maximum number of bytes to capture from standard error before truncating.
    ///     <c>null</c> means no cap is applied.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The result contains the buffered
    ///     process result when the method completes.
    /// </returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task<BufferedProcessResult> CaptureBufferedResultAsync(
        CancellationToken cancellationToken,
        long? maxStandardOutputBytes = null,
        long? maxStandardErrorBytes = null)
    {
        ProcessWrapper wrapper;
        lock (_lifecycleLock)
        {
            wrapper = _processWrapper;
        }

        Task<(string StandardOutput, string StandardError, bool WasTruncated)> outputStrings = Configuration.OutputRedirection ?
            wrapper.ReadAllTextAsync(cancellationToken, maxStandardOutputBytes, maxStandardErrorBytes)
            : Task.FromResult((string.Empty, string.Empty, false));

        // Feed stdin alongside the drain-and-wait so the copy runs at the child's
        // pace instead of blocking ahead of it (ledger T003).
        Task standardInputCopy = Configuration.StandardInput is not null
            && wrapper.StartInfo.RedirectStandardInput
            ? wrapper.PipeStandardInputAsync(Configuration.StandardInput.BaseStream,
                cancellationToken)
            : Task.CompletedTask;

        Task waitTask = wrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken);

        try
        {
            try
            {
                await Task.WhenAll(waitTask, outputStrings, standardInputCopy)
                    .ConfigureAwait(false);
            }
            catch (Exception) when (standardInputCopy.IsFaulted
                                    && !waitTask.IsFaulted
                                    && !outputStrings.IsFaulted
                                    && wrapper.Canceled)
            {
                // A stdin copy fault that is an artifact of this instance's own
                // timeout/cancellation kill (a broken pipe after ForcefulExit) is
                // absorbed at this join; a genuine source-stream read error leaves
                // the kill machinery un-run and still propagates, as do any wait
                // or drain faults (ledger T006).
            }

            BufferedProcessResult result = new BufferedProcessResult(wrapper.StartInfo.FileName,
                wrapper.ExitCode,
                wrapper.Id, outputStrings.Result.StandardOutput, outputStrings.Result.StandardError,
                wrapper.StartTime,
                wrapper.ExitTime,
                canceled: wrapper.Canceled,
                signal: wrapper.Signal,
                wasTruncated: outputStrings.Result.WasTruncated);

            return result;
        }
        finally
        {
            outputStrings.Dispose();
        }
    }

    
    /// <summary>
    /// Suspends the external process that is currently running.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an attempt is made to suspend a process that has already exited.
    /// </exception>
    /// <remarks>
    /// <para> This method uses platform-specific mechanisms for process suspension and
    /// is supported on Windows, macOS, Linux, and FreeBSD. </para>
    /// <para>This operation is not supported on iOS, tvOS, or browser platforms. </para>
    /// </remarks>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public void Suspend()
    {
        lock (_lifecycleLock)
        {
            _processWrapper.SuspendProcess();
        }
    }

    /// <summary>
    /// Resumes the execution of a suspended external process.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the process has already exited and cannot be resumed.
    /// </exception>
    /// <remarks>
    /// <para> This method uses platform-specific mechanisms for process suspension and
    /// is supported on Windows, macOS, Linux, and FreeBSD. </para>
    /// <para>This operation is not supported on iOS, tvOS, or browser platforms. </para>
    /// </remarks>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public void Resume()
    {
        lock (_lifecycleLock)
        {
            _processWrapper.ResumeProcess();
        }
    }
    
    /// <summary>
    ///     Terminates the associated external process based on the specified exit configuration.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when an invalid value is provided for
    ///     ExitConfiguration.TimeoutPolicy.CancellationMode.
    /// </exception>
    public async Task Kill()
    {
        ProcessWrapper wrapper;
        lock (_lifecycleLock)
        {
            wrapper = _processWrapper;
        }

        switch (ExitConfiguration.RequestedCancellationExitBehaviour)
        {
            case ProcessExitBehaviour.ForcefulExit:
                await wrapper.WaitForExitOrForcefulTimeoutAsync(ExitConfiguration,
                    CancellationToken.None).ConfigureAwait(false);
                break;
            case ProcessExitBehaviour.GracefulExit:
                await wrapper.WaitForExitOrGracefulTimeoutAsync(ExitConfiguration,
                    CancellationToken.None).ConfigureAwait(false);
                break;
            case ProcessExitBehaviour.WaitForExit:
                await wrapper.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                return;
            default:
                wrapper.Kill();
                break;
        }
    }

    /// <summary>
    ///     Disposes of the internal managed and unmanaged resources.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Configuration" /> supplied to this process is not disposed here; the caller
    ///     owns disposal of any <see cref="ProcessConfiguration.StandardInput" /> stream or
    ///     <see cref="UserCredential" /> it provided.
    /// </remarks>
    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _processWrapper.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}

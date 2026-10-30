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
///     Wraps a <see cref="Process"/> for managed lifecycle control.
/// </summary>
// ReSharper disable once RedundantExtendsListEntry
public sealed class ExternalProcess : ISuspendableExternalProcess, IExternalProcess
{
    private ProcessWrapper _processWrapper;
    private readonly Lock _lifecycleLock = new();
    private EventHandler? _startedHandler;
    private EventHandler? _exitedHandler;
    private bool _disposed;

    private readonly IFilePathResolver _filePathResolver;
    private readonly FileInfo _resolvedFilePath;

    /// <summary>
    ///     Initialises a new instance of the <see cref="ExternalProcess" /> class,
    ///     resolving <see cref="ProcessConfiguration.TargetFilePath" /> once at construction
    ///     and reusing the resolved path in <see cref="Start" /> and
    ///     <see cref="StartAsync(CancellationToken)" />.
    /// </summary>
    /// <param name="filePathResolver">The resolver used to locate the target executable.</param>
    /// <param name="configuration">The configuration settings for the process.</param>
    /// <param name="exitConfiguration">
    ///     The exit configuration controlling timeout, cancellation, and exception handling;
    ///     defaults to <see cref="ProcessExitConfigurationCreationExtensions.CreateGraceful()" /> if
    ///     <see langword="null" />, which suppresses unexpected-exit exceptions. Pass
    ///     <c>ProcessExitConfiguration.Default</c> (equivalent to
    ///     <c>new ProcessExitConfiguration()</c>) to allow unexpected-exit exceptions instead.
    /// </param>
    /// <exception cref="FileNotFoundException">
    ///     Thrown at construction when the target file cannot be resolved.
    /// </exception>
    public ExternalProcess(IFilePathResolver filePathResolver, ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null)
    {
        _filePathResolver = filePathResolver;
        _resolvedFilePath = filePathResolver.ResolveFilePath(configuration.TargetFilePath);
        _processWrapper = new ProcessWrapper(configuration, _resolvedFilePath);
        Configuration = configuration;
        ExitConfiguration = exitConfiguration ?? ProcessExitConfiguration.CreateGraceful();

        // Re-raise with 'this' as sender: these are this class's own instance events, and
        // subscribers should not be handed the internal ProcessWrapper as the sender.
        _startedHandler = (_, args) => Started?.Invoke(this, args);
        _exitedHandler = (_, args) => Exited?.Invoke(this, args);
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

            _processWrapper.Started -= _startedHandler;
            _processWrapper.Exited -= _exitedHandler;
            _processWrapper.Dispose();
            _processWrapper = new ProcessWrapper(Configuration, _resolvedFilePath);

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

            _processWrapper.Started -= _startedHandler;
            _processWrapper.Exited -= _exitedHandler;
            _processWrapper.Dispose();
            _processWrapper = new ProcessWrapper(Configuration, _resolvedFilePath);

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
                // A copy fault that is an artefact of this instance's own kill machinery
                // (a broken pipe after ForcefulExit) is absorbed here; a genuine
                // source-stream read error propagates to the caller.
            }
            catch (Exception)
            {
                // A genuine stdin fault escaping while the child is still running: the
                // kill machinery never ran, so terminate the child instead of orphaning
                // it when the exception propagates.
                TerminateAbandonedChild();
                throw;
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

            // Only force the redirect when the configuration actually enables it:
            // a real StandardInput writer with RedirectStandardInput=false stays a
            // no-op, exactly like the Start()/StartAsync(CancellationToken) overloads.
            if (configuration.RedirectStandardInput
                && configuration.StandardInput is not null
                && configuration.StandardInput != StreamWriter.Null)
                _processWrapper.StartInfo.RedirectStandardInput = true;

            _processWrapper.Start();

            wrapper = _processWrapper;
        }

        if (configuration.StandardInput is not null
            && wrapper.StartInfo.RedirectStandardInput)
        {
            try
            {
                await wrapper.PipeStandardInputAsync(configuration.StandardInput.BaseStream,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (wrapper.Canceled)
            {
                // A copy fault that is an artifact of this instance's own kill machinery
                // (a broken pipe after ForcefulExit) is absorbed here, mirroring the
                // StartAsync(CancellationToken) overload; a genuine source-stream read
                // error propagates to the caller.
            }
            catch (Exception)
            {
                // A genuine stdin fault escaping while the child is still running: the
                // kill machinery never ran, so terminate the child instead of orphaning
                // it when the exception propagates.
                TerminateAbandonedChild();
                throw;
            }
        }
    }

    /// <summary>
    ///     Asynchronously waits for the process to exit or a specified timeout period elapses.
    /// </summary>
    /// <remarks>
    ///     This method does not drain redirected output: a configuration with
    ///     <see cref="ProcessConfiguration.OutputRedirection"/> enabled whose child writes
    ///     more than the operating-system pipe buffer blocks the child and the wait never
    ///     completes. The invocation pipelines force redirection off in Raw mode before the
    ///     process starts; direct users of this type bypass that normalization — disable
    ///     output redirection or use
    ///     <see cref="CaptureBufferedResultAsync(CancellationToken, long?, long?)"/> to
    ///     capture (and thereby drain) the redirected streams.
    /// </remarks>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The result contains the
    ///     plain <see cref="ProcessResult"/> when the method completes; redirected output
    ///     is not captured by this method — use
    ///     <see cref="CaptureBufferedResultAsync(CancellationToken, long?, long?)"/>
    ///     for buffered output.
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
    ///     process result when the method completes. When cancellation occurs and
    ///     <see cref="ProcessExitConfiguration.CancellationThrowsException"/> is
    ///     <see langword="false"/> (the default), the method returns a normal
    ///     <see cref="BufferedProcessResult"/> with <c>Canceled</c> set to
    ///     <see langword="true"/> and empty captured standard output and standard error
    ///     instead of throwing; an <see cref="OperationCanceledException"/> is thrown only
    ///     when <see cref="ProcessExitConfiguration.CancellationThrowsException"/> is
    ///     <see langword="true"/> or when a genuine stream failure occurs.
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
        // pace instead of blocking ahead of it.
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
                // A stdin copy fault that is an artefact of this instance's own
                // timeout/cancellation kill (a broken pipe after ForcefulExit) is
                // absorbed in this join; a genuine source-stream read error leaves
                // the kill machinery un-run and still propagates, as do any wait
                // or drain faults.
            }
            catch (Exception ex) when (ex is OperationCanceledException
                                       && !ExitConfiguration.CancellationThrowsException
                                       && cancellationToken.IsCancellationRequested
                                       && !waitTask.IsFaulted
                                       && !standardInputCopy.IsFaulted
                                       && wrapper.Canceled)
            {
                // A stream-read OperationCanceledException raised by this instance's
                // own cancellation machinery (the drains observe the caller's token)
                // is absorbed so the documented CancellationThrowsException=false
                // contract holds: the caller receives a normal BufferedProcessResult
                // with Cancelled set and empty captured output instead of an escaping
                // exception. When CancellationThrowsException is true nothing from
                // the cancellation machinery is absorbed — the OCE propagates whether
                // it surfaces from the wait or from the drain. (The two faults race,
                // so the knob is checked here rather than relying on which task
                // faults first.) Genuine (non-OCE) read failures and wait faults
                // always propagate.
            }

            // An absorbed output-drain cancellation leaves outputStrings without a
            // result, so read it only on successful completion; cancelled reads
            // yield no partial strings.
            (string StandardOutput, string StandardError, bool WasTruncated) capturedOutput =
                outputStrings.Status == TaskStatus.RanToCompletion
                    ? outputStrings.Result
                    : (string.Empty, string.Empty, false);

            BufferedProcessResult result = new BufferedProcessResult(wrapper.StartInfo.FileName,
                wrapper.ExitCode,
                wrapper.Id, capturedOutput.StandardOutput, capturedOutput.StandardError,
                wrapper.StartTime,
                wrapper.ExitTime,
                canceled: wrapper.Canceled,
                signal: wrapper.Signal,
                wasTruncated: capturedOutput.WasTruncated);

            return result;
        }
        catch (Exception)
        {
            // Only a propagating path reaches this handler (a genuine stream failure or a
            // wait fault); the absorbed cancellation paths above return results instead.
            // The kill machinery never ran there, so terminate a still-running child
            // rather than orphaning it when the exception escapes.
            TerminateAbandonedChild();
            throw;
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
    ///     <para>
    ///     The behaviour depends on <see cref="ExitConfiguration"/>.RequestedCancellationExitBehaviour:
    ///     <see cref="ProcessExitBehaviour.ForcefulExit"/> and
    ///     <see cref="ProcessExitBehaviour.GracefulExit"/> wait for the process to exit (bounded by
    ///     the configured timeout policy) and terminate it if it is still running;
    ///     <see cref="ProcessExitBehaviour.WaitForExit"/> only waits for the process to exit on its
    ///     own and does not terminate it; any other (out-of-range) value falls back to
    ///     <see cref="Process.Kill()"/>.
    ///     </para>
    /// </summary>
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
    ///     Terminates a started child that is still running when an invocation ends via an
    ///     exception path, so an abandoned invocation (a pre-canceled token, a genuine
    ///     stdin-stream failure, or any other fault between start and result) can never
    ///     orphan a process the caller holds no handle to.
    /// </summary>
    /// <remarks>
    ///     This is a plain forceful exit: it deliberately bypasses <see cref="Kill"/>'s
    ///     <see cref="ProcessExitConfiguration.RequestedCancellationExitBehaviour"/>
    ///     handling, which may wait out the timeout policy before terminating. The
    ///     invocation has already failed and its exception must surface promptly.
    /// </remarks>
    private void TerminateAbandonedChild()
    {
        ProcessWrapper wrapper;
        lock (_lifecycleLock)
        {
            wrapper = _processWrapper;
        }

        try
        {
            wrapper.ForcefulExit();
        }
        catch (Exception)
        {
            // The child never started, was already reaped, or cannot be terminated;
            // the invocation's own outcome is what surfaces to the caller.
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
    }
}
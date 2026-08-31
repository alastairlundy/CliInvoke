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
///     Represents an external process that can be run.
/// </summary>
// ReSharper disable once RedundantExtendsListEntry
public sealed class ExternalProcess : ISuspendableExternalProcess, IExternalProcess
{
    private ProcessWrapper _processWrapper;
    
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

        _processWrapper.Started += (sender, args) => Started?.Invoke(sender, args);
        _processWrapper.Exited += (sender, args) => Exited?.Invoke(sender, args);
    }

    /// <summary>
    ///     Represents the configuration settings used by an external process.
    /// </summary>
    public ProcessConfiguration Configuration { get; init; }

    /// <summary>
    ///     Represents the configuration for handling external process exit.
    /// </summary>
    public ProcessExitConfiguration ExitConfiguration { get; }

    /// <summary>
    ///     Indicates whether the external process has exited.
    /// </summary>
    public bool HasExited => _processWrapper.HasExited;

    /// <summary>
    ///     Indicates whether the external process has started.
    /// </summary>
    public bool HasStarted => _processWrapper.HasStarted;

    /// <summary>
    ///     Represents an event that occurs when the external process starts.
    /// </summary>
    public event EventHandler? Started;

    /// <summary>
    ///     Represents an event that occurs when the external process exits.
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
        if (HasStarted)
            throw new InvalidOperationException("The process has already been started.");

        FileInfo filePath = _filePathResolver.ResolveFilePath(Configuration.TargetFilePath);

        _processWrapper.Dispose();
        _processWrapper = new ProcessWrapper(Configuration, filePath);

        _processWrapper.Started += (sender, args) => Started?.Invoke(sender, args);
        _processWrapper.Exited += (sender, args) => Exited?.Invoke(sender, args);

        _processWrapper.Start();

        return _processWrapper.Id;
    }

    /// <summary>
    ///     Asynchronously starts the external process using the specified configuration.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation. The result contains the buffered process
    ///     result when the method completes.
    /// </returns>
    /// <remarks>
    /// Configuration is not mutated; the resolved file path is returned via the result.
    /// <see cref="ProcessResult.ExecutedFilePath"/>.
    /// </remarks>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (HasStarted)
            throw new InvalidOperationException("The process has already been started.");

        FileInfo filePath = _filePathResolver.ResolveFilePath(Configuration.TargetFilePath);

        _processWrapper.Dispose();
        _processWrapper = new ProcessWrapper(Configuration, filePath);

        _processWrapper.Started += (sender, args) => Started?.Invoke(sender, args);
        _processWrapper.Exited += (sender, args) => Exited?.Invoke(sender, args);

        _processWrapper.Start();

        await _processWrapper.WaitForExitAsync(cancellationToken);
    }

    /// <summary>
    ///     Starts the external process asynchronously using the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration settings for starting the external process.</param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation. The result contains the buffered process
    ///     result when the method completes.
    /// </returns>
    /// <remarks>
    /// Configuration is not mutated; the resolved file path is returned via the result.
    /// <see cref="ProcessResult.ExecutedFilePath"/>.
    /// </remarks>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync(ProcessConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (HasStarted)
            throw new InvalidOperationException("The process has already been started.");

        FileInfo filePath = await Task.FromResult(_filePathResolver.ResolveFilePath(configuration.TargetFilePath));

        _processWrapper.Dispose();
        _processWrapper = new ProcessWrapper(configuration, filePath);

        _processWrapper.Started += (sender, args) => Started?.Invoke(sender, args);
        _processWrapper.Exited += (sender, args) => Exited?.Invoke(sender, args);

        if (configuration.StandardInput is not null
            && configuration.StandardInput != StreamWriter.Null)
            _processWrapper.StartInfo.RedirectStandardInput = true;

        _processWrapper.Start();

        if (configuration.StandardInput is not null)
            await _processWrapper.PipeStandardInputAsync(configuration.StandardInput.BaseStream,
                cancellationToken);
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
        await _processWrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken);

        ProcessResult result = new(
            _processWrapper.StartInfo.FileName,
            _processWrapper.ExitCode,
            _processWrapper.Id,
            _processWrapper.StartTime,
            _processWrapper.ExitTime,
            canceled: _processWrapper.Canceled,
            signal: _processWrapper.Signal
        );

        return result;
    }

    /// <summary>
    ///     Asynchronously waits for the external process to exit or a specified timeout period elapses.
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
        Task<(string StandardOutput, string StandardError, bool WasTruncated)> outputStrings = Configuration.OutputRedirection ?
            _processWrapper.ReadAllTextAsync(cancellationToken, maxStandardOutputBytes, maxStandardErrorBytes)
            : Task.FromResult((string.Empty, string.Empty, false));

        try
        {
            await Task.WhenAll(
                _processWrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken),
                outputStrings);

            BufferedProcessResult result = new BufferedProcessResult(_processWrapper.StartInfo.FileName,
                _processWrapper.ExitCode,
                _processWrapper.Id, outputStrings.Result.StandardOutput, outputStrings.Result.StandardError,
                _processWrapper.StartTime,
                _processWrapper.ExitTime,
                canceled: _processWrapper.Canceled,
                signal: _processWrapper.Signal,
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
    public void Suspend() => _processWrapper.SuspendProcess();

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
    public void Resume() => _processWrapper.ResumeProcess();
    
    /// <summary>
    ///     Terminates the associated external process based on the specified exit configuration.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when an invalid value is provided for
    ///     ExitConfiguration.TimeoutPolicy.CancellationMode.
    /// </exception>
    public async Task Kill()
    {
        switch (ExitConfiguration.RequestedCancellationExitBehaviour)
        {
            case ProcessExitBehaviour.ForcefulExit:
                await _processWrapper.WaitForExitOrForcefulTimeoutAsync(ExitConfiguration,
                    CancellationToken.None);
                break;
            case ProcessExitBehaviour.GracefulExit:
                await _processWrapper.WaitForExitOrGracefulTimeoutAsync(ExitConfiguration,
                    CancellationToken.None);
                break;
            case ProcessExitBehaviour.WaitForExit:
                await _processWrapper.WaitForExitAsync(CancellationToken.None);
                return;
            default:
                _processWrapper.Kill();
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
        _processWrapper.Dispose();

        GC.SuppressFinalize(this);
    }
}

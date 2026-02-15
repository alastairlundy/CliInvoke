/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

#pragma warning disable CS0618 

using CliInvoke.Core.Processes;
using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes;
using CliInvoke.Helpers.Processes.Cancellation;

namespace CliInvoke.Processes;

/// <summary>
/// Represents an external process that can be run.
/// </summary>
public class ExternalProcess : IExternalProcess
{
    private ProcessWrapper _processWrapper;
    
    private readonly IProcessPipeHandler _processPipeHandler;
    private readonly IFilePathResolver _filePathResolver;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathResolver"></param>
    /// <param name="processPipeHandler"></param>
    /// <param name="targetFilePath"></param>
    public ExternalProcess(IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler, string targetFilePath)
    {
        _processPipeHandler = processPipeHandler;
        _filePathResolver = filePathResolver;
        
        Configuration = new ProcessConfiguration(targetFilePath,
            false, true, true);
        _processWrapper = new ProcessWrapper(Configuration, ProcessResourcePolicy.Default);
        ExitConfiguration = ProcessExitConfiguration.Default;
        
        _processWrapper.Started += (sender, args) => Started?.Invoke(sender, args);
        _processWrapper.Exited += (sender, args) => Exited?.Invoke(sender, args);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathResolver"></param>
    /// <param name="processPipeHandler"></param>
    /// <param name="configuration"></param>
    /// <param name="exitConfiguration"></param>
    public ExternalProcess(IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler,
        ProcessConfiguration configuration, ProcessExitConfiguration? exitConfiguration = null)
    {
        _processPipeHandler = processPipeHandler;
        _filePathResolver = filePathResolver;
        
        _processWrapper = new ProcessWrapper(configuration, configuration.ResourcePolicy);
        Configuration = configuration;
        ExitConfiguration = exitConfiguration ?? ProcessExitConfiguration.Default;
        
        _processWrapper.Started += (sender, args) => Started?.Invoke(sender, args);
        _processWrapper.Exited += (sender, args) => Exited?.Invoke(sender, args);
    }

    /// <summary>
    /// Represents the configuration settings used by an external process.
    /// </summary>
    public ProcessConfiguration Configuration { get; set; }

    /// <summary>
    /// Represents the configuration for handling external process exit.
    /// </summary>
    public ProcessExitConfiguration ExitConfiguration { get; set; }

    /// <summary>
    /// Indicates whether the external process has exited.
    /// </summary>
    public bool HasExited => _processWrapper.HasExited;

    /// <summary>
    /// Indicates whether the external process has started.
    /// </summary>
    public bool HasStarted => _processWrapper.HasStarted;

    /// <summary>
    /// Represents an event that occurs when the external process starts.
    /// </summary>
    public event EventHandler Started;

    /// <summary>
    /// Represents an event that occurs when the external process exits.
    /// </summary>
    public event EventHandler Exited;

    /// <summary>
    /// Asynchronously starts the external process using the specified configuration.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the buffered process result when the method completes.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync(CancellationToken cancellationToken) => await StartAsync(Configuration, cancellationToken);

    /// <summary>
    /// Starts the external process asynchronously using the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration settings for starting the external process.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the buffered process result when the method completes.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync(ProcessConfiguration configuration, CancellationToken cancellationToken)
    {
        Configuration.TargetFilePath = _filePathResolver.ResolveFilePath(
            Configuration.TargetFilePath);

        _processWrapper = new ProcessWrapper(configuration, configuration.ResourcePolicy);
        
        if (configuration.StandardInput is not null
            && configuration.StandardInput != StreamWriter.Null)
        {
            _processWrapper.StartInfo.RedirectStandardInput = true;
        }
        
        _processWrapper.Start();
        
        if(configuration.StandardInput is not null)
            await _processPipeHandler.PipeStandardInputAsync(configuration.StandardInput.BaseStream, _processWrapper, cancellationToken);
    }

    /// <summary>
    /// Asynchronously waits for the process to exit or a specified timeout period elapses.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the buffered process result when the method completes.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken)
    {
        Task<Stream> standardOutputStream = Configuration.RedirectStandardOutput ? _processPipeHandler.
                PipeStandardOutputAsync(_processWrapper, cancellationToken) 
            : (Task<Stream>)Task.CompletedTask;
        
        Task<Stream> standardErrorStream = Configuration.RedirectStandardError ? _processPipeHandler.
                PipeStandardErrorAsync(_processWrapper, cancellationToken) 
            : (Task<Stream>)Task.CompletedTask;
        
        try
        {
            await Task.WhenAll([
                _processWrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken),
                standardOutputStream,
                standardErrorStream
            ]);
            
            ProcessResult result = new(
                _processWrapper.StartInfo.FileName,
                _processWrapper.ExitCode,
                _processWrapper.Id,
                _processWrapper.StartTime,
                _processWrapper.ExitTime
            );

            ThrowIfProcessNotSuccessful(result);

            return result;
        }
        finally
        {
            standardOutputStream.Dispose();
            standardErrorStream.Dispose();
            Dispose();
        }
    }

    /// <summary>
    /// Asynchronously waits for the external process to exit or a specified timeout period elapses.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the buffered process result when the method completes.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task<BufferedProcessResult> WaitForBufferedExitOrTimeoutAsync(CancellationToken cancellationToken)
    {
        Task<string> standardOutputString = Configuration.RedirectStandardOutput ? _processWrapper.StandardOutput.ReadToEndAsync(cancellationToken) 
            : Task.FromResult(string.Empty);

        Task<string> standardErrorString = Configuration.RedirectStandardError
            ? _processWrapper.StandardError.ReadToEndAsync(cancellationToken)
            : Task.FromResult(string.Empty);
        
        try
        {
            await Task.WhenAll([
                _processWrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken),
                standardOutputString,
                standardErrorString
            ]);
            
            BufferedProcessResult result = new BufferedProcessResult(_processWrapper.StartInfo.FileName, _processWrapper.ExitCode,
                _processWrapper.Id, await standardOutputString, await standardErrorString, _processWrapper.StartTime,
                _processWrapper.ExitTime);

            ThrowIfProcessNotSuccessful(result);

            return result;
        }
        finally
        {
            standardOutputString.Dispose();
            standardErrorString.Dispose();
            Dispose();
        }
    }

    /// <summary>
    /// Terminates the associated external process based on the specified exit configuration.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid value is provided for ExitConfiguration.TimeoutPolicy.CancellationMode.</exception>
    public async Task Kill()
    {
        switch (ExitConfiguration.TimeoutPolicy.CancellationMode)
        {
            case ProcessCancellationMode.Forceful:
                await _processWrapper.WaitForExitOrForcefulTimeoutAsync(TimeSpan.Zero,
                    ExitConfiguration.CancellationExceptionBehavior, CancellationToken.None);
                break;
            case ProcessCancellationMode.Graceful:
                await _processWrapper.WaitForExitOrGracefulTimeoutAsync(TimeSpan.Zero,
                    ExitConfiguration.CancellationExceptionBehavior, CancellationToken.None);
                break;
            case ProcessCancellationMode.None:
                _processWrapper.Kill();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    /// <summary>
    /// Disposes of the <see cref="Configuration"/> and the internal managed and unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Configuration.Dispose();
        _processWrapper.Dispose();
    }

    private void ThrowIfProcessNotSuccessful(ProcessResult result)
    {
        if (ExitConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero
            && _processWrapper.ExitCode != 0)
        {
            throw new ProcessNotSuccessfulException(new ProcessExceptionInfo(result,
                Configuration));
        }
    }
}
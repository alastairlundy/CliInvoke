/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Piping;
using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes;
using CliInvoke.Helpers.Processes.Cancellation;

namespace CliInvoke.Processes;

/// <summary>
/// Represents an external process that can be run.
/// </summary>
public class ExternalProcess : IDisposable
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
    }

    /// <summary>
    /// 
    /// </summary>
    public ProcessConfiguration Configuration { get; set; }

    /// <summary>
    /// 
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
    /// 
    /// </summary>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync(CancellationToken cancellationToken) => await StartAsync(Configuration, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="cancellationToken"></param>
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
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task<PipedProcessResult> WaitForPipedExitOrTimeoutAsync(CancellationToken cancellationToken)
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

            if (Configuration is { RedirectStandardOutput: true, StandardOutput: not null })
            {
                await standardOutputStream.Result.CopyToAsync(Configuration.StandardOutput.BaseStream, cancellationToken);
            }
            if (Configuration is { RedirectStandardError: true, StandardError: not null })
            {
                await standardErrorStream.Result.CopyToAsync(Configuration.StandardError.BaseStream, cancellationToken);
            }
            
            PipedProcessResult result = new(
                _processWrapper.StartInfo.FileName,
                _processWrapper.ExitCode,
                _processWrapper.Id,
                _processWrapper.StartTime,
                _processWrapper.ExitTime,
                await standardOutputStream,
                await standardErrorStream
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
    /// 
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
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
    /// 
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
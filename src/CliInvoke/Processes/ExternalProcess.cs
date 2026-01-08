using CliInvoke.Core.Piping;
using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes;
using CliInvoke.Piping;

namespace CliInvoke.Processes;

/// <summary>
/// 
/// </summary>
public class ExternalProcess : IDisposable
{
    private ProcessWrapper _processWrapper;
    
    private readonly IProcessPipeHandler _processPipeHandler;
    private readonly IFilePathResolver _filePathResolver;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetFilePath"></param>
    public ExternalProcess(string targetFilePath)
    {
        _processPipeHandler = new ProcessPipeHandler();
        _filePathResolver = new FilePathResolver();
        
        Configuration = new ProcessConfiguration(targetFilePath,
            false, true, true);
        _processWrapper = new ProcessWrapper(Configuration, ProcessResourcePolicy.Default);
        ExitConfiguration = ProcessExitConfiguration.Default;
    }
    
    public ExternalProcess(string targetFilePath, IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler)
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
    /// <param name="configuration"></param>
    /// <param name="exitConfiguration"></param>
    public ExternalProcess(ProcessConfiguration configuration, ProcessExitConfiguration? exitConfiguration = null)
    {
        _processPipeHandler = new ProcessPipeHandler();
        _filePathResolver = new FilePathResolver();
        
        _processWrapper = new ProcessWrapper(configuration, configuration.ResourcePolicy);
        Configuration = configuration;
        ExitConfiguration = exitConfiguration ?? ProcessExitConfiguration.Default;
    }
    
    public ExternalProcess(ProcessConfiguration configuration, IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler,
        ProcessExitConfiguration? exitConfiguration = null)
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

    public bool HasExited => _processWrapper.HasExited;
    
    public bool HasStarted => _processWrapper.HasStarted;
    
    /// <summary>
    /// 
    /// </summary>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync() => await StartAsync(Configuration);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task StartAsync(ProcessConfiguration configuration)
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
            await _processPipeHandler.PipeStandardInputAsync(configuration.StandardInput.BaseStream, _processWrapper);
    }

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken)
    {
        Task<Stream> standardOutputStream = Configuration.RedirectStandardOutput ? _processPipeHandler.PipeStandardOutputAsync(_processWrapper) 
            : (Task<Stream>)Task.CompletedTask;
        
        Task<Stream> standardErrorStream = Configuration.RedirectStandardError ? _processPipeHandler.PipeStandardErrorAsync(_processWrapper) 
            : (Task<Stream>)Task.CompletedTask;
        
        try
        {
            await Task.WhenAll([
                _processWrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken),
                standardOutputStream,
                standardErrorStream
            ]);

            if (Configuration.StandardOutput is not null)
            {
                await standardOutputStream.Result.CopyToAsync(Configuration.StandardOutput.BaseStream, cancellationToken);
            }
            if (Configuration.StandardError is not null)
            {
                await standardErrorStream.Result.CopyToAsync(Configuration.StandardError.BaseStream, cancellationToken);
            }
            
            ProcessResult result = new(
                _processWrapper.StartInfo.FileName,
                _processWrapper.ExitCode,
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
                await standardOutputString, await standardErrorString, _processWrapper.StartTime, _processWrapper.ExitTime);

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

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public async Task<PipedProcessResult> WaitForPipedExitOrTimeoutAsync(CancellationToken cancellationToken)
    {
        Task<Stream> standardOutputStream = Configuration.RedirectStandardOutput ? _processPipeHandler.PipeStandardOutputAsync(_processWrapper) 
            : (Task<Stream>)Task.CompletedTask;
        
        Task<Stream> standardErrorStream = Configuration.RedirectStandardError ? _processPipeHandler.PipeStandardErrorAsync(_processWrapper) 
            : (Task<Stream>)Task.CompletedTask;
        
        try
        {
            await Task.WhenAll([
                _processWrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken),
                standardOutputStream,
                standardErrorStream
            ]);

            if (Configuration.StandardOutput is not null)
            {
                await standardOutputStream.Result.CopyToAsync(Configuration.StandardOutput.BaseStream, cancellationToken);
            }
            if (Configuration.StandardError is not null)
            {
                await standardErrorStream.Result.CopyToAsync(Configuration.StandardError.BaseStream, cancellationToken);
            }
            
            PipedProcessResult result = new(
                _processWrapper.StartInfo.FileName,
                _processWrapper.ExitCode,
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
    
    public void Kill()
    {
        _processWrapper.Kill();
    }

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
                _processWrapper.StartInfo, _processWrapper.Id,
                _processWrapper.StartInfo.FileName, true, Configuration.ResourcePolicy,
                Configuration.Credential));
        }
    }
}
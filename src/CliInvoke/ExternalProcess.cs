using CliInvoke.Core.Piping;
using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes;

namespace CliInvoke;

/// <summary>
/// 
/// </summary>
public class ExternalProcess : IDisposable
{
    private readonly IFilePathResolver _filePathResolver;
    private readonly IProcessPipeHandler _processPipeHandler;

    /// <summary>
    /// 
    /// </summary>
    public required ProcessConfiguration Configuration { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public ProcessExitConfiguration ExitConfiguration { get; set; }

    private ProcessWrapper _processWrapper;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePathResolver"></param>
    /// <param name="processPipeHandler"></param>
    public ExternalProcess(IFilePathResolver filePathResolver, IProcessPipeHandler processPipeHandler)
    {
        _filePathResolver = filePathResolver;
        _processPipeHandler = processPipeHandler;
        ExitConfiguration = ProcessExitConfiguration.Default;
        _processWrapper = new ProcessWrapper(null);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<bool> StartAsync()
    {
        ArgumentNullException.ThrowIfNull(Configuration);
        
        _processWrapper = new ProcessWrapper(resourcePolicy: Configuration.ResourcePolicy)
        {
            EnableRaisingEvents = true,
            StartInfo = Configuration.ToProcessStartInfo(Configuration.RedirectStandardOutput, Configuration.RedirectStandardError)
        };

        Configuration.TargetFilePath =
            _filePathResolver.ResolveFilePath(Configuration.TargetFilePath);
        
        bool started = _processWrapper.Start();

        if (!started)
            return false;

        try
        {
            if (Configuration.RedirectStandardInput && Configuration.StandardInput is not null)
                await _processPipeHandler.PipeStandardInputAsync(Configuration.StandardInput.BaseStream, _processWrapper);
        }
        catch
        {
            // ignored
        }

        return started;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ProcessResult> WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        ExitConfiguration = new ProcessExitConfiguration(ProcessTimeoutPolicy.None,
            ExitConfiguration.ResultValidation, ExitConfiguration.CancellationExceptionBehavior);
        
        return await WaitForExitOrTimeoutAsync(cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ProcessNotSuccessfulException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ProcessResult> WaitForExitOrTimeoutAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _processWrapper.WaitForExitOrTimeoutAsync(ExitConfiguration, cancellationToken);

            ProcessResult result = new(
                _processWrapper.StartInfo.FileName,
                _processWrapper.ExitCode,
                _processWrapper.StartTime,
                _processWrapper.ExitTime
            );

            if (
                ExitConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero
                && _processWrapper.ExitCode != 0
            )
            {
                throw new ProcessNotSuccessfulException(
                    process: _processWrapper,
                    exitCode: _processWrapper.ExitCode
                );
            }

            return result;
        }
        finally
        {
            Dispose();
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<PipedProcessResult> WaitForPipedExitAsync(CancellationToken cancellationToken = default)
    {
        ExitConfiguration = ProcessExitConfiguration.NoTimeoutDefault;
        
        return await WaitForPipedExitOrTimeoutAsync(cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<BufferedProcessResult> WaitForBufferedExitAsync(
        CancellationToken cancellationToken = default)
    {
        ExitConfiguration = new ProcessExitConfiguration(ProcessTimeoutPolicy.None,
            ExitConfiguration.ResultValidation, ExitConfiguration.CancellationExceptionBehavior);

        return await WaitForBufferedExitOrTimeoutAsync(cancellationToken);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<BufferedProcessResult> WaitForBufferedExitOrTimeoutAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            Task<string> standardOut = _processWrapper.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardError = _processWrapper.StandardError.ReadToEndAsync(cancellationToken);

            Task waitForExit = _processWrapper.WaitForExitOrTimeoutAsync(
                ExitConfiguration,
                cancellationToken
            );

            await Task.WhenAll(standardOut, standardError, waitForExit);

            BufferedProcessResult result = new(
                _processWrapper.StartInfo.FileName,
                _processWrapper.ExitCode,
                await standardOut,
                await standardError,
                _processWrapper.StartTime,
                _processWrapper.ExitTime
            );

            if (
                ExitConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero
                && _processWrapper.ExitCode != 0
            )
            {
                throw new ProcessNotSuccessfulException(
                    process: _processWrapper,
                    exitCode: _processWrapper.ExitCode
                );
            }

            if (standardOut.IsCompleted)
                standardOut.Dispose();

            if (standardError.IsCompleted)
                standardError.Dispose();

            return result;
        }
        finally
        {
            Dispose();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ProcessNotSuccessfulException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<PipedProcessResult> WaitForPipedExitOrTimeoutAsync(CancellationToken cancellationToken = default)
    {
        Task<Stream> standardOutput = _processPipeHandler.PipeStandardOutputAsync(_processWrapper);
        Task<Stream> standardError = _processPipeHandler.PipeStandardErrorAsync(_processWrapper);

        try
        {
            Task waitForExit = _processWrapper.WaitForExitOrTimeoutAsync(
                ExitConfiguration,
                cancellationToken
            );

            await Task.WhenAll(standardOutput, standardError, waitForExit);

            PipedProcessResult result = new(
                _processWrapper.StartInfo.FileName,
                _processWrapper.ExitCode,
                _processWrapper.StartTime,
                _processWrapper.ExitTime,
                await standardOutput,
                await standardError
            );

            if (
                ExitConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero
                && _processWrapper.ExitCode != 0
            )
            {
                throw new ProcessNotSuccessfulException(
                    process: _processWrapper,
                    exitCode: _processWrapper.ExitCode
                );
            }

            return result;
        }
        finally
        {
            if (standardOutput.IsCompleted)
                standardOutput.Dispose();

            if (standardError.IsCompleted)
                standardError.Dispose();
            
            Dispose();
        }
    }

    public void Dispose()
    {
        _processWrapper.Dispose();
        Configuration.Dispose();
    }
}
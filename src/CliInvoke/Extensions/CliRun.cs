/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke;

/// <summary>
/// 
/// </summary>
public static class CliRun
{
    private static Func<IProcessInvoker> _processInvokerFactory = () => new 
        ProcessInvoker(FilePathResolver.Shared);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processInvoker"></param>
    public static void UseProcessInvoker(IProcessInvoker processInvoker)
    {
        _processInvokerFactory = () => processInvoker;
    }
    
    private static IProcessInvoker GetInvoker() 
        => _processInvokerFactory.Invoke();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetFilePath"></param>
    /// <param name="arguments"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="timeoutTimeSpan"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<ProcessResult> RunAsync(string targetFilePath,
        string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        workingDirectory ??= Environment.CurrentDirectory;
        
        using ProcessConfiguration configuration = ProcessConfiguration.Create(targetFilePath,
            arguments, workingDirectory, OutputRedirectionMode.None);
        
        timeoutTimeSpan ??= ProcessTimeoutPolicy.Default.TimeoutThreshold;

        ProcessExitConfiguration exitConfiguration = ProcessExitConfiguration.CreateGraceful(
            ProcessTimeoutPolicy.FromTimeSpan((TimeSpan)timeoutTimeSpan));
        
        return await GetInvoker().ExecuteAsync(configuration, exitConfiguration, cancellationToken);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<ProcessResult> RunAsync(ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        exitConfiguration ??= ProcessExitConfiguration.CreateGraceful();
        
        return await GetInvoker().ExecuteAsync(configuration, 
            exitConfiguration, cancellationToken);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetFilePath"></param>
    /// <param name="arguments"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="timeoutTimeSpan"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<BufferedProcessResult> RunBufferedAsync(string targetFilePath,
        string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        workingDirectory ??= Environment.CurrentDirectory;

        using ProcessConfiguration configuration = ProcessConfiguration.Create(targetFilePath,
            arguments, workingDirectory);
        
        timeoutTimeSpan ??= ProcessTimeoutPolicy.Default.TimeoutThreshold;

        ProcessExitConfiguration exitConfiguration = ProcessExitConfiguration.CreateGraceful(
            ProcessTimeoutPolicy.FromTimeSpan((TimeSpan)timeoutTimeSpan));
        
        return await GetInvoker().ExecuteBufferedAsync(configuration, exitConfiguration, cancellationToken);
    }
        
    public static async Task<BufferedProcessResult> RunBufferedAsync(
        ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null, CancellationToken cancellationToken = default)
    {
        exitConfiguration ??= ProcessExitConfiguration.CreateGraceful();

        return await GetInvoker().ExecuteBufferedAsync(configuration, 
            exitConfiguration, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetFilePath"></param>
    /// <param name="arguments"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="timeoutTimeSpan"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<PipedProcessResult> RunPipedAsync(string targetFilePath,
        string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
        CancellationToken cancellationToken = default)
    {
        workingDirectory ??= Environment.CurrentDirectory;
        
        using ProcessConfiguration configuration = ProcessConfiguration.Create(targetFilePath,
            arguments, workingDirectory, OutputRedirectionMode.Pipe);
        
        timeoutTimeSpan ??= ProcessTimeoutPolicy.Default.TimeoutThreshold;

        ProcessExitConfiguration exitConfiguration = ProcessExitConfiguration.CreateGraceful(
            ProcessTimeoutPolicy.FromTimeSpan((TimeSpan)timeoutTimeSpan));
        
        return await GetInvoker().ExecutePipedAsync(configuration, exitConfiguration, cancellationToken);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<PipedProcessResult> RunPipedAsync(
        ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        exitConfiguration ??= ProcessExitConfiguration.CreateGraceful();
        
        return await GetInvoker().ExecutePipedAsync(configuration,
            exitConfiguration, cancellationToken);
    }
}
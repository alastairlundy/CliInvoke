/*
    CliInvoke Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Extensibility;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

namespace AlastairLundy.CliInvoke.Specializations.Invokers;

/// <summary>
/// Run commands through CMD with ease.
/// </summary>
public class CmdProcessInvoker : IProcessInvoker
{
    private readonly IProcessInvoker _processInvoker;
    private readonly IRunnerProcessCreator _runnerProcessCreator;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processInvoker"></param>
    /// <param name="runnerProcessCreator"></param>
    public CmdProcessInvoker(IProcessInvoker processInvoker, IRunnerProcessCreator runnerProcessCreator)
    {
        _processInvoker = processInvoker;
        _runnerProcessCreator = runnerProcessCreator;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public async Task<ProcessResult> ExecuteProcessAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        ProcessConfiguration runnerProcess = _runnerProcessCreator.CreateRunnerProcess(processConfiguration);
        
        return await _processInvoker.ExecuteProcessAsync(runnerProcess, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processResultValidation"></param>
    /// <param name="processResourcePolicy"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public async Task<ProcessResult> ExecuteProcessAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, CancellationToken cancellationToken = default)
    {
        ProcessConfiguration runnerProcess = _runnerProcessCreator.CreateRunnerProcess(new ProcessConfiguration(
            processStartInfo,
            resultValidation: processResultValidation,
            processResourcePolicy: processResourcePolicy ?? ProcessResourcePolicy.Default));
        
        return await _processInvoker.ExecuteProcessAsync(runnerProcess, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        ProcessConfiguration runnerProcess = _runnerProcessCreator.CreateRunnerProcess(processConfiguration);
        
        return await _processInvoker.ExecuteBufferedProcessAsync(runnerProcess, cancellationToken);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processStartInfo"></param>
    /// <param name="processResultValidation"></param>
    /// <param name="processResourcePolicy"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, CancellationToken cancellationToken = default)
    {
        ProcessConfiguration runnerProcess = _runnerProcessCreator.CreateRunnerProcess(new ProcessConfiguration(
            processStartInfo,
            resultValidation: processResultValidation,
            processResourcePolicy: processResourcePolicy ?? ProcessResourcePolicy.Default));
        
        return await _processInvoker.ExecuteBufferedProcessAsync(runnerProcess, cancellationToken);
    }

    
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public async Task<PipedProcessResult> ExecutePipedProcessAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        ProcessConfiguration runnerProcess = _runnerProcessCreator.CreateRunnerProcess(processConfiguration);
        
        return await _processInvoker.ExecutePipedProcessAsync(runnerProcess, cancellationToken);
    }

#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public async Task<PipedProcessResult> ExecutePipedProcessAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, CancellationToken cancellationToken = default)
    {
        ProcessConfiguration runnerProcess = _runnerProcessCreator.CreateRunnerProcess(new ProcessConfiguration(
            processStartInfo,
            resultValidation: processResultValidation,
            processResourcePolicy: processResourcePolicy ?? ProcessResourcePolicy.Default));
        
        return await _processInvoker.ExecutePipedProcessAsync(runnerProcess, cancellationToken);
    }
}
/*
    AlastairLundy.CliInvoke
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using AlastairLundy.CliInvoke.Core.Builders;

using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// 
/// </summary>
public class ProcessExitInfoBuilder : IProcessExitInfoBuilder
{
    private readonly ProcessExitInfo _processExitInfo;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processExitInfo"></param>
    protected ProcessExitInfoBuilder(ProcessExitInfo processExitInfo)
    {
        _processExitInfo = processExitInfo;
    }

    public ProcessExitInfoBuilder()
    {
        _processExitInfo = new ProcessExitInfo();
    }
        
    /// <summary>
    /// Sets the Result Validation whether to throw an exception or not if the Process does not execute successfully.
    /// </summary>
    /// <param name="validation">The result validation behaviour to be used.</param>
    /// <returns>The new ProcessExitInfoBuilder object with the configured Result Validation behaviour.</returns>
    public IProcessExitInfoBuilder WithValidation(ProcessResultValidation validation) =>
        new ProcessExitInfoBuilder(
            new ProcessExitInfo(_processExitInfo.TimeoutPolicy, validation));

    /// <summary>
    /// Sets the Process Timeout Policy to be used for this Process.
    /// </summary>
    /// <param name="processTimeoutPolicy">The process timeout policy to use.</param>
    /// <returns>The new ProcessExitInfoBuilder with the specified Process Timeout Policy.</returns>
    public IProcessExitInfoBuilder WithProcessTimeoutPolicy(ProcessTimeoutPolicy processTimeoutPolicy) =>
        new ProcessExitInfoBuilder(
            new ProcessExitInfo(processTimeoutPolicy, _processExitInfo.ResultValidation));

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ProcessExitInfo Build() =>  
        _processExitInfo;
}
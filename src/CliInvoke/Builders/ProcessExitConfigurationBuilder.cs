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
public class ProcessExitConfigurationBuilder : IProcessExitConfigurationBuilder
{
    private readonly ProcessExitConfiguration _processExitConfiguration;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processExitInfo"></param>
    protected ProcessExitConfigurationBuilder(ProcessExitConfiguration processExitInfo)
    {
        _processExitConfiguration = processExitInfo;
    }

    public ProcessExitConfigurationBuilder()
    {
        _processExitConfiguration = new ProcessExitConfiguration();
    }
        
    /// <summary>
    /// Sets the Result Validation whether to throw an exception or not if the Process does not execute successfully.
    /// </summary>
    /// <param name="validation">The result validation behaviour to be used.</param>
    /// <returns>The new ProcessExitInfoBuilder object with the configured Result Validation behaviour.</returns>
    public IProcessExitConfigurationBuilder WithValidation(ProcessResultValidation validation) =>
        new ProcessExitConfigurationBuilder(
            new ProcessExitConfiguration(_processExitInfo.TimeoutPolicy, validation));

    /// <summary>
    /// Sets the Process Timeout Policy to be used for this Process.
    /// </summary>
    /// <param name="processTimeoutPolicy">The process timeout policy to use.</param>
    /// <returns>The new ProcessExitInfoBuilder with the specified Process Timeout Policy.</returns>
    public IProcessExitConfigurationBuilder WithProcessTimeoutPolicy(ProcessTimeoutPolicy processTimeoutPolicy) =>
        new ProcessExitConfigurationBuilder(
            new ProcessExitConfiguration(processTimeoutPolicy, _processExitInfo.ResultValidation));

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ProcessExitConfiguration Build() =>  
        _processExitConfiguration;
}
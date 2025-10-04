/*
    AlastairLundy.CliInvoke
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics.Contracts;
using AlastairLundy.CliInvoke.Core.Builders;

using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// A class that implements fluent builder methods for configuring a ProcessExitConfiguration. 
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
    [Pure]
    public IProcessExitConfigurationBuilder WithValidation(ProcessResultValidation validation) =>
        new ProcessExitConfigurationBuilder(
            new ProcessExitConfiguration(_processExitConfiguration.TimeoutPolicy, validation,
                _processExitConfiguration.CancellationExceptionBehavior));

    /// <summary>
    /// Sets the Process Timeout Policy to be used for this Process.
    /// </summary>
    /// <param name="processTimeoutPolicy">The process timeout policy to use.</param>
    /// <returns>The new ProcessExitInfoBuilder with the specified Process Timeout Policy.</returns>
    [Pure]
    public IProcessExitConfigurationBuilder WithProcessTimeoutPolicy(ProcessTimeoutPolicy processTimeoutPolicy) =>
        new ProcessExitConfigurationBuilder(
            new ProcessExitConfiguration(processTimeoutPolicy, _processExitConfiguration.ResultValidation, 
                _processExitConfiguration.CancellationExceptionBehavior));

    /// <summary>
    /// Sets the Process Cancellation Exception Behaviour to be used for this Process.
    /// </summary>
    /// <param name="cancellationExceptionBehavior">The Process Cancellation Exception Behavior to Set.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified <see cref="ProcessCancellationExceptionBehavior"/> strategy.</returns>
    [Pure]
    public IProcessExitConfigurationBuilder WithCancellationExceptionBehaviour(
        ProcessCancellationExceptionBehavior cancellationExceptionBehavior) =>
        new ProcessExitConfigurationBuilder(
            new ProcessExitConfiguration(_processExitConfiguration.TimeoutPolicy, _processExitConfiguration.ResultValidation, 
                cancellationExceptionBehavior));

    /// <summary>
    /// Builds the ProcessExitConfiguration with the configured parameters.
    /// </summary>
    /// <returns>The newly configured Process Exit Configuration.</returns>
    public ProcessExitConfiguration Build() =>  
        _processExitConfiguration;
}
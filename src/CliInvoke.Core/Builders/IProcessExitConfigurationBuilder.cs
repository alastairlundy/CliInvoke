/*
    AlastairLundy.CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Core.Builders;

/// <summary>
/// An interface that defines fluent builder methods for configuring a ProcessExitConfiguration. 
/// </summary>
public interface IProcessExitConfigurationBuilder
{
        
    /// <summary>
    /// Sets the Result Validation whether to throw an exception or not if the Process does not execute successfully.
    /// </summary>
    /// <param name="validation">The result validation behaviour to be used.</param>
    /// <returns>The new ProcessConfigurationBuilder object with the configured Result Validation behaviour.</returns>
    IProcessExitConfigurationBuilder WithValidation(ProcessResultValidation validation);
    
    /// <summary>
    /// Sets the Process Timeout Policy to be used for this Process.
    /// </summary>
    /// <param name="processTimeoutPolicy">The process timeout policy to use.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Process Timeout Policy.</returns>
    IProcessExitConfigurationBuilder WithProcessTimeoutPolicy(ProcessTimeoutPolicy processTimeoutPolicy);

    /// <summary>
    /// Sets the Process Cancellation Exception Behaviour to be used for this Process.
    /// </summary>
    /// <param name="cancellationExceptionBehavior">The Process Cancellation Exception Behavior to Set.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified <see cref="ProcessCancellationExceptionBehavior"/> strategy.</returns>
    IProcessExitConfigurationBuilder WithCancellationExceptionBehaviour(ProcessCancellationExceptionBehavior cancellationExceptionBehavior);
    
    /// <summary>
    /// Builds the ProcessExitConfiguration with the configured parameters.
    /// </summary>
    /// <returns>The newly configured Process Exit Configuration.</returns>
    ProcessExitConfiguration Build();
}
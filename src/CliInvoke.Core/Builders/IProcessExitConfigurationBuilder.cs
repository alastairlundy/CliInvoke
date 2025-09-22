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
/// 
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
    /// 
    /// </summary>
    /// <returns></returns>
    ProcessExitConfiguration Build();
}
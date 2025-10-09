/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;

namespace AlastairLundy.CliInvoke.Core.Builders;

/// <summary>
/// Defines a fluent builder interface for creating a ProcessTimeoutPolicy.
/// </summary>
public interface IProcessTimeoutPolicyBuilder
{
    
    /// <summary>
    /// Sets the timeout threshold for the process.
    /// </summary>
    /// <param name="timeoutThreshold">The TimeSpan that the process is allowed to run before timing out.</param>
    /// <return>This method returns itself allowing for method chaining.</return>
    IProcessTimeoutPolicyBuilder WithTimeoutThreshold(TimeSpan timeoutThreshold);

    /// <summary>
    /// Sets the cancellation mode for the process if the timeout is reached.
    /// </summary>
    /// <param name="cancellationMode">The ProcessCancellationMode to use.</param>
    /// <returns>This method returns itself allowing for method chaining.</returns>
    IProcessTimeoutPolicyBuilder WithCancellationMode(ProcessCancellationMode cancellationMode);
    
    /// <summary>
    /// Builds a new instance of ProcessTimeoutPolicy based on the specified settings.
    /// </summary>
    /// <returns>The ProcessTimeoutPolicy based on the specified settings</returns>
    ProcessTimeoutPolicy Build();
}
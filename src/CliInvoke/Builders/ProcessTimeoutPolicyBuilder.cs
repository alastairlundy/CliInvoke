/*
    AlastairLundy.CliInvoke
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */


using System;
using System.Diagnostics.Contracts;

using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// Implements the IProcessTimeoutPolicyBuilder fluent interface.
/// </summary>
public class ProcessTimeoutPolicyBuilder : IProcessTimeoutPolicyBuilder
{
    private readonly ProcessTimeoutPolicy _policy;

    /// <summary>
    /// Instantiates the <see cref="ProcessTimeoutPolicyBuilder"/> allowing for the construction of a ProcessTimeoutPolicy instance.
    /// </summary>
    public ProcessTimeoutPolicyBuilder()
    {
        _policy = ProcessTimeoutPolicy.None;
    }

    /// <summary>
    /// Instantiates the <see cref="ProcessTimeoutPolicyBuilder"/> with the specified Timeout Policy.
    /// </summary>
    /// <param name="policy">The process timeout policy to configure.</param>
    protected ProcessTimeoutPolicyBuilder(ProcessTimeoutPolicy policy)
    {
        _policy = policy;
    }
    
    /// <summary>
    /// Sets the timeout threshold for the process.
    /// </summary>
    /// <param name="timeoutThreshold">The TimeSpan that the process is allowed to run before timing out.</param>
    /// <return>This method returns itself allowing for method chaining.</return>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the <see cref="TimeSpan"/> is less than zero milliseconds.</exception>
    [Pure]
    public IProcessTimeoutPolicyBuilder WithTimeoutThreshold(TimeSpan timeoutThreshold)
    {
#if NET8_0_OR_GREATER
        bool lessThanZero = double.IsNegative(timeoutThreshold.TotalMilliseconds);
#else
        bool lessThanZero = timeoutThreshold.TotalMilliseconds < double.Parse("0.0");
#endif        

       if(timeoutThreshold < TimeSpan.Zero || lessThanZero)
           throw new ArgumentOutOfRangeException(nameof(timeoutThreshold));
        
       return new ProcessTimeoutPolicyBuilder(
           new ProcessTimeoutPolicy(timeoutThreshold, _policy.CancellationMode));
    }

    /// <summary>
    /// Sets the cancellation mode for the process if the timeout is reached.
    /// </summary>
    /// <param name="cancellationMode">The ProcessCancellationMode to use.</param>
    /// <returns>This method returns itself allowing for method chaining.</returns>
    [Pure]
    public IProcessTimeoutPolicyBuilder WithCancellationMode(ProcessCancellationMode cancellationMode) =>
        new ProcessTimeoutPolicyBuilder(
            new ProcessTimeoutPolicy(_policy.TimeoutThreshold, cancellationMode));

    /// <summary>
    /// Builds a new instance of ProcessTimeoutPolicy based on the specified settings.
    /// </summary>
    /// <returns>The ProcessTimeoutPolicy based on the specified settings</returns>
    public ProcessTimeoutPolicy Build() => _policy;
}
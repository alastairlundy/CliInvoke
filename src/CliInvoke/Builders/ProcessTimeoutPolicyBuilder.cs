/*
    AlastairLundy.CliInvoke
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */


using System;
using AlastairLundy.CliInvoke.Core.Builders;

using AlastairLundy.DotPrimitives.Processes;
using AlastairLundy.DotPrimitives.Processes.Policies;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// 
/// </summary>
public class ProcessTimeoutPolicyBuilder : IProcessTimeoutPolicyBuilder
{
    private readonly ProcessTimeoutPolicy _policy;
    
    /// <summary>
    /// 
    /// </summary>
    public ProcessTimeoutPolicyBuilder()
    {
        _policy = ProcessTimeoutPolicy.None;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="policy"></param>
    protected ProcessTimeoutPolicyBuilder(ProcessTimeoutPolicy policy)
    {
        _policy = policy;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeoutThreshold"></param>
    /// <returns></returns>
    public IProcessTimeoutPolicyBuilder WithTimeoutThreshold(TimeSpan timeoutThreshold) =>
        new ProcessTimeoutPolicyBuilder(
            new ProcessTimeoutPolicy(timeoutThreshold, _policy.CancellationMode));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationMode"></param>
    /// <returns></returns>
    public IProcessTimeoutPolicyBuilder WithCancellationMode(ProcessCancellationMode cancellationMode) =>
        new ProcessTimeoutPolicyBuilder(
            new ProcessTimeoutPolicy(_policy.TimeoutThreshold, cancellationMode));

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ProcessTimeoutPolicy Build() => _policy;
}
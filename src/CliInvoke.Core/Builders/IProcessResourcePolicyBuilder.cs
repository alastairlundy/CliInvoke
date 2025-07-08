/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;
using AlastairLundy.DotPrimitives.Processes.Policies;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AlastairLundy.CliInvoke.Core.Builders;

/// <summary>
/// An interface that defines the fluent builder methods all ProcessResourcePolicyBuilder classes must implement.
/// </summary>
public interface IProcessResourcePolicyBuilder
{
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified ProcessorAffinity.
    /// </summary>
    /// <param name="processorAffinity">The processor affinity to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated ProcessorAffinity.</returns>
    /// <remarks>Process objects only support Processor Affinity on Windows and Linux operating systems.</remarks>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
#endif
    IProcessResourcePolicyBuilder WithProcessorAffinity(nint processorAffinity);
    
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Minimum Working Set.
    /// </summary>
    /// <param name="minWorkingSet">The minimum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated minimum working set.</returns>
    IProcessResourcePolicyBuilder WithMinWorkingSet(nint minWorkingSet);
    
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Maximum Working Set.
    /// </summary>
    /// <param name="maxWorkingSet">The maximum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated maximum working set.</returns>
    IProcessResourcePolicyBuilder WithMaxWorkingSet(nint maxWorkingSet);
    
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Process Priority Class.
    /// </summary>
    /// <param name="processPriorityClass">The Process Priority Class to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated Process Priority Class.</returns>
    IProcessResourcePolicyBuilder WithPriorityClass(ProcessPriorityClass processPriorityClass);
    
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Priority Boost behaviour.
    /// </summary>
    /// <param name="enablePriorityBoost">The priority boost behaviour to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated priority boost behaviour.</returns>
    IProcessResourcePolicyBuilder WithPriorityBoost(bool enablePriorityBoost);
    
    /// <summary>
    /// Builds the configured ProcessResourcePolicy
    /// </summary>
    /// <returns>The configured ProcessResourcePolicy.</returns>
    ProcessResourcePolicy Build();
}
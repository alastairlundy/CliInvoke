/*
    AlastairLundy.ProcessInvoke  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.Versioning;
using AlastairLundy.CliInvoke.Core.Abstractions.Builders;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// A class to fluently configure and build ProcessResourcePolicy objects.
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ProcessResourcePolicyBuilder : IProcessResourcePolicyBuilder
{
    private readonly ProcessResourcePolicy _processResourcePolicy;

    /// <summary>
    /// Instantiates the ProcessResourcePolicy Builder with default ProcessResourcePolicy values.
    /// </summary>
    public ProcessResourcePolicyBuilder()
    {
        _processResourcePolicy = ProcessResourcePolicy.Default;
    }

    /// <summary>
    /// Internally instantiates the ProcessResourcePolicy object with the specified ProcessResourcePolicy value.
    /// </summary>
    /// <param name="processResourcePolicy">The process resource policy object to use.</param>
    protected ProcessResourcePolicyBuilder(ProcessResourcePolicy processResourcePolicy)
    {
        _processResourcePolicy = processResourcePolicy;
    }

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
    [Pure]
    public IProcessResourcePolicyBuilder WithProcessorAffinity(nint processorAffinity) =>
        new ProcessResourcePolicyBuilder(new ProcessResourcePolicy(
            processorAffinity,
            _processResourcePolicy.MinWorkingSet,
            _processResourcePolicy.MaxWorkingSet,
            _processResourcePolicy.PriorityClass,
            _processResourcePolicy.EnablePriorityBoost));

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Minimum Working Set.
    /// </summary>
    /// <param name="minWorkingSet">The minimum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated minimum working set.</returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("android")]
#endif
    [Pure]
    public IProcessResourcePolicyBuilder WithMinWorkingSet(nint minWorkingSet) =>
        new ProcessResourcePolicyBuilder(new ProcessResourcePolicy(
            _processResourcePolicy.ProcessorAffinity,
            minWorkingSet,
            _processResourcePolicy.MaxWorkingSet,
            _processResourcePolicy.PriorityClass,
            _processResourcePolicy.EnablePriorityBoost));
    
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Maximum Working Set.
    /// </summary>
    /// <param name="maxWorkingSet">The maximum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated maximum working set.</returns>
    [Pure]
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("android")]
#endif
    public IProcessResourcePolicyBuilder WithMaxWorkingSet(nint maxWorkingSet) =>
        new ProcessResourcePolicyBuilder(new ProcessResourcePolicy(
            _processResourcePolicy.ProcessorAffinity,
            _processResourcePolicy.MinWorkingSet,
            maxWorkingSet,
            _processResourcePolicy.PriorityClass,
            _processResourcePolicy.EnablePriorityBoost));
    
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Process Priority Class.
    /// </summary>
    /// <param name="processPriorityClass">The Process Priority Class to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated Process Priority Class.</returns>
    [Pure]
    public IProcessResourcePolicyBuilder WithPriorityClass(ProcessPriorityClass processPriorityClass) =>
        new ProcessResourcePolicyBuilder(new ProcessResourcePolicy(
            _processResourcePolicy.ProcessorAffinity,
            _processResourcePolicy.MinWorkingSet,
            _processResourcePolicy.MaxWorkingSet,
            processPriorityClass,
            _processResourcePolicy.EnablePriorityBoost));
    
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Priority Boost behaviour.
    /// </summary>
    /// <param name="enablePriorityBoost">The priority boost behaviour to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated priority boost behaviour.</returns>
    [Pure]
    public IProcessResourcePolicyBuilder WithPriorityBoost(bool enablePriorityBoost) =>
        new ProcessResourcePolicyBuilder(new ProcessResourcePolicy(
            _processResourcePolicy.ProcessorAffinity,
            _processResourcePolicy.MinWorkingSet,
            _processResourcePolicy.MaxWorkingSet,
            _processResourcePolicy.PriorityClass,
            enablePriorityBoost));
    
    /// <summary>
    /// Builds the configured ProcessResourcePolicy
    /// </summary>
    /// <returns>The configured ProcessResourcePolicy.</returns>
    public ProcessResourcePolicy Build() => _processResourcePolicy;
}
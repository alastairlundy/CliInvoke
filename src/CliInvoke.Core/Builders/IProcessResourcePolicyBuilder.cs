/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;

namespace CliInvoke.Core.Builders;

/// <summary>
/// A fluent builder interface for configuring and building a Process Resource Policy.
/// </summary>
public interface IProcessResourcePolicyBuilder
{
    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified ProcessorAffinity.
    /// </summary>
    /// <param name="processorAffinity">The processor affinity to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated ProcessorAffinity.</returns>
    /// <remarks>Process objects only support Processor Affinity on Windows and Linux operating systems.</remarks>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    IProcessResourcePolicyBuilder SetProcessorAffinity(nint processorAffinity);

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Minimum Working Set.
    /// </summary>
    /// <param name="minWorkingSet">The minimum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated minimum working set.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("android")]
    IProcessResourcePolicyBuilder SetMinWorkingSet(nint minWorkingSet);

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Maximum Working Set.
    /// </summary>
    /// <param name="maxWorkingSet">The maximum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated maximum working set.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("android")]
    IProcessResourcePolicyBuilder SetMaxWorkingSet(nint maxWorkingSet);

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Process Priority Class.
    /// </summary>
    /// <param name="processPriorityClass">The Process Priority Class to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated Process Priority Class.</returns>
    IProcessResourcePolicyBuilder SetPriorityClass(ProcessPriorityClass processPriorityClass);

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Priority Boost behaviour.
    /// </summary>
    /// <param name="enablePriorityBoost">The priority boost behaviour to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated priority boost behaviour.</returns>
    IProcessResourcePolicyBuilder ConfigurePriorityBoost(bool enablePriorityBoost);

    /// <summary>
    /// Builds the configured ProcessResourcePolicy
    /// </summary>
    /// <returns>The configured ProcessResourcePolicy.</returns>
    ProcessResourcePolicy Build();
}
